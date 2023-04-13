﻿using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Tools;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Contours {
    public class BoxContour : ContourBase {
        public override string Name => "contoured box";
        public override MeshGeometry3D Geometry { get; protected set; }
        public override DMesh3 Mesh { get; protected set; }
        public BolusModel Bolus { get; set; }
        public float OffsetXY { get; set; }
        public float OffsetBottom { get; set; }
        public float OffsetTop { get; set; }
        public float Resolution { get; set; } //size of the cells when doing marching cubes, in mm

        public override void Calculate() {
            Geometry = new MeshGeometry3D();
            Mesh = new DMesh3();
            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;
            var numberOfCells = (int)Math.Ceiling(Bolus.TransformedMesh.CachedBounds.MaxDim / Resolution);
            var offsetMesh = MoldUtility.OffsetMeshD(Bolus.TransformedMesh, OffsetXY);

            Bitmap3 bmp = BolusUtility.MeshBitmap(offsetMesh, numberOfCells);

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = BitmapBox(bmp);
            voxGen.Generate();
            var result = new DMesh3(MoldUtility.MarchingCubesSmoothing(voxGen.Meshes[0], numberOfCells));

            //mesh is small and not aligned
            var scale = offsetMesh.CachedBounds.MaxDim / Resolution;
            MeshTransforms.Scale(result, scale);
            BolusUtility.CentreMesh(result, offsetMesh);

            Mesh = result;
            Geometry = BolusUtility.DMeshToMeshGeometry(Mesh);
        }

        private Bitmap3 BitmapBox(Bitmap3 bmp) {
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
