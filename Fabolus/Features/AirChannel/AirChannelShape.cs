using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {

    public abstract class AirChannelShape {
        public virtual MeshGeometry3D Geometry { get; protected set; }
        public virtual DMesh3 Mesh { get; protected set; }
    }

    public class AirChannelStraight : AirChannelShape {
        private double _diameter;
        private double _radius => _diameter / 2;
        private double _height;
        private double _length => _height - _topAnchor.Z;
        private Point3D _anchor { get; set; }
        private Point3D _topAnchor => new Point3D(_anchor.X, _anchor.Y, _height);
        private Vector3d _dAnchor => new Vector3d(_anchor.X, _anchor.Y, _anchor.Z);
        private Vector3d _dTop => new Vector3d(_topAnchor.X, _topAnchor.Y, _topAnchor.Z);

        public AirChannelStraight(Point3D anchor, double diameter, double height) {
            _anchor = new Point3D(anchor.X, anchor.Y, anchor.Z - 1);
            _diameter = diameter;
            _height = height;

            SetGeometry();
            SetMesh();
        }

        private void SetGeometry() {
            var mesh = new MeshBuilder();
            mesh.AddSphere(_anchor, _radius);
            mesh.AddCylinder(
                _anchor,
                _topAnchor,
                _radius);
            Geometry = mesh.ToMesh();
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
    }

    public class AirChannelAngled : AirChannelShape {
        private double _diameter;
        private double _radius => _diameter / 2;
        private double _height;
        private double _length => _height - _topAnchor.Z;
        private Point3D _anchor { get; set; }
        private Vector3D _direction { get; set; }
        private Point3D _topAnchor => new Point3D(_anchor.X, _anchor.Y, _height);
        private Vector3d _dAnchor => new Vector3d(_anchor.X, _anchor.Y, _anchor.Z);
        private Vector3d _dTop => new Vector3d(_topAnchor.X, _topAnchor.Y, _topAnchor.Z);

        public AirChannelAngled(Point3D anchor, Vector3D direction, double diameter, double height) {
            _anchor = anchor + direction * -0.5f; //moving the anchor deep 2.0 mm deep within the model
            _direction = direction;
            _diameter = diameter;
            _height = height;

            SetGeometry();
            SetMesh();
        }

        private void SetGeometry() {
            float coneLength = 10.0f;

            var mesh = new MeshBuilder();
            //mesh.AddSphere(_anchor, _radius  / 2);
            mesh.AddCone(
                _anchor, //cone tip position
                _direction, //cone direction
                _radius, //cone base radius
                _radius + 1.0f, //cone top radius
                coneLength, //cone length
                true, //base cap
                false, //top cap
                16 //divisions/resolution
                );

            var point = _anchor + _direction * coneLength; //used for anchor for next mesh addition
            mesh.AddSphere(point, _radius + 1.0f);
            mesh.AddCylinder(
                point,
                new Point3D(point.X, point.Y, _height),
                _radius + 1.0f);
            Geometry = mesh.ToMesh();
        }

        private void SetMesh() {

        }
    }

    public class AirChannelPath : AirChannelShape {
        private double _diameter, _height;
        private double _radius => _diameter / 2;
        private List<Point3D> _path { get; set; }

        public AirChannelPath(List<Point3D> path, double diameter, double height) {
            _path = path;
            _diameter = diameter;
            _height = height;

            SetGeometry();
            //SetMesh(); //TODO
        }

        private void SetGeometry() {
            if (_path == null) return;
            if(_path.Count == 0 ) return;

            var mesh = new MeshBuilder();

            mesh.AddSphere(_path[0], _radius);
            AddCylinder(_path[0], ref mesh);

            Point3D origin, end;
            for (int i = 1; i < _path.Count; i++) {
                origin = _path[i - 1];
                end = _path[i];

                mesh.AddSphere(end, _radius);
                AddCylinder(end, ref mesh);
                AddChannel(origin, end, ref mesh);
            }

           

            Geometry = mesh.ToMesh();
        }
        
        private void AddSphere(int pointIndex, ref MeshBuilder mesh) => mesh.AddSphere(Path(pointIndex), _radius);
        private void AddCylinder(Point3D point, ref MeshBuilder mesh) => mesh.AddCylinder(point, new Point3D(point.X, point.Y, _height), _radius, 16, true, true);
        private void AddChannel(Point3D origin, Point3D end, ref MeshBuilder mesh) {
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
            var f1 = FacePoints(origin, direction);
            mesh.AddPolygon(f1);

            f1.ForEach(p => positions.Add(p));

            //opposite face
            var f2 = FacePoints(end, origin);
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
            mesh.AddCylinder(o, e, _radius, 16, true, true);
            return;

            
        }

        private Point3D Path(int pointIndex) => new Point3D(_path[pointIndex].X, _path[pointIndex].Y, _path[pointIndex].Z - 2.5f);
        private Point3D TopPoint(Point3D point) => new Point3D(point.X, point.Y, _height);
        private Vector3D Direction(Point3D start, Point3D end) {
            var result = end - start;
            result.Normalize();
            return result;
        }

        private List<Point3D> FacePoints(Point3D origin, Point3D end) {
            var direction = Direction(origin, end);
            return FacePoints(origin, direction);
        }

        private List<Point3D> FacePoints(Point3D origin, Vector3D direction) {
            double length = _radius;
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
            p2.Z = _height; //top left
            var p3 = p4;
            p3.Z= _height;//top right

            return new List<Point3D> { p1, p2, p3, p4 };
        }
        
    }

}
