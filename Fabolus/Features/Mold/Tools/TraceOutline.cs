using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Tools {
    public static partial class MoldTools {

        public static List<Point3D> TraceOutline(MeshGeometry3D mesh, Vector3D refAxis) {
            //start point

            //centre point

            //start from left

            //next id

            //break count?

            //get list of edges

            //arrange list left to right, top to bottom

            //out points

            //while loop
            //increment
            //if length is 0, skip?
            //using vector a, check vector b for the next nearest clockwise
            //if next vector equals start id, break



            //until implemented correctly, will only return the bounding box
            var boundry = mesh.Bounds;

            List<Point3D> points = new();
            var bottomOrigin = new Point3D(boundry.X, boundry.Y,boundry.Z);
            points.Add(bottomOrigin);
            points.Add(new Point3D(bottomOrigin.X, bottomOrigin.Y + boundry.SizeY, bottomOrigin.Z));
            points.Add(new Point3D(bottomOrigin.X + boundry.SizeX, bottomOrigin.Y + boundry.SizeY, bottomOrigin.Z));
            points.Add(new Point3D(bottomOrigin.X + boundry.SizeX, bottomOrigin.Y, bottomOrigin.Z));

            return points;
        }

        public static List<Point3D> MeshContour(DMesh3 mesh) {
            var refNormal = Vector3d.AxisZ;

            var result = new DMesh3(mesh);

            //remove any vtx who's normal isn't positive dot to the ref axis
            for(int i = 0; i < mesh.VertexCount; i++) {
                if (Vector3d.Dot(refNormal, mesh.GetVertexNormal(i)) < 0)
                    result.RemoveVertex(i);
            }

            //remove any edge that isn't a boundry
            int edgeCount = result.EdgeCount;
            Index2i tris;
            for(int i = 0; i < edgeCount; i++) {
                tris = result.GetEdgeOpposingV(i);
                if (tris.b != DMesh3.InvalidID)
                    result.RemoveTriangle(tris.b);
            }

            var loop = new EdgeLoop(result);

            bool isBoundry = loop.IsBoundaryLoop();

            var points = new List<Point3D>();
            var vert = new Vector3d();
            foreach(var e in loop.Vertices) {
                vert = loop.GetVertex(e);
                points.Add(new Point3D(vert.x, vert.y, vert.z));
            }
            return points;








            //get visible triangles by calculating the dot product between the triangle face and the reference angle. Positive results are visible triangles
            var visibleTriangles = new List<int>(); 
            for(int i = 0; i < mesh.TriangleCount; i++) {
                if (Vector3d.Dot(refNormal, mesh.GetTriNormal(i)) > 0) {
                    visibleTriangles.Add(i);
                }
            }

            //convert those visible triangles into edges
            //collect the edge IDs
            //remove the edge id if one or more exist
            //edges must have two and only two triangles, so this should be efficient
            var visibleEdges = new HashSet<int>();
            for(int i = 0; i < visibleTriangles.Count; i++) { 
                var edge = mesh.GetTriEdges(i).array;
                foreach(var e in edge) {
                    if(visibleEdges.Contains(e)) visibleEdges.Remove(e);
                    else visibleEdges.Add(e);
                }
            }

            //create a list of points 
            //starts with the first edge, gets the next point
            //gets the next edge sharing that point and gets the next point, etc

            //use EdgeLoop FromEdges?



            var result = new List<Point3D>();
            int index = visibleEdges.ElementAt(0);//first edge ID
            int startID = mesh.GetEdgeV(index).a; //first vert ID
            int nextVert = startID;
            while (true) {
                //get next edge
                var edge = mesh.Get
                //get the vert not already stored

                Index4i edge = mesh.GetEdge(index);
                if(edge.a == nextVert) nextVert = edge.b;
                else nextVert = edge.a;
                mesh.Vtx
                var vert = mesh.GetVertex(nextVert);
                result.Add(new Point3D(vert.x, vert.y, vert.z));

                if (startID == index) break;
                
            } 

        }
    }
}
