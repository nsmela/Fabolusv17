using g3;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    public class AirChannelModel {
        public AirChannelShape Shape { get; private set; }


        #region Meshes
        public MeshGeometry3D Geometry => Shape.Geometry;
        public DMesh3 Mesh => Shape.Mesh;

        #endregion

        #region Public Methods
        public AirChannelModel(AirChannelShape shape) {
            Shape = shape;

        }
        #endregion
    }

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
            _anchor = anchor + direction * -2.0f; //moving the anchor deep 2.0 mm deep within the model
            _direction = direction;
            _diameter= diameter;
            _height= height;

            SetGeometry();
            SetMesh();
        }

        private void SetGeometry() {
            float coneLength = 10.0f;

            var mesh = new MeshBuilder();
            mesh.AddSphere(_anchor, _radius  / 2);
            mesh.AddCone(
                _anchor, //cone tip position
                _direction, //cone direction
                _radius /2, //cone base radius
                _radius, //cone top radius
                coneLength, //cone length
                false, //base cap
                false, //top cap
                16 //divisions/resolution
                );

            var point = _anchor + _direction * coneLength; //used for anchor for next mesh addition
            mesh.AddSphere(point, _radius);
            mesh.AddCylinder(
                point,
                new Point3D(point.X, point.Y, _height),
                _radius); 
            Geometry = mesh.ToMesh();
        }

        private void SetMesh() {
            
        }
    }
}
