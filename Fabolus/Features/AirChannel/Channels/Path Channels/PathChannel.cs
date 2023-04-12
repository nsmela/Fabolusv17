using Fabolus.Features.AirChannel.MouseTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public class PathChannel : ChannelBase {
        public override string Name => "path air channel";

        public float ChannelDepth, ChannelDiameter, UpperDiameter, UpperHeight;
        public List<Point3D> PathPoints;

        public Type ChannelType => typeof(PathChannel);
        public PathChannel() {
            ViewModel = new PathChannelViewModel();
            MouseToolType = typeof(PathAirChannelMouseTool);

            //values
            PathPoints = new();
            ChannelDepth = 0.25f;
            ChannelDiameter = 5.0f;
            UpperDiameter = ChannelDiameter + 3.0f;
            UpperHeight = 5.0f;
        }
    }
}
