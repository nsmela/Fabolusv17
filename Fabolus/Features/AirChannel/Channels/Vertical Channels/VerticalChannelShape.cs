using Fabolus.Features.Bolus;
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
            
            Geometry = GenerateGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            var mesh = new MeshBuilder();

            var radius = _radius + offset;

            mesh.AddSphere(BottomAnchor, radius);
            mesh.AddCylinder(
                BottomAnchor,
                new Point3D(TopAnchor.X, TopAnchor.Y, TopAnchor.Z - heightOffset), //lower the top of the mesh if offset
                radius);
            return mesh.ToMesh();
        }

        public override DMesh3 OffsetMesh(float offset, float height) => BolusUtility.MeshGeometryToDMesh(GenerateGeometry(offset, height));

    }
}
