using Fabolus.Features.AirChannel.MouseTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel.Channels
{
    public class AngledChannel : ChannelBase {
        public override string Name => "angled air channel";

        public float ChannelDepth, ChannelDiameter, ConeLength, ConeDiameter;

        public Type ChannelType => typeof(AngledChannel);
        public AngledChannel() {
            ViewModel = new AngledChannelViewModel();
            MouseToolType = typeof(AngledAirChannelMouseTool);

            //values
            ChannelDepth = 0.25f;
            ChannelDiameter = 5.0f;
            ConeLength = 10.0f;
            ConeDiameter = 4.0f;
        }
    }
}
