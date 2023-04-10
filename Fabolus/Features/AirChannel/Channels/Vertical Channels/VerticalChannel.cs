using Fabolus.Features.AirChannel.MouseTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel.Channels {
    public class VerticalChannel : ChannelBase {
        public override string Name => "vertical air channel";

        public float ChannelDepth, ChannelDiameter;

        public Type ChannelType => typeof(VerticalChannel);
        public VerticalChannel() {
            ViewModel = new VerticalChannelViewModel();
            MouseToolType = typeof(VerticalAirChannelMouseTool);

            //values
            ChannelDepth = 2.0f;
            ChannelDiameter = 5.0f;
        }

    }
}
