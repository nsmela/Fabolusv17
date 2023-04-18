using Fabolus.Features.AirChannel.Channels;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    public class AirChannelModel {
        public ChannelShape Shape { get; private set; }
        public int? Id { get; set; }

        #region Meshes
        public MeshGeometry3D Geometry => Shape.Geometry;
        public g3.DMesh3 Mesh => Shape.Mesh;

        #endregion

        #region Public Methods
        public AirChannelModel(ChannelShape shape, int? id) {
            Shape = shape;
            Id = id;
        }
        #endregion
    }

}
