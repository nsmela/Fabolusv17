using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel.Controls {
    public abstract class ChannelControlViewModel : ObservableObject {
        protected bool _isFrozen = true; //used to prevent updating loop with channel variables
        public abstract void Initialize();
    }
}
