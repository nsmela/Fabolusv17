using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel.Channels
{
    public partial class AngledChannelViewModel : ChannelControlViewModel {
        [ObservableProperty] private string _channelName;
        [ObservableProperty] private float _channelDepth, _channelDiameter, _coneLength, _coneDiameter;

        partial void OnChannelDepthChanged(float value) => SendChannelUpdate();
        partial void OnChannelDiameterChanged(float value) => SendChannelUpdate();
        partial void OnConeLengthChanged(float value) => SendChannelUpdate();
        partial void OnConeDiameterChanged(float value) => SendChannelUpdate();

        private AngledChannel _channel;

        public override void Initialize() {
            //Messages
            _channel = WeakReferenceMessenger.Default.Send<AirChannelAngledRequestMessage>();

            //fill out
            ChannelName = _channel.Name;
            ChannelDepth = _channel.ChannelDepth;
            ChannelDiameter = _channel.ChannelDiameter;
            ConeLength = _channel.ConeLength;
            ConeDiameter = _channel.ConeDiameter;

            _isFrozen = false;
        }

        private void SendChannelUpdate() {
            if (_isFrozen) return;

            _isFrozen = true;
            _channel.ChannelDepth = ChannelDepth;
            _channel.ChannelDiameter = ChannelDiameter;
            _channel.ConeLength = ConeLength;
            _channel.ConeDiameter = ConeDiameter;
            WeakReferenceMessenger.Default.Send(new SetChannelMessage(_channel));

            _isFrozen = false;
        }
    }
}
