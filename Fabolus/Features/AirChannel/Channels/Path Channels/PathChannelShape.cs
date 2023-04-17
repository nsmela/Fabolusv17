using Fabolus.Features.Bolus;
using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public class PathChannelShape : ChannelShape {
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
            _upperRadius = upperDiameter / 2;
            _upperHeight = upperHeight;

            Geometry = GenerateGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            if (_path == null || _path.Count < 2) return new MeshGeometry3D();

            var mesh = new MeshBuilder();

            var radius = _radius + offset;
            var upperRadius = _upperRadius + offset;
            Point3D topPoint, bottomPoint;

            for(int i = 0; i < _path.Count; i++) {
                var path = _path[i];

                var depthVector = new Vector3D(0, 0, _depth);
                var upperVector = new Vector3D(0, 0, _upperHeight + upperRadius);
                var topVector = new Vector3D(0, 0, _height - heightOffset);

                AddChannel(ref mesh, path + depthVector, path + upperVector);
                AddChannel(ref mesh, path + upperVector, path + topVector);
                //if i+1  < path.count, add box to next path[i]
                //also add the upper hieght boxes
            }

            return mesh.ToMesh();
        }

        private void AddChannel(ref MeshBuilder mesh, Point3D bottomAnchor, Point3D topAnchor, float offset = 0, float offsetHeight = 0) {
            var radius = _radius + offset;
            var upperPoint = new Point3D(topAnchor.X, topAnchor.Y, topAnchor.Z - offsetHeight);
            var subdivisions = 16;

            mesh.AddSphere(bottomAnchor, radius);
            mesh.AddCylinder(bottomAnchor, upperPoint, radius, subdivisions);
        }
    }
}
