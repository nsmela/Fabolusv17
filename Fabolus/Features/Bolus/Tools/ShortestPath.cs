using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public bool Visited;
    }

    public partial class BolusModel {
        public int? GetTriangleId(int t1, int t2, int t3) {
            var tId = Mesh.FindTriangle(t1, t2, t3);
            return tId;
        }

        //used for mesh vertices calculations
        private PointHashGrid3d<int> _pointhash;

        private List<Point3D> ShortestPath(Vector3d startPoint, Vector3d endPoint) {
            //uses the Dijkstra-inspired method A* to find the path from the nodes
            //create list of nodes
            var map = new List<Node>();
            foreach(var index in Mesh.VertexIndices()) {
                Vector3d vert = Mesh.GetVertex(index);
                var node = new Node {
                    Id = index,
                    Vertex = vert,
                    DistanceToEnd = vert.Distance(endPoint),
                    DistanceSoFar = 0.0f,
                    ChildrenNodes = GetChildrenNodes(index),
                    Visited = false
                };
                map.Add(node);
                //Debug.WriteLine("Node {0}: index {1} | Dist {2} | Nodes: {3}", index.ToString(), node.Id, node.DistanceToEnd.ToString("0.00"), node.ChildrenNodes.Count().ToString());
            }

            _pointhash = new PointHashGrid3d<int>(Mesh.CachedBounds.MaxDim / 32, -1);
            foreach(var index in Mesh.VertexIndices()) {
                _pointhash.InsertPoint(index, Mesh.GetVertex(index));
            }

            //setting up search variables
            double searchRadius = 40.0f;
            int startVertIndex = FindClosestVertices(startPoint, searchRadius);
            int endVertIndex = FindClosestVertices(endPoint, searchRadius);
            var startNodeIndex = map.FindIndex( n => n.Id == startVertIndex );
            var endNodeIndex = map.FindIndex(n => n.Id == endVertIndex);

            if (startNodeIndex == -1 || endNodeIndex == -1) return null;

            //search
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(map[startNodeIndex]);

            do {
                //sort queue
                queue.OrderBy(n => n.DistanceSoFar + n.DistanceToEnd);
                //grab first node and remove it from the queue
                var parentNode = queue.Dequeue();
                int parentIndex = map.FindIndex(n => n == parentNode);

                //foreach child
                foreach (var childNode in parentNode.ChildrenNodes.OrderBy(t => t.Item2)) {
                    if (map[childNode.Item1].Visited) continue; //skip child node if visited

                    int nodeIndex = childNode.Item1;
                    double nodeDistance = childNode.Item2;
                    
                    //calculate weight = parent.DistanceSoFar + child.Distance
                    double weight = parentNode.DistanceSoFar + nodeDistance;

                    //if nearest to start is null
                    //OR if weight < smallest weight
                    if (map[nodeIndex].DistanceSoFar == 0.0f || parentNode.DistanceSoFar + nodeDistance < map[nodeIndex].DistanceSoFar) {
                        //childnode.DistanceSoFar = weight
                        map[nodeIndex].DistanceSoFar = weight;
                        //childnode.NearestToStartNode = parentIndex(in map)
                        map[nodeIndex].NearestToStartIndex = parentIndex;
                        //if queue does not have this node, add it
                        if (!queue.Contains(map[nodeIndex])) queue.Enqueue(map[nodeIndex]);
                    }
                }
                //parent.Visited = true
                parentNode.Visited = true;
                //if parent equals the end index, exit the algorythm
                if (parentIndex == endNodeIndex) 
                    return GetPathPoints(map, startNodeIndex, endNodeIndex);

            }while (queue.Any());

            return null;//didn't find a path
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
