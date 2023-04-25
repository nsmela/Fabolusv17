using Fabolus.Features.Bolus;
using Fabolus.Features.Helpers;
using g3;
using HelixToolkit.Wpf;
using System;
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

            Geometry = GenerateGeometry();
            Mesh = Geometry.ToDMesh();// BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            var mesh = new MeshBuilder(true, false);

            var radius = _radius + offset;
            var coneRadius = _coneRadius + offset;
            var vertOffset = new Vector3D(0, 0, -heightOffset);
            var coneLength = _coneLength + _depth;

            mesh.AddSphere(ConeAnchor, coneRadius, SEGMENTS);
            mesh.AddCone(ConeAnchor, _direction, coneRadius, radius, coneLength, true, true, SEGMENTS);
            mesh.AddSphere(BottomAnchor, radius, SEGMENTS);
            mesh.AddCylinder(BottomAnchor, TopAnchor + vertOffset, radius, SEGMENTS, true, true);

            return mesh.ToMesh();
        }

        public override DMesh3 OffsetMesh(float offset, float height) => BolusUtility.MeshGeometryToDMesh(GenerateGeometry(offset, height));

    }
}
