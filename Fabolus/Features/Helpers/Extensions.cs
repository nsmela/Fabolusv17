using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Helpers { 
    public static class Extensions {
        public static Vector3d ToVector3d(this Vector3D vector) => new Vector3d(vector.X, vector.Y, vector.Z);
        public static Vector3d ToVector3d(this Point3D point) => new Vector3d(point.X, point.Y, point.Z);
        public static MeshGeometry3D ToGeometry(this DMesh3 mesh) {
            if (mesh != null) {
                //compacting the DMesh to the indices are true
                MeshGeometry3D geometry = new();

                //calculate positions and normals
                var vertices = mesh.Vertices();
                foreach (var vert in vertices) {
                    geometry.Positions.Add(new Point3D(vert.x, vert.y, vert.z));
                }

                //calculate faces
                var vID = mesh.VertexIndices().ToArray();
                var faces = mesh.Triangles();
                foreach (Index3i f in faces) {
                    geometry.TriangleIndices.Add(Array.IndexOf(vID, f.a));
                    geometry.TriangleIndices.Add(Array.IndexOf(vID, f.b));
                    geometry.TriangleIndices.Add(Array.IndexOf(vID, f.c));
                }

                geometry.Normals = MeshGeometryHelper.CalculateNormals(geometry);

                return geometry;
            } else
                return new MeshGeometry3D();
        }

        public static DMesh3 ToDMesh(this MeshGeometry3D geometry) {
            List<Vector3d> vertices = new();
            foreach (Point3D point in geometry.Positions)
                vertices.Add(new Vector3d(point.X, point.Y, point.Z));

            List<Vector3f> normals = new();
            foreach (Point3D normal in geometry.Normals)
                normals.Add(new Vector3f(normal.X, normal.Y, normal.Z));

            if (normals.Count == 0)
                normals = null;

            List<Index3i> triangles = new();
            for (int i = 0; i < geometry.TriangleIndices.Count; i += 3)
                triangles.Add(new Index3i(geometry.TriangleIndices[i], geometry.TriangleIndices[i + 1], geometry.TriangleIndices[i + 2]));

            //converting the meshes to use Implicit Surface Modeling
            return DMesh3Builder.Build(vertices, triangles, normals);
        }
    }
}
