using Fabolus.Features.Bolus;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Tools {
    public static partial class MoldUtility {
        public static DMesh3 OffsetMeshD(DMesh3 mesh, double offset, int resolution = 64) {
            BoundedImplicitFunction3d meshImplicit = meshToImplicitF(mesh, resolution, offset);
            return generatMeshF(new ImplicitOffset3d() { A = meshImplicit, Offset = offset }, resolution);

        }

        public static MeshGeometry3D OffsetMesh(DMesh3 mesh, double offset, int resolution = 64) {
            DMesh3 offSesh = OffsetMeshD(mesh, offset, resolution);
            return BolusUtility.DMeshToMeshGeometry(offSesh);
        }
    }
}
