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
            //TODO: calculate air channels and bolus offset mesh at the same time with multithreading
            //each set up as a task and output the result
            //continue when both are done
            Geometry = new MeshGeometry3D();
            Mesh = new DMesh3();

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

            var offsetMesh = MoldUtility.BooleanUnion(task1.Result, task2.Result);

            //Mesh = CalculateByMesh(offsetMesh, numberOfCells);
            //Geometry = Mesh.ToGeometry();

            //Geometry = CalculateByContour(offsetMesh, numberOfCells);
            //Mesh = Geometry.ToDMesh();

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

        private MeshGeometry3D CalculateByContour(DMesh3 offsetMesh, int numberOfCells) {
            //testing
            var contour = GetContour(offsetMesh);

            var verts = new List<Point3D>();
            var points = new List<System.Windows.Point>();
            contour.ForEach(v => {
                verts.Add(new Point3D(v.x, v.y, v.z));
                points.Add(new System.Windows.Point(v.x, v.y));
            });

            //using helix toolkit instead
            var builder = new MeshBuilder(false, false);

            var maxBolusHeight = (float)(_bolus.Geometry.Bounds.Z + _bolus.Geometry.Bounds.SizeZ);

            var upperVerts = new List<Point3D>();
            verts.ForEach(p => { upperVerts.Add(new Point3D(p.X, p.Y, maxBolusHeight + OffsetTop)); });

            builder.AddPolygon(verts); //these aren't robust. some triangles are intersecting the boundry
            builder.AddPolygon(upperVerts);

            //sides of the contour
            int count = verts.Count;
            for (int i = 0; i < verts.Count - 1; i++) {
                var p1 = verts[i];
                var p2 = verts[i + 1];
                var p3 = upperVerts[i];
                var p4 = upperVerts[i + 1];

                builder.AddQuad(p4, p2, p1, p3);
            }

            return builder.ToMesh();
        }

        private MeshGeometry3D CalculateContour(DMesh3 mesh) {
            var zHeight = (float)(_bolus.Geometry.Bounds.SizeZ + _bolus.Geometry.Bounds.Z);
            var contour = GetContour(mesh);
            if (contour.Count % 2 != 0) contour.Add(contour[0]);

            var bottomVector = new Vector3D(0, 0, contour[0].z - OffsetBottom);
            var topVector = new Vector3D(0, 0, zHeight + OffsetTop);

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

                var p0 = new Point3D(v0.X, v0.Y, bottomVector.Z);
                var p1 = new Point3D(v1.X, v1.Y, bottomVector.Z);
                var p2 = new Point3D(v2.X, v2.Y, bottomVector.Z);

                builder.AddTriangle(p0, p1, p2);

                p0 = new Point3D(v0.X, v0.Y, topVector.Z);
                p1 = new Point3D(v1.X, v1.Y, topVector.Z);
                p2 = new Point3D(v2.X, v2.Y, topVector.Z);
                builder.AddTriangle(p2, p1, p0);
            }

            //sides of the contour
            int count = verts.Count;
            var indicesCount = builder.Positions.Count();
            for (int i = 0; i < count - 1; i++) {
                var indices = new List<int> {
                    count + i + 1, 
                    i + 1, 
                    i,
                    count
                };

                builder.AddQuad(indices);
            }

            /*
            var points = new List<System.Windows.Point>();
            contour.ForEach(v => points.Add(new System.Windows.Point(v.x, v.y)));

            builder.AddExtrudedSegments(
                points,
                new Vector3D(-1, 0, 0),
                bottomVector.ToPoint3D(),
                topVector.ToPoint3D()

            );*/



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

            //hit tests
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

            var startPoint = new Vector2i(minX, minY);
            var currentPoint = startPoint;
            var lastPoint = new Vector2i(minX, minY - 1);
            var nextPoint = startPoint;
            var contour = new List<Vector3d>();
            var bottom_z = mesh.CachedBounds.Min.z - OffsetBottom + 4.0f;

            while (true) {
                currentPoint = nextPoint;

                //create a vector for the current point and store it
                var vector = new Vector3d(
                    (currentPoint.x + 0.5f) * resolution + min.x, 
                    (currentPoint.y + 0.5) * resolution + min.y, 
                    bottom_z);
                contour.Add(vector);

                //look for the next point to use
                nextPoint = GetNextPoint(map, currentPoint, lastPoint);
                lastPoint = currentPoint;

                //test to see if we should exit the loop
                if (nextPoint == startPoint) {
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
    }
}
