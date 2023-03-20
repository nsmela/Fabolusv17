using g3;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    public class AirChannelModel {
        public Point3D Point { get; set; }
        public double Diameter { get; set; }
        public double Length { get; set; }
        public double Radius => Diameter / 2;
        public Point3D Anchor => new Point3D(Point.X, Point.Y, Point.Z - 1);
        public Point3D Top => new Point3D(Point.X, Point.Y, Point.Z + Length);
        public Vector3d DAnchor => new Vector3d(Anchor.X, Anchor.Y, Anchor.Z);
        public Vector3d DTop => new Vector3d(Top.X, Top.Y, Top.Z);

        #region Meshes
        public MeshGeometry3D Geometry {
            get {
                MeshBuilder mesh = new MeshBuilder(true);
                mesh.AddSphere(Anchor, Radius);
                mesh.AddCylinder(Anchor, Top, Radius, 32, false, true);
                return mesh.ToMesh();
            }
        }

        public DMesh3 Mesh {
            get {
                MeshEditor mesh = new MeshEditor(new DMesh3());

                Sphere3Generator_NormalizedCube generateSphere = new Sphere3Generator_NormalizedCube {
                    Radius = this.Radius,
                    EdgeVertices = 5
                };
                DMesh3 sphereMesh = generateSphere.Generate().MakeDMesh();
                MeshTransforms.Translate(sphereMesh, DAnchor);
                mesh.AppendMesh(sphereMesh);

                CappedCylinderGenerator generateCyclinder = new CappedCylinderGenerator {
                    BaseRadius = (float)Radius,
                    TopRadius = (float)Radius,
                    Height = (float)Length,
                    Slices = 64
                };
                DMesh3 tubeMesh = generateCyclinder.Generate().MakeDMesh();
                Quaterniond rotate = new Quaterniond(Vector3d.AxisX, 90.0f);
                MeshTransforms.Rotate(tubeMesh, Vector3d.Zero, rotate);
                MeshTransforms.Translate(tubeMesh, DAnchor);
                mesh.AppendMesh(tubeMesh);

                return mesh.Mesh;
            }
        }
        #endregion

        #region Public Methods
        public AirChannelModel(Point3D point, double diameter, double length) {
            Point = point;
            Diameter = diameter;
            Length = length;
        }

        public MeshGeometry3D OffsetGeometry(double offset) {
            MeshBuilder mesh = new MeshBuilder(true);
            mesh.AddSphere(Anchor, Radius);
            mesh.AddCylinder(Anchor, Top, Radius + offset, 32, false, true);
            return mesh.ToMesh();
        }
        #endregion
    }

    public abstract class AirChannelShape {
         public virtual MeshGeometry3D Geometry { get; protected set; }
        public virtual DMesh3 Mesh { get; protected set; }
        public abstract void Build();
    }

    public class AirChannelStraight : AirChannelShape {
        private double _diameter;
        private double _radius => _diameter / 2;
        private double _height;
        private double _length => _height - _topAnchor.Z;
        private Point3D _anchor { get; set; }
        private Point3D _topAnchor => new Point3D(_anchor.X, _anchor.Y, _height);
        public Vector3d _dAnchor => new Vector3d(_anchor.X, _anchor.Y, _anchor.Z);
        public Vector3d _dTop => new Vector3d(_topAnchor.X, _topAnchor.Y, _topAnchor.Z);

        public AirChannelStraight(Point3D anchor, double height, double diameter = 2.0f) {
            _anchor = new Point3D(anchor.X, anchor.Y, anchor.Z - 1);
            _diameter = diameter;
            _height = height;
            Build();
        }

        public override void Build() {
            SetGeometry();
        }

        private void SetGeometry() {
            var mesh = new MeshBuilder();
            mesh.AddSphere(_anchor, _radius);
            mesh.AddCylinder(
                _anchor,
                _topAnchor,
                _diameter);
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
}
