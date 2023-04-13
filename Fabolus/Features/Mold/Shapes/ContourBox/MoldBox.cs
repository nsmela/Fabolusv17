using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Tools;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Shapes
{
    public class MoldBox : MoldShape {
        public override string Name => "contoured box";
        public float OffsetXY { get; set; } 
        public float OffsetBottom { get; set; }
        public float OffsetTop { get; set; }
        public float Resolution { get; set; } //size of the cells when doing marching cubes, in mm

        public MoldBox(float offsetXY, float offsetBottom, float offsetTop, float resolution, BolusModel bolus = null) {
            Bolus = bolus;

            OffsetXY = offsetXY;
            OffsetBottom = offsetBottom;
            OffsetTop = offsetTop;
            Resolution = resolution;
        }

        public override void ToMesh() {
            /*if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;

            var offsetMesh = MoldUtility.OffsetMeshD(Bolus.TransformedMesh, OffsetXY);

            Bitmap3 bmp = BolusUtility.MeshBitmap(offsetMesh, Resolution);

            //TODO: use Offet top and bottom on BMP to increase the size of the mesh if needed

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = BitmapBox(bmp);
            voxGen.Generate();
            var result = new DMesh3(MoldUtility.MarchingCubesSmoothing(voxGen.Meshes[0], Resolution));

            //mesh is small and not aligned
            var scale = offsetMesh.CachedBounds.MaxDim / Resolution;
            MeshTransforms.Scale(result, scale);
            BolusUtility.CentreMesh(result, offsetMesh);

            Geometry = BolusUtility.DMeshToMeshGeometry(result);*/
            return;
        }

        public override void UpdateMesh() {
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;


        }

        private List<Point3D> OffsetContour(DMesh3 mesh) {
            //get offset mesh
            var offsetMesh = new DMesh3();//MoldUtility.OffsetMeshD(mesh, OffsetXY, Resolution);

            //get bitmap for that offset
            var bitmap = BolusUtility.MeshBitmap(offsetMesh, Resolution);

            //convert it into a box bitmap
            var bmp = BitmapBox(bitmap);

            //get points around bitmap


            return new List<Point3D>();
        }

        static Bitmap3 BitmapBox(Bitmap3 bmp) {
            int[,,] grid = new int[bmp.Dimensions.x, bmp.Dimensions.y, bmp.Dimensions.z];

            //getting the top and bottoms
            int z_bottom = 0;
            for (int z = 0; z < bmp.Dimensions.z; z++) {
                if (z_bottom != 0) break;

                for (int x = 0; x < bmp.Dimensions.x; x++) {
                    if (z_bottom != 0) break;

                    for (int y = 0; y < bmp.Dimensions.y; y++) {
                        if (bmp.Get(new Vector3i(x, y, z))) {
                            z_bottom = z;
                            break;
                        }
                    }
                }

            }

            int z_top = z_bottom;

            for (int x = 0; x < bmp.Dimensions.x; x++) {
                for (int y = 0; y < bmp.Dimensions.y; y++) {
                    for (int z = z_bottom; z < bmp.Dimensions.z; z++) {
                        if (bmp.Get(new Vector3i(x, y, z))) {
                            if (z > z_top) z_top = z;
                        }
                    }
                }
            }

            //if an airhole is too low, this will extend the mesh from the lowest airhole up to the top.
            //the 3D print will be easier to fill
            for (int x = 0; x < bmp.Dimensions.x; x++) {
                for (int y = 0; y < bmp.Dimensions.y; y++) {
                    for (int z = z_bottom; z <= z_top; z++) {
                        if (bmp.Get(new Vector3i(x, y, z))) {
                            for (int j = z_bottom; j <= z_top; j++) {
                                bmp.Set(new Vector3i(x, y, j), true);
                            }
                            break;
                        }
                    }
                }
            }


            return bmp;
        }

    }
}
