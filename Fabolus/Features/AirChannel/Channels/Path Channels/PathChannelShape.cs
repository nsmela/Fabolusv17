using Fabolus.Features.Bolus;
using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public class PathChannelShape : ChannelShape {
        private const int SUBDIVISIONS = 16; //spheres and cylinder subdivisions
        public override Type ChannelType => typeof(PathChannel);

        private float _depth, _radius, _height, _upperRadius, _upperHeight;

        private List<Point3D> _path { get; set; }


        public PathChannelShape(List<Point3D> pathAnchors, float depth, float diameter, float height, float upperDiameter, float upperHeight) {
            _path = pathAnchors;
            UpdateSettings(depth, diameter, height, upperDiameter, upperHeight);
        }


        public override ChannelBase GetChannelSettings() => 
            new PathChannel { ChannelDepth = _depth, ChannelDiameter = _radius* 2, UpperDiameter=_upperRadius * 2, UpperHeight = _upperHeight};

        public override void UpdateSettings(ChannelBase channel) {
            if (channel.GetType() != ChannelType) return;

            var pathChannel = channel as PathChannel;
            if (pathChannel == null) return;

            UpdateSettings(pathChannel.ChannelDepth, pathChannel.ChannelDiameter, pathChannel.Height, pathChannel.UpperDiameter, pathChannel.UpperHeight);
        }
        public override DMesh3 OffsetMesh(float offset, float height) => BolusUtility.MeshGeometryToDMesh(GenerateGeometry(offset, height));

        private void UpdateSettings(float depth, float diameter, float height, float upperDiameter, float upperHeight) {
            _depth = depth;
            _radius = diameter / 2;
            _height = height;
            _upperRadius = MathF.Max( upperDiameter / 2, _radius);
            _upperHeight = upperHeight;

            Geometry = GenerateGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            if (_path == null || _path.Count < 2) return new MeshGeometry3D();

            var mesh = new MeshBuilder(true, false);

            var radius = _radius + offset;
            var upperRadius = _upperRadius + offset;

            AddRoundedBox(ref mesh, _path[0], _path[1], radius, _upperHeight);
            return mesh.ToMesh();

            for (int i = 0; i < _path.Count; i++) {
                var path = _path[i];

                var depthVector = new Vector3D(0, 0, _depth);
                var upperVector = new Vector3D(0, 0, _upperHeight + upperRadius);
                var topVector = new Vector3D(0, 0, _height - heightOffset);

                //bottom channel
                mesh.AddSphere(path - depthVector, radius);
                mesh.AddCylinder(path - depthVector, path + upperVector, radius * 2, SUBDIVISIONS);

                //top channel
                mesh.AddSphere(path + upperVector, upperRadius);
                mesh.AddCylinder(path + upperVector, new Point3D(path.X, path.Y, topVector.Z), upperRadius, SUBDIVISIONS, true, true);

                //also add the upper height boxes
                if (i + 1 >= _path.Count) continue;

                var nextPath = _path[i + 1];

                //bottom box w/ round bottom
                mesh.AddCylinder(path - depthVector, nextPath - depthVector, radius * 2, SUBDIVISIONS);
                AddExtendedChannel(ref mesh, path - depthVector, nextPath - depthVector, radius, (float)(upperVector + depthVector).Z); //height + depth equals total length needed

                //top box w/ round bottom
                mesh.AddCylinder(path + upperVector, nextPath + upperVector, upperRadius * 2, SUBDIVISIONS);
                AddExtendedChannel(ref mesh, path + upperVector, nextPath + upperVector, upperRadius, (float)topVector.Z, false);
            }

            return mesh.ToMesh();
        }



        private void AddChannel(ref MeshBuilder mesh, Point3D bottomAnchor, Point3D topAnchor, float radius) {
            mesh.AddSphere(bottomAnchor, radius);
            mesh.AddCylinder(bottomAnchor, topAnchor, radius * 2, SUBDIVISIONS);
        }

        private void AddExtendedChannel(ref MeshBuilder mesh, Point3D origin, Point3D end, float radius, float height, bool evenHeightLengths = true) {
            float vertLength = 0.0f;

            var indices = new int[]{
                0, 1, 2, 3, //f1
                4, 5, 6, 7, //f2
                7, 6, 1, 0, //f3
                5, 4, 3, 2, //f4
                2, 1, 6, 5, //f5
                0, 3, 4, 7  //f6
            };

            var positions = new List<Point3D>();


            //points for the starting face
            vertLength = evenHeightLengths ? height : (float)(height - origin.Z);
            var f1 = FacePoints(origin, end, radius, vertLength);
            mesh.AddPolygon(f1);

            f1.ForEach(p => positions.Add(p));

            //opposite face
            vertLength = evenHeightLengths ? height : (float)(height - end.Z);
            var f2 = FacePoints(end, origin, radius, vertLength);
            mesh.AddPolygon(f2);

            f2.ForEach(p => positions.Add(p));

            //right face
            var f3 = new List<Point3D> {
                positions[indices[8]], positions[indices[9]], positions[indices[10]], positions[indices[11]]
            };
            mesh.AddPolygon(f3);

            //left face
            var f4 = new List<Point3D> {
                positions[indices[12]], positions[indices[13]], positions[indices[14]], positions[indices[15]]
            };
            mesh.AddPolygon(f4);

            //top face
            var f5 = new List<Point3D> {
                positions[indices[16]], positions[indices[17]], positions[indices[18]], positions[indices[19]]
            };
            mesh.AddPolygon(f5);

            //bottom face
            var f6 = new List<Point3D> {
                positions[indices[20]], positions[indices[21]], positions[indices[22]], positions[indices[23]]
            };
            mesh.AddPolygon(f6);

        }

        //methods to build extended channel
        private List<Point3D> FacePoints(Point3D origin, Point3D end, float radius, float verticalLength) {
            var length = radius;
            var verticalVector = new Vector3D(0, 0, verticalLength);
            //setting up the direction vector to calculate the points
            var v = (end - origin);
            v.Z = 0;
            v.Normalize();
            v = Vector3D.CrossProduct(v, new Vector3D(0, 0, 1));

            //points base on origin transformed by perpendicular vectors
            //broken out like this so they can rely on each other for calculations
            var p0 = new Point3D(origin.X, origin.Y, origin.Z); //bottom centre
            var p1 = p0 + (v * length); //bottom left
            var p4 = p0 - (v * length); //bottom right
            var p2 = p1 + verticalVector;//top left
            var p3 = p4 + verticalVector;//top right

            return new List<Point3D> { p1, p2, p3, p4 };

        }

        private void AddRoundedBox(ref MeshBuilder mesh, Point3D origin, Point3D end, float radius, float height) {
            var direction = end - origin;
            var distance = direction.Length;
            direction.Z = 0;
            direction.Normalize();
            var horizontalOffset = Vector3D.CrossProduct(direction, new Vector3D(0, 0, 1));
            var verticalOffset = new Vector3D(0, 0, height);

            var p1 = origin + (horizontalOffset * radius); //bottom left
            var p4 = origin - (horizontalOffset * radius); //bottom right
            var p2 = p1 + verticalOffset;//top left
            var p3 = p4 + verticalOffset;//top right

            var p5 = p1 + direction * distance;
            var p6 = p2 + direction * distance;
            var p7 = p3 + direction * distance;
            var p8 = p4 + direction * distance;


            var arcPoints = HalfArcPoints(p1, p4, -direction, radius);
            var upperArcPoints = new List<Point3D>();
            arcPoints.ForEach(p => upperArcPoints.Add(p + verticalOffset));

            for(int i = 0; i < arcPoints.Count - 1; i++) {
                var a1 = arcPoints[i];
                var a2 = upperArcPoints[i];
                var a3 = upperArcPoints[i+1];
                var a4 = arcPoints[i+1];
                mesh.AddQuad(a1, a2, a3, a4);
                mesh.AddSphere(a1, 0.1f);
                mesh.AddSphere(a1, 0.15f);
            }

            //mesh.AddQuad(p1, p2, p3, p4); //front face
            mesh.AddQuad(p5, p6, p2, p1); //left side
            //mesh.AddQuad(p8, p7, p6, p5); //back face
            mesh.AddQuad(p7, p8, p4, p3); //right side
 
        }

        //Generated by ChatGPT
        private List<Point3D> HalfArcPoints(Point3D start, Point3D end, Vector3D direction, float radius) {

            Vector3D axis = end - start;
            double length = axis.Length;
            axis.Normalize();

            Vector3D normal = new Vector3D(0, 0, 1);
            Vector3D tangent = Vector3D.CrossProduct(direction, normal);
            tangent.Normalize();

            Point3D centre = start + axis * (length / 2);
            Vector3D startVector = start - centre;
            Vector3D endVector = end - centre;

            double startAngle = Math.Atan2(startVector.Y, startVector.X);
            double endAngle = Math.Atan2(endVector.Y, endVector.X);

            if (startAngle < 0) {
                startAngle += 2 * Math.PI;
            }

            if (endAngle < 0) {
                endAngle += 2 * Math.PI;
            }

            if (endAngle < startAngle) {
                endAngle += 2 * Math.PI;
            }

            double deltaAngle = endAngle - startAngle;

            var points = new List<Point3D>();
            for (int i = 0; i <= SEGMENTS; i++) {
                double angle = startAngle + deltaAngle * ((double)i / (double)SEGMENTS);

                Point3D point = centre + radius * (Math.Cos(angle) * tangent + Math.Sin(angle) * normal);
                points.Add(point);
            }

            return points;
        }

        private List<Point3D> ArcPoints(Point3D start, Point3D end, Vector3D axis, Vector3D direction, float radius) {

        }
    }
}
