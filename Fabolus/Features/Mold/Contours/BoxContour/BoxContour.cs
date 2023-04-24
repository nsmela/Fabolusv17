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
            //TODO: calculate air channels and bolus offset mesh at the same time with multithreading
            //each set up as a task and output the result
            //continue when both are done
            Geometry = new MeshGeometry3D();
            Mesh = new DMesh3();

            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            if (bolus == null || bolus.Mesh == null || bolus.Mesh.VertexCount == 0) return;
            var numberOfCells = (int)Math.Ceiling(bolus.TransformedMesh.CachedBounds.MaxDim / Resolution);
            var maxBolusHeight = (float)(bolus.Geometry.Bounds.Z + bolus.Geometry.Bounds.SizeZ);
            var maxHeight = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            var heightOffset = maxHeight - (maxBolusHeight + OffsetTop);

            //tasks
            var task1 = Task.Run(() => GetOffsetMesh(bolus, OffsetXY));
            var task2 = Task.Run(() => GetOffsetAirChannels(numberOfCells, OffsetXY, heightOffset));

            Task.WaitAll(task1, task2); 

            var offsetMesh = MoldUtility.BooleanUnion(task1.Result, task2.Result);

            Bitmap3 bmp = BolusUtility.MeshBitmap(offsetMesh, numberOfCells); //another huge time sink

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = BitmapBox(bmp);
            voxGen.Generate();
            var result = new DMesh3(MoldUtility.MarchingCubesSmoothing(voxGen.Meshes[0], numberOfCells));

            //mesh is small and not aligned
            var scale = offsetMesh.CachedBounds.MaxDim / numberOfCells;
            MeshTransforms.Scale(result, scale);
            BolusUtility.CentreMesh(result, offsetMesh);
            
            Mesh = result;
            Geometry = Mesh.ToGeometry();

            //testing
            var timer = new Stopwatch();
            timer.Start();
            var text = "testing contour timing:\r\n";
            var contour = GetContour(offsetMesh);

            text += $" contour: {timer.ElapsedMilliseconds}";
            timer.Stop();


        }

        private DMesh3 GetOffsetMesh(BolusModel bolus, float offset) => MoldUtility.OffsetMeshD(bolus.TransformedMesh, offset);
        
        private DMesh3 GetOffsetAirChannels(int numberOfCells, float offset, float heightOffset) {
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();

            if (channels == null || channels.Count <= 0) return new DMesh3();

            var airHole = new MeshEditor(new DMesh3());
                //multithreading
                var airMeshes = new ConcurrentBag<DMesh3>();
                Parallel.ForEach(channels, channel => {
                    if (channel.Geometry is null) return;
                    airMeshes.Add(channel.Shape.OffsetMesh(offset, heightOffset));
                });
                foreach (var m in airMeshes) airHole.AppendMesh(m); //this is a major hurdle?

                return airHole.Mesh;
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
                }
            }

            return bmp;
        }

        private List<Vector3d> GetContour(DMesh3 mesh, float resolution = 1.0f, int padding = 2) {
            if (mesh is null) return null;

            var spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            //try a hit test for each cube?
            var z = mesh.CachedBounds.Max.z + 2.0f; //where hit tests will start
            var min = new Vector2d(mesh.CachedBounds.Min.x, mesh.CachedBounds.Min.y);
            var max = new Vector2d(mesh.CachedBounds.Max.x, mesh.CachedBounds.Max.y);

            var dimensions = new Index2i(
                (int)((max.x - min.x) /resolution) + padding * 2, //for the negative and plus side of x
                (int)((max.y - min.y) / resolution) + padding * 2
                );

            var map = new bool[dimensions.a, dimensions.b]; //stores the results of hits

            //hit tests

            var hitRay = new Ray3d(Vector3d.Zero, new Vector3d(0, 0, -1));
            for(int x = 0; x < dimensions.a; x++) {
                var xSet = min.x + 0.5f + (x * resolution);
                for(int y = 0; y < dimensions.b; y++) {
                    var ySet = min.y + 0.5f + (y * resolution);
                    hitRay.Origin = new Vector3d(xSet, ySet, z);

                    var hit = spatial.FindNearestHitTriangle(hitRay);

                    if (hit != DMesh3.InvalidID) map[x, y] = true;
                }
            }

            //use map to make a contour

            return null;
        }
}
    }
