using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Bolus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    #region Messages
    public sealed record AddAirChannelMessage(Point3D point);
    public sealed record SetAirChannelDiameterMessage(double diameter);
    public sealed record ClearAirChannelsMessage();
    public sealed record RequestAirChannelsMessage();
    public sealed record AirChannelsUpdatedMessage(List<AirChannelModel> channels, double diameter, double height);

    //requests
    public class AirChannelsRequestMessage : RequestMessage<List<AirChannelModel>> { }
    public class AirChannelDiameterRequestMessage : RequestMessage<double> { }

    #endregion

    public class AirChannelStore : RequestMessage<List<AirChannelModel>> {
        private List<AirChannelModel> _channels;

        private double _channelDiameter = 5.0f;
        private double _zHeightOffset = 5.0f;
        private double _maxZHeight => _bolus.Geometry.Bounds.Z + _bolus.Geometry.Bounds.SizeZ + _zHeightOffset;

        private BolusModel _bolus;

        public AirChannelStore() {
            _channels = new List<AirChannelModel>();

            //registering messages
            WeakReferenceMessenger.Default.Register<AddAirChannelMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<SetAirChannelDiameterMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearAirChannelsMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m) => { Receive(m); });

            //request messages
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelsRequestMessage>(this, (r, m) => { m.Reply(r._channels); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelDiameterRequestMessage>(this, (r, m) => { m.Reply(r._channelDiameter); });

            //requesting info
            _bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
        }

        private void SendUpdate() {
            WeakReferenceMessenger.Default.Send(new AirChannelsUpdatedMessage(_channels, _channelDiameter, _maxZHeight));
        }

        #region Receiving
        private void Receive(AddAirChannelMessage message) {
            var point = message.point;
            _channels.Add(new AirChannelModel(point, _channelDiameter, _maxZHeight));

            SendUpdate();
        }

        private void Receive(SetAirChannelDiameterMessage message) {
            _channelDiameter = message.diameter;

            SendUpdate();
        }

        private void Receive(ClearAirChannelsMessage message) {
            _channels.Clear();

            SendUpdate();
        }

        private void Receive(BolusUpdatedMessage message) {
            _bolus = message.bolus;
            if (_bolus.Geometry is null) return;

            _channels.Clear(); //changing the bolus means airholes no longer valid

            SendUpdate();
        }
        #endregion
    }
}
