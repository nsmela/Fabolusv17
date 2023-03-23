using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using g3;
using Microsoft.Windows.Themes;

namespace Fabolus.Features.Bolus {
    internal class Node {
        public int Id; //vertex id
        public Vector3d Vertex;
        public double DistanceToEnd;
        public double DistanceSoFar;
        public List<Tuple<int, double>> ChildrenNodes; //id and distance to that node
        public int? NearestToStartIndex; //id of node in map
        public bool Visited; //node has been fully evaluated and doesn't need to be revisited
    }

    internal class EdgeNode {
        public int Id; //edge id
        public int StartVert;
        public int EndVert;
        public double Length;
        public Index2i Edge;
    }

    public partial class BolusModel {
        public int? GetTriangleId(int t1, int t2, int t3) {
            var tId = TransformedMesh.FindTriangle(t1, t2, t3);
            return tId;
        }

        //used for mesh vertices calculations
        private PointHashGrid3d<int> _pointhash;
        private List<Node> _nodeMap; //used for calculating paths

        private List<Point3D>? ShortestPath(Point3D startPoint, int startTriangle, Point3D endPoint, int endTriangle, double angleThreshold = 45.0f) {
            //uses the Dijkstra-inspired method A* to find the path from the nodes
            //create/update list of nodes
            GenerateNodeMap(new Vector3d(endPoint.X, endPoint.Y, endPoint.Z));

            //using triangles to get the starting vertex index of the node. Crude, but reliable and quick.
            //TODO: optimize to find the best vertex closest to the end point or start point
            var start = TransformedMesh.GetTriangle(startTriangle).a;
            var end = TransformedMesh.GetTriangle(endTriangle).a;

            if (start < 0 || end < 0) return null; //if cannot find start or end positions, abort

            //angle used to judge how far off from the input
            var refAngle = TransformedMesh.GetVertexNormal(start);
            double weightOffset = 2.0f;

            //search
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(_nodeMap[start]);

            do {
                //sort queue
                queue.OrderBy(n => n.DistanceSoFar + n.DistanceToEnd);
                //grab first node and remove it from the queue
                var parentNode = queue.Dequeue();
                int parentIndex = _nodeMap.FindIndex(n => n == parentNode);

                //foreach child
                foreach (var childNode in parentNode.ChildrenNodes.OrderBy(t => t.Item2)) {
                    if (_nodeMap[childNode.Item1].Visited) continue; //skip child node if visited

                    int nodeIndex = childNode.Item1;
                    double nodeDistance = childNode.Item2;

                    //calculate weight = parent.DistanceSoFar + child.Distance
                    double weight = parentNode.DistanceSoFar + nodeDistance;
                    //checks angle
                    if (TransformedMesh.GetVertexNormal(nodeIndex).AngleD(refAngle) > angleThreshold) weight *= weightOffset;
                    if (TransformedMesh.GetVertexNormal(nodeIndex).AngleD(refAngle * 2.0f) > angleThreshold) weight *= weightOffset;

                    //if nearest to start is null
                    //OR if weight < smallest weight
                    if (_nodeMap[nodeIndex].DistanceSoFar == 0.0f || parentNode.DistanceSoFar + nodeDistance < _nodeMap[nodeIndex].DistanceSoFar) {
                        _nodeMap[nodeIndex].DistanceSoFar = weight;
                        _nodeMap[nodeIndex].NearestToStartIndex = parentIndex;
                        //if queue does not have this node, add it
                        if (!queue.Contains(_nodeMap[nodeIndex])) queue.Enqueue(_nodeMap[nodeIndex]);
                    }
                }
                parentNode.Visited = true;
                //if parent equals the end index, exit the algorythm
                if (parentIndex == end)
                    return GetPathPoints(_nodeMap, start, end);

            } while (queue.Any());

            return null;//didn't find a path
        }

        private void GenerateNodeMap(Vector3d endPoint) {
            if (TransformedMesh.VertexIndices().Count() <= 0) return;
            if (_nodeMap == null) {
                _nodeMap = new();

                foreach (var index in TransformedMesh.VertexIndices()) {
                    Vector3d vert = TransformedMesh.GetVertex(index);
                    var node = new Node {
                        Id = index,
                        Vertex = vert,
                        DistanceToEnd = vert.Distance(endPoint),
                        DistanceSoFar = 0.0f,
                        ChildrenNodes = GetChildrenNodes(index),
                        Visited = false
                    };
                    _nodeMap.Add(node);
                }

                return;
            }

            foreach(var node in _nodeMap) {
                //skips calculating node children
                node.Visited = false;
                node.Vertex = TransformedMesh.GetVertex(node.Id);//if rotated after generated, need to update
                node.DistanceToEnd = node.Vertex.Distance(endPoint);
                node.DistanceSoFar = 0.0f;
            }
        }
    
        private List<Tuple<int, double>> GetChildrenNodes(int index) {
            var result = new List<Tuple<int, double>>();

            //all vertices linked to a single vert can be found from the triangles linked to that vert
            var children = TransformedMesh.VtxVerticesItr(index);
            foreach(var child in children) {
                if (child == index) continue;
                result.Add(new Tuple<int, double>(child, DistanceTo(index, child)));
            }

            return result;
        }

        private double DistanceTo(int startVertex, int endVertex) {
            Vector3d start = TransformedMesh.GetVertex(startVertex);
            Vector3d end = TransformedMesh.GetVertex(endVertex);

            return start.Distance(end);
        }

        //TODO reuse this? was faulting before, but that couldbe due to not using the transformed mesh
        private int FindClosestVertices(Vector3d point, double searchRadius) {
            //outputs the vid and the distance
            var result = _pointhash.FindNearestInRadius(point, searchRadius,
                (v) => { return point.DistanceSquared(TransformedMesh.GetVertex(v)); });
            return result.Key;
        }

        private List<Point3D> GetPathPoints(List<Node> map, int startIndex, int endIndex) {
            var path = new List<Point3D>();

            int currentNodeIndex = endIndex;
            do {
                var node = map[currentNodeIndex];
                Point3D point = new Point3D(node.Vertex.x, node.Vertex.y, node.Vertex.z);
                path.Add(point);
                currentNodeIndex = (int)node.NearestToStartIndex;
            } while (currentNodeIndex != startIndex);

            return path;
        }

        private List<Point3D> GetGeodesicPath(List<Point3D> path) {
            //Youtube link: https://www.youtube.com/watch?v=DbNEsryLULE

            //create an A* path between endpoints
            //var path = ShortestPath(start, t1, end, t2);

            //a new mesh to edit as we see fit
            var mesh = new DMesh3();
            mesh.Copy(TransformedMesh);

            //don't use vertex positions, only edge lengths
            //get vertex id from nodeMap
            var verts = new List<int>();
            foreach (var n in _nodeMap) verts.Add(n.Id);

            //get edge ids from those vertex ids
            var edges = new List<int>();
            for(int i = 1; i < verts.Count; i++) {
                var e = mesh.VtxVerticesItr(verts[i]);
                var edge = e.First(x => x == verts[i - 1]);
                edges.Add(edge);
            }

            //generate node list
            var nodes = new List<EdgeNode>();
            foreach (var e in edges) {
                nodes.Add(new EdgeNode {
                    Id = e,
                    Edge = mesh.GetEdgeV(e)
                });
            }

            //convert path into a list of edges and their lengths
            //IterativeShorten();
            bool shortest = false;
            while (shortest) {
                Index3i orig_t0 = new();
                Index3i orig_t1 = new();
                Index3i flip_t0 = new();
                Index3i flip_t1 = new();

                DMesh3.EdgeFlipInfo eInfo = new();

                for (int i = 0; i < nodes.Count - 1; i++) {
                    //check angle formed by each step in the path. greater than pi implies geodesic
                    //if not, introduce edge flips to reduce the path
                    var eID = nodes[i].Id;
                    var endID = nodes[i].Id;
                    var edge = mesh.GetEdge(eID); // return [v0,v1,t0,t1], or Index4i.Max if eid is invalid
                    var endEdge = mesh.GetEdge(endID);

                    //finding the edge verts and their associations
                    //start vert, shared vert, endpoint vert
                    int v0 = FindEdgeVertexNotShared(endEdge, edge);
                    int v1 = FindSharedEdgeVertex(edge, endEdge);
                    int v2 = FindEdgeVertexNotShared(edge, endEdge);

                    //find the first triangle's edges and their verts not on the edge
                    int? vT0 = GetOtherTriangleVertex(mesh, edge.c, v0, v1);
                    int? vT1 = GetOtherTriangleVertex(mesh, edge.d, v0, v1);

                    if (vT0 == null || vT1 == null) continue;

                    //which triangle vertex is cloest to the endpoint?
                    double distance0 = mesh.GetVertex((int)vT0).Distance(mesh.GetVertex(v2));
                    double distance1 = mesh.GetVertex((int)vT1).Distance(mesh.GetVertex(v2));

                    int vID = (distance0 < distance1) ? (int)vT0 : (int)vT1; //the vertex to use for shortening
                    
                    //is angle Bi less than pi?








                    //index ids


                    //check which triangle is best suited
                    //the vert that's closest to the endpoint belongs to the best triangle










                    //for current edge, check if the edge flip
                    var angleBetweenEdges = 0;

                    //preview the edge flip
                    MeshUtil.GetEdgeFlipTris(mesh, nodes[i].Id,
                        out orig_t0, out orig_t1, out flip_t0, out flip_t1);

                    //FlipOut subroutine
                        //input path through verts a, b, c
                        //output: new shorter path through verts
                        //output: also each direction to assist with tracing along the surface

                        //get both triangles associated with edge ab
                        //which of these triangles should be flipped? the one who's vert is closest to the endpoint
                        //

                        //check if angle < pi
                        //using the edge to be flipped, use MeshUtil.OpeningAngleD
                        //if so, flip
                        //next edge
                        //continue until back to input edges endpoint

                    var flip = mesh.FlipEdge(nodes[i].Id, out eInfo);
                }
                //checked if this is the shortest path possible
            }
            //once completed, need to trace out the path to keep the line along the surface. 
            return null;
        }

        private int FindSharedEdgeVertex(Index4i e0, Index4i e1) {
            if (e0.a == e1.a) return e0.a;
            if (e0.a == e1.b) return e0.a;
            if (e0.b == e1.a) return e0.b;
            if (e0.b == e1.a) return e0.b;

            return DMesh3.InvalidID;
        }

        /// <summary>
        /// Find the vertex on test edge that isn't shared with referenceEdge
        /// </summary>
        /// <param name="reID">Reference Edge used to filter out vertices</param>
        /// <param name="testEdge">Test Edge has vertex we want</param>
        //
        private int FindEdgeVertexNotShared(Index4i reID, Index4i teID) {
            if (teID.a != reID.a && teID.a != reID.b) return teID.a;
            if (teID.b != reID.a && teID.a != reID.b) return teID.b;

            return DMesh3.InvalidID;
        }

        private Index3i FindClosestTriangle(Index3i t1, Index3i t2, Vector3d target) {
            //Indexi is vId0, vID1, vID2

            return DMesh3.InvalidTriangle;
        }

        private int? GetOtherTriangleVertex(DMesh3 mesh, int tID, int v0, int v1) {
            var verts = mesh.GetTriangle(tID); //get vert IDs from triangle

            foreach(var v in verts.array ) 
                if (v != v0 && v != v1) return v;

            return null; //return null if invalid
        }
    }
}
