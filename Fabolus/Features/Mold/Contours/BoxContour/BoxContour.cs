using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Tools;
using g3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Fabolus.Features.Helpers;
using System.Collections.Concurrent;

namespace Fabolus.Features.Mold.Contours {
    public class BoxContour : ContourBase {
        public override string Name => "contoured box";
        public float OffsetXY { get; set; }
        public float OffsetBottom { get; set; }
        public float OffsetTop { get; set; }
        public float Resolution { get; set; } //size of the cells when doing marching cubes, in mm

        public override void Calculate() {
            var timer = new Stopwatch();
            timer.Start();
            string text = "Calculating box contour: \r\n";
            Geometry = new MeshGeometry3D();
            Mesh = new DMesh3();
            text += $"  istantiated variables: {timer.ElapsedMilliseconds}\r\n";
            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            text += $"  incoming message: {timer.ElapsedMilliseconds}\r\n";
            if (bolus == null || bolus.Mesh == null || bolus.Mesh.VertexCount == 0) return;
            var numberOfCells = (int)Math.Ceiling(bolus.TransformedMesh.CachedBounds.MaxDim / Resolution);
            var offsetMesh = MoldUtility.OffsetMeshD(bolus.TransformedMesh, OffsetXY);
            text += $"  created offset mesh: {timer.ElapsedMilliseconds}\r\n";
            timer.Reset();
            timer.Start();

            //add air channels as offset mesh 
            var maxBolusHeight = (float)(bolus.Geometry.Bounds.Z + bolus.Geometry.Bounds.SizeZ);
            float maxZHeight = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            var heightOffset = maxZHeight + OffsetTop - (maxBolusHeight);
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();

            if (channels != null && channels.Count > 0) {
                var airHole = new MeshEditor(new DMesh3());
                //foreach (var channel in channels)
                //if (channel.Geometry != null) 
                //airHole.AppendMesh(channel.Shape.OffsetMesh(OffsetXY, heightOffset)); //some reason, first channel is null

                //multithreading
                var airMeshes = new ConcurrentBag<DMesh3>();
                Parallel.ForEach(channels, channel => {
                    if (channel.Geometry is null) return;
                    airMeshes.Add(channel.Shape.OffsetMesh(OffsetXY, heightOffset));
                });
                foreach (var m in airMeshes) airHole.AppendMesh(m); //this is a major hurdle?
                
                offsetMesh = MoldUtility.BooleanUnion(offsetMesh, airHole.Mesh);
            }
            text += $"  consulted air channels: {timer.ElapsedMilliseconds}\r\n";
            timer.Reset();
            timer.Start();

            Bitmap3 bmp = BolusUtility.MeshBitmap(offsetMesh, numberOfCells); //another huge time sink
            text += $"  bitmap made: {timer.ElapsedMilliseconds}\r\n";
            timer.Reset();
            timer.Start();

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = BitmapBox(bmp);
            voxGen.Generate();
            var result = new DMesh3(MoldUtility.MarchingCubesSmoothing(voxGen.Meshes[0], numberOfCells));
            text += $"  voxel mesh: {timer.ElapsedMilliseconds}\r\n";

            //mesh is small and not aligned
            var scale = offsetMesh.CachedBounds.MaxDim / numberOfCells;
            MeshTransforms.Scale(result, scale);
            BolusUtility.CentreMesh(result, offsetMesh);

            Mesh = result;
            Geometry = Mesh.ToGeometry();
            //MessageBox.Show(text); //for testing
        }

        private Bitmap3 BitmapBox(Bitmap3 bmp) { //~71 ms
            int[,,] grid = new int[bmp.Dimensions.x, bmp.Dimensions.y, bmp.Dimensions.z];
            bool[,] whitelist = new bool[bmp.Dimensions.x, bmp.Dimensions.y]; //optimize searching by ignoring vacant spaces

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
                            whitelist[x, y] = true;
                        }
                    }
                }
            }

            //todo: create a blacklist of x,y coordinates that have nothing?


            //if an airhole is too low, this will extend the mesh from the lowest airhole up to the top.
            //the 3D print will be easier to fill
            for (int x = 0; x < bmp.Dimensions.x; x++) {
                for (int y = 0; y < bmp.Dimensions.y; y++) {
                    if (!whitelist[x, y]) continue;
                    //multithreading
                    Parallel.For(z_bottom, z_top, z => {
                        if (bmp.Get(new Vector3i(x, y, z))) {
                            for (int j = z_bottom; j <= z_top; j++) {
                                bmp.Set(new Vector3i(x, y, j), true);
                            }
                            return;
                        }
                    }); 


                    /*
                    for (int z = z_bottom; z <= z_top; z++) {
                        if (bmp.Get(new Vector3i(x, y, z))) {
                            for (int j = z_bottom; j <= z_top; j++) {
                                bmp.Set(new Vector3i(x, y, j), true);
                            }
                            break;
                        }
                    }*/
                }
            }

            return bmp;
        }
    }
}
