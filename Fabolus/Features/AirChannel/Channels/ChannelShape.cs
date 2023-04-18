using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public abstract class ChannelShape {
        protected static int SEGMENTS = 32;
        public virtual Type ChannelType => typeof(ChannelBase);
        public virtual MeshGeometry3D Geometry { get; protected set; }
        public virtual DMesh3 Mesh { get; protected set; }
        public abstract DMesh3 OffsetMesh(float offset, float height);
        public abstract ChannelBase GetChannelSettings();
        public abstract void UpdateSettings(ChannelBase channel);
    }
}
