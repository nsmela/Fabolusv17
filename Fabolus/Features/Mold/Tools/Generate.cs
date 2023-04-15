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
        public static DMesh3 GenerateMold(ContourBase contour) {
            //grab meshes for the preview mold and the bolus
            var mold = contour.Mesh;

            //invert bolus mesh and add preview mold mesh
            BolusModel bolusModel = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            var bolus = new DMesh3(bolusModel.TransformedMesh); //new because otherwise it'd affect the original mesh
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
