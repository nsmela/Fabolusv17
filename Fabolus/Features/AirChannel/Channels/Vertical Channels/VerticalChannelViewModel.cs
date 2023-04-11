using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel.Channels {
    public partial class VerticalChannelViewModel : ChannelControlViewModel {
        [ObservableProperty] private string _channelName;
        [ObservableProperty] private float _channelDepth, _channelDiameter;

        partial void OnChannelDepthChanged(float value) => SendChannelUpdate();
        partial void OnChannelDiameterChanged(float value) => SendChannelUpdate();

        private VerticalChannel _channel;

        public override void Initialize() {
            //Messages
            _channel = WeakReferenceMessenger.Default.Send<AirChannelVerticalRequestMessage>();

            //fill out
            ChannelName = _channel.Name;
            ChannelDepth = _channel.ChannelDepth;
            ChannelDiameter = _channel.ChannelDiameter;

            _isFrozen = false;
        }

        private void SendChannelUpdate() {
            if (_isFrozen) return;

            _isFrozen = true;
            _channel.ChannelDepth = ChannelDepth;
            _channel.ChannelDiameter = ChannelDiameter;
            WeakReferenceMessenger.Default.Send(new SetChannelMessage(_channel));

            _isFrozen = false;
        }
    }
}
