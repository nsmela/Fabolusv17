using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Bolus {
    public static partial class BolusUtility {
        public static float CalculateVolume(DMesh3 mesh) {
            float volume = 0.0f;

            if (mesh == null || mesh.TriangleCount <= 0) return volume;

            Vector3d v0, v1, v2;
            foreach (var triangle in mesh.Triangles()) {
                v0 = mesh.GetVertex(triangle.a);
                v1 = mesh.GetVertex(triangle.b);
                v2 = mesh.GetVertex(triangle.c);

                volume += SignedVolumeOfTriangle(v0, v1, v2);
            }

            return volume;
        }

        public static float CalculateVolume(MeshGeometry3D mesh) {
            float volume = 0.0f;

            if (mesh == null || mesh.TriangleIndices.Count() <= 0) return volume;

            Point3D p1, p2, p3;
            for (int i = 0; i < mesh.TriangleIndices.Count(); i += 3) {
                p1 = mesh.Positions[i];
                p2 = mesh.Positions[i + 1];
                p3 = mesh.Positions[i + 2];

                volume += SignedVolumeOfTriangle(p1, p2, p3);
            }

            return volume;
        }

        public static string VolumeToText(DMesh3 mesh) => string.Format("{0:0,0.0} mL", CalculateVolume(mesh)/1000);
        public static string VolumeToText(MeshGeometry3D mesh) => string.Format("{0:0,0.0} mL", CalculateVolume(mesh)/1000);

        private static float SignedVolumeOfTriangle(Vector3d v0, Vector3d v1, Vector3d v2) {
            var v321 = v2.x * v1.y * v0.z;
            var v231 = v1.x * v2.y * v0.z;
            var v312 = v2.x * v0.y * v1.z;
            var v132 = v0.x * v2.y * v1.z;
            var v213 = v1.x * v0.y * v2.z;
            var v123 = v0.x * v1.y * v2.z;
            return (float)((1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123));
        }

        /// <summary>
        /// calculates volume of a triangle. signed so that negative volumes exist, easing the calculation
        /// </summary>
        private static float SignedVolumeOfTriangle(Point3D p1, Point3D p2, Point3D p3) {
            var v321 = p3.X * p2.Y * p1.Z;
            var v231 = p2.X * p3.Y * p1.Z;
            var v312 = p3.X * p1.Y * p2.Z;
            var v132 = p1.X * p3.Y * p2.Z;
            var v213 = p2.X * p1.Y * p3.Z;
            var v123 = p1.X * p2.Y * p3.Z;
            return (float)((1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123));
        }
    }
}
