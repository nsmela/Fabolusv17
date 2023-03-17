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
}
