using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public partial class PathChannelViewModel : ChannelViewModelBase {
        [ObservableProperty] private string _channelName;
        [ObservableProperty] private float _channelDepth, _channelDiameter, _upperDiameter, _upperHeight;
        [ObservableProperty] private List<Point3D> _pathPoints;

        partial void OnChannelDepthChanged(float value) => SendChannelUpdate();
        partial void OnChannelDiameterChanged(float value) => SendChannelUpdate();
        partial void OnUpperDiameterChanged(float value) => SendChannelUpdate();
        partial void OnUpperHeightChanged(float value) => SendChannelUpdate();

        private PathChannel _channel;

        public override void Initialize() {
            //Messages
            _channel = WeakReferenceMessenger.Default.Send<AirChannelPathRequestMessage>();

            PathPoints = _channel.PathPoints;

            //fill out
            ChannelName = _channel.Name;
            ChannelDepth = _channel.ChannelDepth;
            ChannelDiameter = _channel.ChannelDiameter;
            UpperDiameter = _channel.UpperDiameter;
            UpperHeight = _channel.UpperHeight;


            _isFrozen = false;
        }

        private void SendChannelUpdate() {
            if (_isFrozen) return;

            _isFrozen = true;
            _channel.ChannelDepth = ChannelDepth;
            _channel.ChannelDiameter = ChannelDiameter;
            _channel.UpperDiameter = UpperDiameter;
            _channel.UpperHeight = UpperHeight;
            _channel.PathPoints = PathPoints;

            WeakReferenceMessenger.Default.Send(new SetChannelMessage(_channel));

            _isFrozen = false;
        }
    }
}
