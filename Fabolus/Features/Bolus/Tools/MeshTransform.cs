using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Bolus {
    public static partial class BolusUtility {
        public static DMesh3 OrientationCentre(DMesh3 mesh) {
            double x = mesh.CachedBounds.Center.x * -1;
            double y = mesh.CachedBounds.Center.y * -1;
            double z = mesh.CachedBounds.Center.z * -1;
            MeshTransforms.Translate(mesh, x, y, z);
            return mesh;
        }

        public static DMesh3 MoveToCentre(DMesh3 mesh, DMesh3 target) {
            double x = mesh.CachedBounds.Center.x * -1;
            double y = mesh.CachedBounds.Center.y * -1;
            double z = mesh.CachedBounds.Center.z * -1;
            MeshTransforms.Translate(mesh, x, y, z);
            return mesh;
        }

        public static void CentreMesh(DMesh3 mesh, DMesh3 originalMesh) {
            //positioning the mesh ontop of the old one
            double x = originalMesh.CachedBounds.Center.x - mesh.CachedBounds.Center.x;
            double y = originalMesh.CachedBounds.Center.y - mesh.CachedBounds.Center.y;
            double z = originalMesh.CachedBounds.Center.z - mesh.CachedBounds.Center.z;
            MeshTransforms.Translate(mesh, x, y, z);
        }
    }
}
