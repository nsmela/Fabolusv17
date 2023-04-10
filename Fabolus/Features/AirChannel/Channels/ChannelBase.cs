using Fabolus.Features.AirChannel.Controls;
using Fabolus.Features.AirChannel.MouseTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel.Channels {
    public abstract class ChannelBase {
        public virtual string Name => "air channel base";
        
        //view model and view
        public virtual ChannelControlViewModel ViewModel { get; protected set; }

        //mouse tools
        public virtual Type MouseToolType { get; protected set; }

    }
}
