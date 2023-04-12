using Fabolus.Features.AirChannel.Channels;
using Fabolus.Features.Bolus;
using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {

    public abstract class AirChannelShape {
        public virtual Type ChannelType => typeof(ChannelBase);
        public virtual MeshGeometry3D Geometry { get; protected set; }
        public virtual DMesh3 Mesh { get; protected set; }
        public abstract DMesh3 MeshOffset(float offset, float height);
        public abstract ChannelBase GetChannelSettings();
        public abstract void UpdateSettings(ChannelBase channel);

        //mesh methods
        protected DMesh3 Sphere(double radius, int edgeVerts, Vector3d anchor) {
            Sphere3Generator_NormalizedCube generateSphere = new Sphere3Generator_NormalizedCube {
                Radius = radius,
                EdgeVertices = edgeVerts
            };
            DMesh3 sphereMesh = generateSphere.Generate().MakeDMesh();
            MeshTransforms.Translate(sphereMesh, anchor);
            return sphereMesh;
        }

        protected DMesh3 Cylinder(double radius, double height, int slices, Vector3d anchor) {
            var generateCyclinder = new CappedCylinderGenerator {
                BaseRadius = (float)radius,
                TopRadius = (float)radius,
                Height = (float)height,
                Slices = slices
            };
            DMesh3 tubeMesh = generateCyclinder.Generate().MakeDMesh();
            Quaterniond rotate = new Quaterniond(Vector3d.AxisX, 90.0f);
            MeshTransforms.Rotate(tubeMesh, Vector3d.Zero, rotate);
            MeshTransforms.Translate(tubeMesh, anchor);
            return tubeMesh;
        }

        protected DMesh3 Cone(double bottomRadius, double topRadius, double height, int slices, Vector3d angle, Vector3d anchor) {
            var generateCyclinder = new CappedCylinderGenerator {
                BaseRadius = (float)bottomRadius,
                TopRadius = (float)topRadius,
                Height = (float)height,
                Slices = slices
            };
            DMesh3 tubeMesh = generateCyclinder.Generate().MakeDMesh();
            Quaterniond rotate = new Quaterniond(Vector3d.Zero, angle);
            MeshTransforms.Rotate(tubeMesh, Vector3d.Zero, rotate);
            MeshTransforms.Translate(tubeMesh, anchor);
            return tubeMesh;
        }
    }

    public class AirChannelStraight : AirChannelShape {
        public override Type ChannelType => typeof(VerticalChannel);
        public override ChannelBase GetChannelSettings() => new VerticalChannel { ChannelDepth = (float)_depth, ChannelDiameter = (float)_diameter };
        public override void UpdateSettings(ChannelBase channel) {
            if (channel.GetType() != ChannelType) return;

            var vertChannel = channel as VerticalChannel;
            _depth = vertChannel.ChannelDepth;
            _diameter = vertChannel.ChannelDiameter;
            //_height = vertChannel.Height;

            BottomAnchor = new Point3D(_anchor.X, _anchor.Y, _anchor.Z - _depth);
            TopAnchor = new Point3D(_anchor.X, _anchor.Y, _height);

            Geometry = SetGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private double  _depth, _diameter;
        private double _radius => _diameter / 2;
        private double _height;
        private double _length => _height - _dAnchor.z;
        private Point3D _anchor { get; set; }
        private Point3D BottomAnchor { get; set; }
        private Point3D TopAnchor { get; set; }
        private Point3D _topAnchor => new Point3D(_anchor.X, _anchor.Y, _height);
        private Vector3d _dAnchor => new Vector3d(_anchor.X, _anchor.Y, _anchor.Z);
        private Vector3d _dTop => new Vector3d(_topAnchor.X, _topAnchor.Y, _topAnchor.Z);

        public AirChannelStraight(Point3D anchor, double depth, double diameter, double height) {
            _anchor = new Point3D(anchor.X, anchor.Y, anchor.Z);
            _depth = depth;
            _diameter = diameter;
            _height = height;

            BottomAnchor = new Point3D(_anchor.X, _anchor.Y, _anchor.Z - _depth);
            TopAnchor = new Point3D(_anchor.X, _anchor.Y, _height);

            Geometry = SetGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D SetGeometry() {
            var mesh = new MeshBuilder();

            mesh.AddSphere(BottomAnchor, _radius);
            mesh.AddCylinder(
                BottomAnchor,
                TopAnchor,
                _radius);
            return mesh.ToMesh();
        }

        private void SetMesh() {
            MeshEditor mesh = new MeshEditor(new DMesh3());

            Sphere3Generator_NormalizedCube generateSphere = new Sphere3Generator_NormalizedCube {
                Radius = _radius,
                EdgeVertices = 5
            };
            DMesh3 sphereMesh = generateSphere.Generate().MakeDMesh();
            MeshTransforms.Translate(sphereMesh, _dAnchor);
            mesh.AppendMesh(sphereMesh);

            CappedCylinderGenerator generateCyclinder = new CappedCylinderGenerator {
                BaseRadius = (float)_radius,
                TopRadius = (float)_radius,
                Height = (float)_length,
                Slices = 32
            };
            DMesh3 tubeMesh = generateCyclinder.Generate().MakeDMesh();
            Quaterniond rotate = new Quaterniond(Vector3d.AxisX, 90.0f);
            MeshTransforms.Rotate(tubeMesh, Vector3d.Zero, rotate);
            MeshTransforms.Translate(tubeMesh, _dAnchor);
            mesh.AppendMesh(tubeMesh);

            Mesh = mesh.Mesh;
        }

        public override DMesh3 MeshOffset(float offset, float height) {
            var mesh = new MeshBuilder();
            var radius = _radius + offset;

            mesh.AddSphere(BottomAnchor, radius);
            mesh.AddCylinder(
                BottomAnchor,
                new Point3D(TopAnchor.X, TopAnchor.Y, height),
                radius);
            return BolusUtility.MeshGeometryToDMesh( mesh.ToMesh());
        }
    }

    public class AirChannelAngled : AirChannelShape {
        public override Type ChannelType => typeof(AngledChannel);
        public override ChannelBase GetChannelSettings() => new AngledChannel { ChannelDepth = (float)_depth, ChannelDiameter = (float)_diameter, ConeDiameter = (float)_coneDiameter, ConeLength = (float)_coneLength };
        public override void UpdateSettings(ChannelBase channel) {
            if (channel.GetType() != ChannelType) return;

            var angleChannel = channel as AngledChannel;
            _depth = angleChannel.ChannelDepth;
            _diameter = angleChannel.ChannelDiameter;
            _coneLength = angleChannel.ConeLength;
            _coneDiameter = angleChannel.ConeDiameter;
            //_height = angleChannel.Height;

            _coneAnchor = _anchor + _direction * -_depth;
            _bottomAnchor = _coneAnchor + _direction * (_coneLength);
            _topAnchor = new Point3D(_bottomAnchor.X, _bottomAnchor.Y, _height);

            Geometry = SetGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private Point3D _coneAnchor, _bottomAnchor, _topAnchor;

        private double _depth, _diameter, _coneLength, _coneDiameter, _height;
        private Point3D _anchor { get; set; }
        private Vector3D _direction { get; set; }

        public AirChannelAngled(Point3D anchor, Vector3D direction, double depth, double diameter, double coneLength, double coneDiameter, double height) {
            _anchor = anchor;
            _depth = depth;
            _direction = direction;
            _diameter = diameter;
            _coneLength = coneLength;
            _coneDiameter = coneDiameter;
            _height = height;

            _coneAnchor = _anchor + _direction * -_depth;
            _bottomAnchor = _coneAnchor + _direction * (_coneLength);
            _topAnchor = new Point3D(_bottomAnchor.X, _bottomAnchor.Y, _height);

            Geometry = SetGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D SetGeometry() {
            var mesh = new MeshBuilder();
            mesh.AddCone(
                _coneAnchor, //cone tip position
                _direction, //cone direction
                _coneDiameter / 2, //cone base radius
                _diameter / 2, //cone top radius
                _coneLength, //cone length
                true, //base cap
                false, //top cap
                16 //divisions/resolution
                );

            mesh.AddSphere(_bottomAnchor, _diameter / 2);
            mesh.AddCylinder(
                _bottomAnchor,
                _topAnchor,
                _diameter / 2);
            return mesh.ToMesh();
        }

        private void SetMesh() {
            BolusUtility.MeshGeometryToDMesh(Geometry); //temp solution
        }

        public override DMesh3 MeshOffset(float offset, float height) {
            var mesh = new MeshBuilder();

            var anchor = _anchor + _direction * -_depth;
            var radius = _diameter / 2 + offset;

            mesh.AddCone(
                anchor, //cone tip position
                _direction, //cone direction
                _coneDiameter / 2 + offset, //cone base radius
                radius, //cone top radius
                _coneLength, //cone length
                true, //base cap
                false, //top cap
                16 //divisions/resolution
                );

            var point = anchor + _direction * (_coneLength); //used for anchor for next mesh addition

            mesh.AddSphere(point, radius);
            mesh.AddCylinder(
                point,
                new Point3D(point.X, point.Y, _height),
                radius);
            return BolusUtility.MeshGeometryToDMesh(mesh.ToMesh());
        }
    }

    public class AirChannelPath : AirChannelShape {
        public override Type ChannelType => typeof(AngledChannel);
        public override ChannelBase GetChannelSettings() => new AngledChannel { ChannelDepth = (float)_depth, ChannelDiameter = (float)_diameter };
        public override void UpdateSettings(ChannelBase channel) {
            throw new NotImplementedException();
        }

        private double _depth, _diameter, _height;
        private double _radius => _diameter / 2;
        private List<Point3D> _path { get; set; }

        public AirChannelPath(List<Point3D> path, double diameter, double height) {
            _path = path;
            _diameter = diameter;
            _height = height;

            Geometry = SetGeometry();
            //GeometryOffset = SetGeometry(3.2f);
            //SetMesh(); //TODO
        }

        private MeshGeometry3D SetGeometry() {
            if (_path == null) return null;
            if(_path.Count == 0 ) return null;

            var mesh = new MeshBuilder();

            mesh.AddSphere(_path[0], _radius);
            AddCylinder(_path[0], _radius, _height,  ref mesh);

            Point3D origin, end;
            for (int i = 1; i < _path.Count; i++) {
                origin = _path[i - 1];
                end = _path[i];

                mesh.AddSphere(end, _radius);
                AddCylinder(end, _radius, _height, ref mesh);
                AddChannel(origin, end, _radius, _height, ref mesh);
            }

            return mesh.ToMesh();
        }

        public override DMesh3 MeshOffset(float offset, float height) {
            if (_path == null) return null;
            if (_path.Count == 0) return null;

            var mesh = new MeshBuilder();
            var radius = _radius + offset;

            mesh.AddSphere(_path[0], radius);
            AddCylinder(_path[0], radius, height, ref mesh);

            Point3D origin, end;
            for (int i = 1; i < _path.Count; i++) {
                origin = _path[i - 1];
                end = _path[i];

                mesh.AddSphere(end, radius);
                AddCylinder(end, radius, height, ref mesh);
                AddChannel(origin, end, radius, height, ref mesh);
            }

            return BolusUtility.MeshGeometryToDMesh( mesh.ToMesh());
        }

        private void AddCylinder(Point3D point, double radius, double height, ref MeshBuilder mesh) => mesh.AddCylinder(point, new Point3D(point.X, point.Y, height), radius, 16, true, true);
        private void AddChannel(Point3D origin, Point3D end, double radius, double height, ref MeshBuilder mesh) {
            var direction = Direction(origin, end);
            
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
            var f1 = FacePoints(origin, direction, radius, height);
            mesh.AddPolygon(f1);

            f1.ForEach(p => positions.Add(p));

            //opposite face
            var f2 = FacePoints(end, origin, radius, height);
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

            var o = new Point3D(origin.X, origin.Y, origin.Z);
            var e = new Point3D(end.X, end.Y, end.Z);
            mesh.AddCylinder(o, e, radius, 16, true, true);
            return;

            
        }

        private Point3D Path(int pointIndex) => new Point3D(_path[pointIndex].X, _path[pointIndex].Y, _path[pointIndex].Z - 2.5f);
        private Point3D TopPoint(Point3D point) => new Point3D(point.X, point.Y, _height);
        private Vector3D Direction(Point3D start, Point3D end) {
            var result = end - start;
            result.Normalize();
            return result;
        }

        private List<Point3D> FacePoints(Point3D origin, Point3D end, double radius, double height) {
            var direction = Direction(origin, end);
            return FacePoints(origin, direction, radius, height);
        }

        private List<Point3D> FacePoints(Point3D origin, Vector3D direction, double radius, double height) {
            double length = radius;
            var v = direction;
            v.Z = 0;
            v.Normalize();
            v = Vector3D.CrossProduct(v, new Vector3D(0, 0, 1));

            //points base on origin transformed by perpendicular vectors
            //broken out like this so they can rely on each other for calculations
            var p0 = new Point3D(origin.X, origin.Y, origin.Z); //bottom centre
            var p1 = p0 + (v * length); //bottom left
            var p4 = p0 - (v * length); //bottom right
            var p2 = p1;
            p2.Z = height; //top left
            var p3 = p4;
            p3.Z= height;//top right

            return new List<Point3D> { p1, p2, p3, p4 };
        }




    }

}
