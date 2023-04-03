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
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            var airHole = new MeshEditor(new DMesh3());
            foreach(var channel in channels) 
                if(channel.Geometry!= null) airHole.AppendMesh(BolusUtility.MeshGeometryToDMesh(channel.Geometry)); //some reason, first channel is null
            var mesh = BooleanSubtraction(editor.Mesh, airHole.Mesh);

            //result mesh
            return BolusUtility.DMeshToMeshGeometry(mesh);
        }
    }
}
