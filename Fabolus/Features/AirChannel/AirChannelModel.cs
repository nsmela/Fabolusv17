using g3;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    public class AirChannelModel {
        public AirChannelShape Shape { get; private set; }
        public int? Id { get; set; }

        #region Meshes
        public MeshGeometry3D Geometry => Shape.Geometry;
        public DMesh3 Mesh => Shape.Mesh;

        #endregion

        #region Public Methods
        public AirChannelModel(AirChannelShape shape, int? id) {
            Shape = shape;
            Id = id;
        }
        #endregion
    }

}
