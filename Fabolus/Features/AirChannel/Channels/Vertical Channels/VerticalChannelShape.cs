using Fabolus.Features.Bolus;
using Fabolus.Features.Helpers;
using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public class VerticalChannelShape : ChannelShape {
        public override Type ChannelType => typeof(VerticalChannel);

        private float _depth, _radius, _height;
        private Point3D _anchor;

        private Point3D BottomAnchor { get; set; }    
        private Point3D TopAnchor { get; set; }

        public VerticalChannelShape(Point3D anchor, float depth, float diameter, float height) => UpdateSettings(depth, diameter, height, anchor);

        public override ChannelBase GetChannelSettings() => new VerticalChannel { ChannelDepth = _depth, ChannelDiameter = _radius * 2 };
        public override void UpdateSettings(ChannelBase channel) {
            if (channel.GetType() != ChannelType) return;

            var vertChannel = channel as VerticalChannel;
            if (vertChannel == null) return;

            UpdateSettings(vertChannel.ChannelDepth, vertChannel.ChannelDiameter, vertChannel.Height);
        }

        private void UpdateSettings(float depth, float diameter, float height, Point3D? anchor = null) {
            _depth = depth;
            _radius = diameter / 2;
            _height = height;

            if (anchor != null) _anchor = anchor.Value;
            BottomAnchor = new Point3D(_anchor.X, _anchor.Y, _anchor.Z - _depth);
            TopAnchor = new Point3D(_anchor.X, _anchor.Y, _height);

            Mesh = GenerateMesh();
            Geometry = Mesh.ToGeometry();
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            var mesh = new MeshBuilder(true, false);
            var radius = _radius + offset;
            var heightVector = new Vector3D(0, 0, heightOffset);

            mesh.AddSphere(BottomAnchor, radius);
            mesh.AddCylinder(
                BottomAnchor,
                TopAnchor - heightVector, //lower the top of the mesh if offset
                radius);
            return mesh.ToMesh();
        }

        private DMesh3 GenerateMesh(float offset = 0, float heightOffset = 0) {
            var mesh = new MeshEditor(new DMesh3(true, false));
            var radius = _radius + offset;
            var anchor = new Vector3d(BottomAnchor.X, BottomAnchor.Y, BottomAnchor.Z - _depth);


            //create cylinder
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
            MeshTransforms.Translate(cylinder, anchor);
            mesh.AppendMesh(cylinder);
            var result = mesh.Mesh;
            
            result.ReverseOrientation();//comes out with reversed normals
            return mesh.Mesh;
            
        }

        public override DMesh3 OffsetMesh(float offset, float height) => GenerateMesh(offset, height);

    }
}
