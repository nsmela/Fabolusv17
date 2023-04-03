using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Tools;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Shapes {
    public class MoldRisingContour : MoldShape {
        public override string Name => "rising contour";

        public MoldRisingContour(MoldStore.MoldSettings settings, BolusModel bolus = null) {
            Bolus = bolus;
            Settings = settings;
        }

        public override void ToMesh() {
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;

            var offsetMesh = MoldTools.OffsetMeshD(Bolus.TransformedMesh, Settings.OffsetXY);
            
            List<AirChannelModel> airchannels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();

            Bitmap3 bmp = BolusUtility.MeshBitmap(offsetMesh, Settings.Resolution);

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            //voxGen.Voxels = BitmapBox(bmp);
            voxGen.Generate();
            var result = new DMesh3(MoldTools.MarchingCubesSmoothing(voxGen.Meshes[0], Settings.Resolution));

            //mesh is small and not aligned
            var scale = offsetMesh.CachedBounds.MaxDim / Settings.Resolution;
            MeshTransforms.Scale(result, scale);
            BolusUtility.CentreMesh(result, offsetMesh);

            Geometry = BolusUtility.DMeshToMeshGeometry(result);
            return;
        }

        public override void UpdateMesh() {
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;
        }
    }
}
