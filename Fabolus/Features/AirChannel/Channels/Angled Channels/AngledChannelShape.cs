using Fabolus.Features.Bolus;
using Fabolus.Features.Helpers;
using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels.Angled_Channels {
    public class AngledChannelShape : ChannelShape {
        public override Type ChannelType => typeof(AngledChannel);

        private float _depth, _radius, _height, _coneRadius, _coneLength;
        private Point3D _anchor;
        private Vector3D _direction;

        private Point3D ConeAnchor { get; set; }
        private Point3D BottomAnchor { get; set; }
        private Point3D TopAnchor { get; set; }

        public AngledChannelShape(Point3D anchor, Vector3D direction, float depth, float diameter, float height, float coneDiameter, float coneLength) =>
            UpdateSettings(depth, diameter, height, coneDiameter, coneLength, direction, anchor);

        public override ChannelBase GetChannelSettings() => new AngledChannel {
            ChannelDepth = _depth,
            ChannelDiameter = _radius * 2,
            ConeDiameter = _coneRadius * 2,
            ConeLength = _coneLength
        };

        public override void UpdateSettings(ChannelBase channel) {
            if (channel.GetType() != ChannelType) return;

            var angledChannel = channel as AngledChannel;
            if (angledChannel == null) return;

            UpdateSettings(angledChannel.ChannelDepth, angledChannel.ChannelDiameter, angledChannel.Height, angledChannel.ConeDiameter, angledChannel.ConeLength);
        }

        private void UpdateSettings(float depth, float diameter, float height, float coneDiameter, float coneLength, Vector3D? direction = null, Point3D? anchor = null) {
            _depth = depth;
            _radius = diameter / 2;
            _height = height;
            _coneLength = coneLength;
            _coneRadius = coneDiameter / 2;

            if (direction != null) _direction = direction.Value;
            if (anchor != null) _anchor = anchor.Value;

            ConeAnchor = _anchor + (_direction * -_depth);
            BottomAnchor = ConeAnchor + _direction * (_coneLength + _depth);
            TopAnchor = new Point3D(BottomAnchor.X, BottomAnchor.Y, _height);

            //Geometry = GenerateGeometry();
            //Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
            Mesh = GenerateMesh();
            Geometry = Mesh.ToGeometry();
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            var mesh = new MeshBuilder(true, false);

            var radius = _radius + offset;
            var vertOffset = new Vector3D(0, 0, -heightOffset);

            //create circle at cone opening
            mesh.AddTube(
                new List<Point3D> { ConeAnchor, BottomAnchor, TopAnchor + vertOffset }, 
                null, 
                new[] { (double)_coneRadius * 2, (double)radius * 2, (double)radius * 2 }, 
                SEGMENTS, 
                false, 
                true, 
                true
            );

            return mesh.ToMesh();
        }

        public override DMesh3 OffsetMesh(float offset, float height) => BolusUtility.MeshGeometryToDMesh(GenerateGeometry(offset, height));

        private DMesh3 GenerateMesh(float offset = 0, float heightOffset = 0) {
            var mesh = new MeshEditor(new DMesh3());

            var coneRadius = _coneRadius + offset;
            var radius = _radius + offset;
            var vertOffset = new Vector3D(0, 0, -heightOffset);

            //sphere
            Sphere3Generator_NormalizedCube sphereGenerator = new Sphere3Generator_NormalizedCube {
                Radius = coneRadius,
                EdgeVertices = SEGMENTS
            };
            sphereGenerator.Generate();
            var sphere = new DMesh3(sphereGenerator.MakeDMesh());
            MeshTransforms.Translate(sphere, ConeAnchor.ToVector3d());
            mesh.AppendMesh(sphere);

            //cone
            CappedCylinderGenerator coneGenerator = new CappedCylinderGenerator {
                BaseRadius = coneRadius,
                TopRadius = radius,
                Height = _coneLength,
                Slices = SEGMENTS
            };
            coneGenerator.Generate();

            var direction = _direction.ToVector3d();
            var cone = new DMesh3(coneGenerator.MakeDMesh());
            var vX = Vector3d.AxisX.AngleD(direction);
            var vY = Vector3d.AxisY.AngleD(direction);
            var vZ = Vector3d.AxisZ.AngleD(direction);
            var coneRotationX = new Quaterniond(Vector3d.AxisX, vX);
            var coneRotationY = new Quaterniond(Vector3d.AxisY, vY);
            var coneRotationZ = new Quaterniond(Vector3d.AxisZ, vZ);
            MeshTransforms.Rotate(cone, ConeAnchor.ToVector3d(), coneRotationX);
            MeshTransforms.Rotate(cone, ConeAnchor.ToVector3d(), coneRotationY);
            MeshTransforms.Rotate(cone, ConeAnchor.ToVector3d(), coneRotationZ);
            MeshTransforms.Translate(cone, ConeAnchor.ToVector3d());
            mesh.AppendMesh(cone);

            //sphere
            sphereGenerator = new Sphere3Generator_NormalizedCube {
                Radius = radius,
                EdgeVertices = SEGMENTS
            };
            sphereGenerator.Generate();
            sphere = new DMesh3(sphereGenerator.MakeDMesh());
            MeshTransforms.Translate(sphere, BottomAnchor.ToVector3d());
            mesh.AppendMesh(sphere);

            //cylinder
            var cyl_gen = new CappedCylinderGenerator {
                BaseRadius = radius,
                TopRadius = radius,
                Height = (float)(_height - heightOffset - BottomAnchor.Z - _depth),
                Slices = SEGMENTS
            };

            cyl_gen.Generate();
            DMesh3 cylinder = new DMesh3(cyl_gen.MakeDMesh());
            var rotation = new Quaterniond(Vector3d.AxisX, 90.0f);
            MeshTransforms.Rotate(cylinder, Vector3d.Zero, rotation);
            MeshTransforms.Translate(cylinder, BottomAnchor.ToVector3d());
            mesh.AppendMesh(cylinder);
            var result = mesh.Mesh;

            result.ReverseOrientation();//comes out with reversed normals
            return mesh.Mesh;
        }
    }
}
