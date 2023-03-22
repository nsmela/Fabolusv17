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

    public partial class BolusModel {
        public int? GetTriangleId(int t1, int t2, int t3) {
            var tId = Mesh.FindTriangle(t1, t2, t3);
            return tId;
        }

        //used for mesh vertices calculations
        private PointHashGrid3d<int> _pointhash;
        private List<Node> _nodeMap; //used for calculating paths

        private List<Point3D>? ShortestPath(Point3D startPoint, int startTriangle, Point3D endPoint, int endTriangle) {
            //uses the Dijkstra-inspired method A* to find the path from the nodes
            //create/update list of nodes
            GenerateNodeMap(new Vector3d(endPoint.X, endPoint.Y, endPoint.Z));

            //using triangles to get the starting vertex index of the node. Crude, but reliable and quick.
            //TODO: optimize to find the best vertex closest to the end point or start point
            var start = Mesh.GetTriangle(startTriangle).a;
            var end = Mesh.GetTriangle(endTriangle).a;

            if (start < 0 || end < 0) return null; //if cannot find start or end positions, abort

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
            if (Mesh.VertexIndices().Count() <= 0) return;
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
                node.DistanceToEnd = node.Vertex.Distance(endPoint);
                node.DistanceSoFar = 0.0f;
            }
        }
    
        private List<Tuple<int, double>> GetChildrenNodes(int index) {
            var result = new List<Tuple<int, double>>();

            //all vertices linked to a single vert can be found from the triangles linked to that vert
            var children = Mesh.VtxVerticesItr(index);
            foreach(var child in children) {
                if (child == index) continue;
                result.Add(new Tuple<int, double>(child, DistanceTo(index, child)));
            }

            return result;
        }

        private double DistanceTo(int startVertex, int endVertex) {
            Vector3d start = Mesh.GetVertex(startVertex);
            Vector3d end = Mesh.GetVertex(endVertex);

            return start.Distance(end);
        }

        //TODO reuse this? was faulting before, but that couldbe due to not using the transformed mesh
        private int FindClosestVertices(Vector3d point, double searchRadius) {
            //outputs the vid and the distance
            var result = _pointhash.FindNearestInRadius(point, searchRadius,
                (v) => { return point.DistanceSquared(Mesh.GetVertex(v)); });
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


    }
}
