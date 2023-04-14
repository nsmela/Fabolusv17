using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Contours;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Fabolus.Features.Mold.Tools {
    public static partial class MoldUtility {
        public static MeshGeometry3D GenerateMold(MoldShape shape) {
            //grab meshes for the preview mold and the bolus
            var mold = BolusUtility.MeshGeometryToDMesh(shape.Geometry as MeshGeometry3D);
            var bolus = BolusUtility.MeshGeometryToDMesh(shape.Bolus.Geometry as MeshGeometry3D);

            //invert bolus mesh and add preview mold mesh
            var editor = new MeshEditor(bolus);
            editor.ReverseTriangles(bolus.TriangleIndices(), true);
            editor.AppendMesh(mold);

            //boolean subtract air channels
            DMesh3 channels = WeakReferenceMessenger.Default.Send<AirChannelMeshRequestMessage>();
            var mesh = BooleanSubtraction(editor.Mesh, channels);

            //result mesh
            return BolusUtility.DMeshToMeshGeometry(mesh);
        }

        public static DMesh3 GenerateMold(ContourBase contour) {
            //grab meshes for the preview mold and the bolus
            DMesh3 mold = contour.Mesh;

            //invert bolus mesh and add preview mold mesh
            BolusModel bolusModel = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            DMesh3 bolus = bolusModel.TransformedMesh;
            var editor = new MeshEditor(bolus);
            editor.ReverseTriangles(bolus.TriangleIndices(), true);
            editor.AppendMesh(mold);

            //boolean subtract air channels
            DMesh3 channels = WeakReferenceMessenger.Default.Send<AirChannelMeshRequestMessage>();
            var mesh = BooleanSubtraction(editor.Mesh, channels);

            //result mesh
            return mesh;
        }
    }
}
