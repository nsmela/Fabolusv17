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

            var offsetMesh = MoldUtility.OffsetMeshD(Bolus.TransformedMesh, Settings.OffsetXY);
            
            List<AirChannelModel> airchannels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();

            if (airchannels != null && airchannels.Count >= 1) {
                var channels = new MeshEditor(new DMesh3());
                foreach (var a in airchannels) {
                    if (a != null && a.Geometry != null)
                        channels.AppendMesh((a.Shape.MeshOffset(3.2f, (float)(Bolus.Mesh.CachedBounds.Max.z + 10))));
                }
                if (channels.Mesh != null && channels.Mesh.TriangleCount > 3) {
                    var mesh = MoldUtility.OffsetMeshD(channels.Mesh, Settings.OffsetXY);
                    offsetMesh = MoldUtility.BooleanUnion(offsetMesh, mesh);
                }
            }

            var resolution = 2.5f;
            var cells = (int)(offsetMesh.CachedBounds.MaxDim / resolution);
            Bitmap3 bmp = BolusUtility.MeshBitmap(offsetMesh, cells);
            var processedBmp = BitmapExtendedToFloor(bmp);

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = processedBmp;
            voxGen.Generate();
            
            var result = new DMesh3(MoldUtility.MarchingCubesSmoothing(voxGen.Meshes[0], cells));

            //mesh is small and not aligned
            var scale = offsetMesh.CachedBounds.MaxDim / cells;

            MeshTransforms.Scale(result, scale);
            BolusUtility.CentreMesh(result, offsetMesh);

            Geometry = BolusUtility.DMeshToMeshGeometry(result);
            return;
        }

        public override void UpdateMesh() {
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;
        }

        private static Bitmap3 BitmapExtendedToFloor(Bitmap3 bmp, int z_height = 0) {
            int[,,] grid = new int[bmp.Dimensions.x, bmp.Dimensions.y, bmp.Dimensions.z];
            int zTop = bmp.Dimensions.z - 1;

            //check the very top for filled voxels (true or false)
            //-1 if nothing is above, otherwise number is how far from filled voxel above this one
            //to be used later one for more robust calculations
            for (int x = 0; x < bmp.Dimensions.x; x++) {
                for (int y = 0; y < bmp.Dimensions.y; y++) {
                    if (bmp.Get(new Vector3i(x, y, zTop))) grid[x, y, zTop] = 0;
                    else grid[x, y, zTop] = -1;
                }
            }

            //cycles from top to bottom
            //filled if original is filled
            //if not, counts how far from a filled cell above itself
            //-1 if whole cell stack is empty so far
            for (int z = zTop - 1; z >= 0; z--) {
                for (int x = 0; x < bmp.Dimensions.x; x++) {
                    for (int y = 0; y < bmp.Dimensions.y; y++) {
                        Vector3i cell = new Vector3i(x, y, z);
                        bool value = bmp.Get(cell);

                        if (value) grid[x, y, z] = 0; //actively filled
                        else if (grid[x, y, z + 1] > -1) grid[x, y, z] = grid[x, y, z + 1] + 1; //not filled, but how far from a filled cell
                        else grid[x, y, z] = -1; //not filled, no filled cells above
                    }
                }
            }

            //pass the grid results over to a new bitmap
            Bitmap3 result = new Bitmap3(bmp.Dimensions);
            for (int x = 0; x < bmp.Dimensions.x; x++) {
                for (int y = 0; y < bmp.Dimensions.y; y++) {
                    for (int z = 0; z < bmp.Dimensions.z; z++) {
                        bool value = grid[x, y, z] > -1;
                        result.Set(new Vector3i(x, y, z), value);
                    }
                }
            }

            return result;

        }
    }
}
