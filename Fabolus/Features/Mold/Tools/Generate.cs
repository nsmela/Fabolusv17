using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Tools {
    public static partial class MoldTools {
        public static MeshGeometry3D GenerateMold(MoldShape shape) {
            var mold = BolusUtility.MeshGeometryToDMesh(shape.Geometry as MeshGeometry3D);
            var bolus = BolusUtility.MeshGeometryToDMesh(shape.Bolus.Geometry as MeshGeometry3D);

            //invert bolus mesh and add mold mesh
            var editor = new MeshEditor(bolus);
            editor.ReverseTriangles(bolus.TriangleIndices(), true);
            editor.AppendMesh(mold);

            //boolean subtract air channels
            var channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();

            //result mesh
            return BolusUtility.DMeshToMeshGeometry(editor.Mesh);
        }
    }
}
