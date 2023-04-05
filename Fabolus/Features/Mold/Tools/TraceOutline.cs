using g3;
using HelixToolkit.Wpf;
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
            var result = new DMesh3(mesh);
            foreach(var triangle in mesh.TriangleIndices()) {
                if(mesh.GetTriNormal(triangle).z > 0) continue; //filter out the good triangles
                result.RemoveTriangle(triangle);
            }

            var edges = new HashSet<int>();
            var pointSet = new List<Point3D>();

            foreach(var edge in result.Edges()) {
                if (edge.d != DMesh3.InvalidID) continue; //not a boundry edge
                var vertf = result.GetVertexf(edge.a);
                pointSet.Add(new Point3D(vertf.x, vertf.y, -200));
            }

            return pointSet;

            //var edges = new HashSet<int>();

            //remove any vtx who's normal isn't positive dot to the ref axis
            for(int i = 0; i < mesh.EdgeCount; i++) {
                var normal = mesh.GetEdgeNormal(i);
                if (normal.z > 0) { //if the z on the normal is positive, it means the edge is visible
                    var edgeTriangles = mesh.GetEdgeOpposingV(i);
                    var triA = mesh.GetTriNormal(edgeTriangles.a);
                    var triB = mesh.GetTriNormal(edgeTriangles.b);

                    //if one of the two triangles has a negative normal, this edge is a boundry
                    if (triA.z < 0 && triB.z < 0) continue;
                    if (triA.z > 0 && triB.z > 0) continue;
                    
                    edges.Add(i);
                }
            }

            var points = new List<Point3D>();
            var index = new Index2i();
            var vert = new Vector3d();
            foreach(var i in edges) {
                index = mesh.GetEdgeV(i);
                vert = mesh.GetVertex(index.a);
                points.Add(new Point3D(vert.x, vert.y, -200));
            }
            return points;

        }

        public static MeshGeometry3D ContourMesh(DMesh3 mesh) {
            if(mesh == null) return new MeshGeometry3D();

            var edgeBoundry = MeshContour(mesh);

            var editor = new MeshBuilder(true);
            foreach(var p in edgeBoundry) {
                editor.AddSphere(p);
            }

            return editor.ToMesh();
        }
    }
}
