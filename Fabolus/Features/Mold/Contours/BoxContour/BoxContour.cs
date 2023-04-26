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
using System.CodeDom;
using gs;
using HelixToolkit.Wpf;
using TriangleNet.Geometry;
using TriangleNet.Tools;
using TriangleNet.Meshing;

namespace Fabolus.Features.Mold.Contours {
    public class BoxContour : ContourBase {
        public override string Name => "contoured box";
        private BolusModel _bolus;

        public float OffsetXY { get; set; }
        public float OffsetBottom { get; set; }
        public float OffsetTop { get; set; }
        public float Resolution { get; set; } //size of the cells when doing marching cubes, in mm
       
        public override void Calculate() {
            var timer = new Stopwatch();
            var text = "Starting BoxContour.Calculate timer!\r\n";
            timer.Start();

            Geometry = new MeshGeometry3D();
            Mesh = new DMesh3();

            //variables to use
            _bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            if (_bolus == null || _bolus.Mesh == null || _bolus.Mesh.VertexCount == 0) return;
            var numberOfCells = (int)Math.Ceiling(_bolus.TransformedMesh.CachedBounds.MaxDim / Resolution);
            var maxBolusHeight = (float)(_bolus.Geometry.Bounds.Z + _bolus.Geometry.Bounds.SizeZ);
            var maxHeight = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            var heightOffset = maxHeight - (maxBolusHeight + OffsetTop);

            //tasks
            var task1 = Task.Run(() => GetOffsetMesh(_bolus, OffsetXY));
            var task2 = Task.Run(() => GetOffsetAirChannels(numberOfCells, OffsetXY, heightOffset));

            Task.WaitAll(task1, task2);

            var offsetMesh = MoldUtility.BooleanUnion(task1.Result, task2.Result, 64);

            Geometry = CalculateContour(offsetMesh);
            Mesh = Geometry.ToDMesh();

        }

        private DMesh3 CalculateByMesh(DMesh3 offsetMesh, int numberOfCells) {
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

            return result;
        }

        private MeshGeometry3D CalculateContour(DMesh3 mesh) {
            var contour = GetContour(mesh, Resolution);
            //if (contour.Count % 2 != 0) contour.Add(contour[0]);

            var zHeight = (float)(_bolus.Geometry.Bounds.SizeZ + _bolus.Geometry.Bounds.Z) + OffsetTop;
            var zBottom = contour[0].z - OffsetBottom;

            var verts = new List<Vertex>();
            contour.ForEach(v => { verts.Add(new Vertex(v.x, v.y)); });
            var polygon = new TriangleNet.Geometry.Polygon();
            var outline = new Contour(verts);
            
            polygon.Add(outline);

            var builder = new MeshBuilder(false, false);
            foreach(var t in new GenericMesher().Triangulate(polygon).Triangles) {
                var v0 = t.GetVertex(0);
                var v1 = t.GetVertex(1);
                var v2 = t.GetVertex(2);

                var p0 = new Point3D(v0.X, v0.Y, zBottom);
                var p1 = new Point3D(v1.X, v1.Y, zBottom);
                var p2 = new Point3D(v2.X, v2.Y, zBottom);

                builder.AddTriangle(p0, p1, p2);

                p0 = new Point3D(v0.X, v0.Y, zHeight);
                p1 = new Point3D(v1.X, v1.Y, zHeight);
                p2 = new Point3D(v2.X, v2.Y, zHeight);

                builder.AddTriangle(p2, p1, p0);
            }

            var lowerVerts = new List<Point3D>();
            contour.ForEach(v => lowerVerts.Add(new Point3D(v.x, v.y, zBottom)));
            var upperVerts = new List<Point3D>();
            verts.ForEach(p => upperVerts.Add(new Point3D(p.X, p.Y, zHeight)));

            //sides of the contour
            int count = verts.Count;
            for (int i = 0; i < verts.Count - 1; i++) {
                var p1 = lowerVerts[i];
                var p2 = lowerVerts[i + 1];
                var p3 = upperVerts[i];
                var p4 = upperVerts[i + 1];

                builder.AddQuad(p4, p2, p1, p3);
            }

            return builder.ToMesh();
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

        private List<Vector3d> GetContour(DMesh3 mesh, float resolution = 1.0f, int padding = 3) {
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

            //hit tests to create the boolean map
            var hitRay = new Ray3d(Vector3d.Zero, new Vector3d(0, 0, -1));
            var points = new List<System.Windows.Point>();
            int minX = dimensions.a, minY = dimensions.b;
            for (int x = 0; x < dimensions.a; x++) {
                var xSet = min.x + 0.5f + (x * resolution);
                if (x < minX) minX = x;

                for (int y = 0; y < dimensions.b; y++) {
                    var ySet = min.y + 0.5f + (y * resolution);
                    hitRay.Origin = new Vector3d(xSet, ySet, z);

                    var hit = spatial.FindNearestHitTriangle(hitRay);

                    if (hit == DMesh3.InvalidID) continue;
                    
                    map[x, y] = true;

                    //used to start the contour

                    if(x == minX && y < minY) minY = y;
                }
            }

            //use map to make a contour
            var bottom_z = mesh.CachedBounds.Min.z - OffsetBottom + 4.0f;

            var startPoint = new Vector2i(minX, minY);
            var currentPoint = startPoint;
            var nextPoint = startPoint;
            var contour = new List<Vector3d>();

            //track the direction around the mold
            //if subsequent points follw the same direction, no need to add them
            //reduces the contour's points to those only needed
            var direction = GridPosition(0, 1);

            while (true) {
                //currentPoint = nextPoint;
                var nextDirection = NextDirection(map, currentPoint, direction);

                //only add to contour if the new direction is different
                if (nextDirection != direction) contour.Add(new Vector3d(
                    (currentPoint.x + 0.5f) * resolution + min.x,
                    (currentPoint.y + 0.5) * resolution + min.y,
                    bottom_z));

                direction= nextDirection;
                currentPoint += GridPosition(direction);

                //test to see if we should exit the loop
                if (currentPoint == startPoint) {
                    contour.Add(contour[0]);//link the last point to the first point
                    break;
                }
            }

            return contour;
        }

        private Vector2i GetNextPoint(bool[,] map, Vector2i currentPoint, Vector2i lastPoint) {
            //double check current point and last point are valid
            int x = currentPoint.x;
            int y = currentPoint.y;
            
            int lastX = lastPoint.x;
            int lastY = lastPoint.y;    

            //starting with last point, go around the current point counter clockwise until a filled point is hit
            var position = NextClockwise(new Vector2i((lastX - x), (lastY - y)));
            int pX, pY;
            for (int i = 0; i < 8; i++) {
                if (position == null) return new Vector2i(); //invalid value
                if (position.Value == lastPoint) return new Vector2i(); //went all the way around and didn't find anything

                pX = position.Value.x + currentPoint.x; 
                pY = position.Value.y + currentPoint.y;

                if (pX >= 0 && pY >= 0 && map[pX, pY]) return new Vector2i(pX, pY); //found the correct position

                position = NextClockwise(position.Value);
            }

            //didn't find anything
            return new Vector2i();
        }

        /// <summary>
        /// Using GridPosition, determine the next spot on the grid
        /// </summary>
        /// <param name="map"></param>
        /// <param name="currentPoint"></param>
        /// <param name="lastPoint">int value that represents the direction to go</param>
        /// <returns></returns>
        private int NextDirection(bool[,] map, Vector2i currentPoint, int direction) {
            int x = currentPoint.x;
            int y = currentPoint.y;

            //invert the value to start from the last position
            Vector2i pVector = GridPosition(direction);
            pVector *= -1;
            var position = GridPosition(pVector);
            int pX, pY;

            //cycle clockwise until a new spot is hit
            for(int i = 0; i < 9; i++) {
                position++;
                if (position > 8) position = 1;
                
                pVector = GridPosition(position);
                pX = x + pVector.x; 
                pY = y + pVector.y;

                //check if map is true or false at this position
                if (pX >= 0 &&  pY >= 0 && map[pX, pY]) return position;
                
            }

            return -1;
        }

        /// <summary>
        /// Take a Vector2i with values between -1 and 1 for the x and y
        /// Goes clockwise around 0,0 to find the next position
        /// </summary>
        /// <param name="position"></param>
        /// <returns>The next Vector2i clockwise around 0,0</returns>
        private Vector2i? NextClockwise(Vector2i position) {
            int x = position.x;
            int y = position.y;

            //simple yet effective 
            if (x == -1) {
                if (y == -1) return new Vector2i(-1, 0);
                if (y == 0) return new Vector2i(-1, 1);
                if (y == 1) return new Vector2i(0, 1);
            }
            if (x == 0) {
                if (y == -1) return new Vector2i(-1, -1);
                if (y == 0) return new Vector2i(0, -1); //sent the current point, which means we need some point to start. this is most likely to be empty
                if (y == 1) return new Vector2i(1, 1);
            }
            if (x == 1) {
                if (y == -1) return new Vector2i(0, -1);
                if (y == 0) return new Vector2i(1, -1);
                if (y == 1) return new Vector2i(1, 0);
            }

            //invalid entry
            return null;
        }

        private static int GridPosition(Vector2i position) => GridPosition(position.x, position.y);
        
        private static int GridPosition(int x, int y) {
            if (x == -1) {
                if (y == -1) return 7;
                if (y == 0) return 8;
                if (y == 1) return 1;
            }
            if (x == 0) {
                if (y == -1) return 6;
                if (y == 0) return 9; //sent the current point, which means we need some point to start. this is most likely to be empty
                if (y == 1) return 2;
            }
            if (x == 1) {
                if (y == -1) return 5;
                if (y == 0) return 4;
                if (y == 1) return 3;
            }

            return 0;
        }

        private static Vector2i GridPosition(int position) {
            switch (position) {
                case 1: return new Vector2i(-1, 1);
                case 2: return new Vector2i(0, 1);
                case 3: return new Vector2i(1, 1);
                case 4: return new Vector2i(1, 0);
                case 5: return new Vector2i(1, -1);
                case 6: return new Vector2i(0, -1);
                case 7: return new Vector2i(-1, -1);
                case 8: return new Vector2i(-1, 0);
                default: return new Vector2i(0, 0);
            }
        }

 
    }
}
