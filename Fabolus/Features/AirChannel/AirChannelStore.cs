using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Bolus;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    #region Messages
    public sealed record AddAirChannelMessage(Point3D point);
    public sealed record SetAirChannelDiameterMessage(double diameter);
    public sealed record ClearAirChannelsMessage();
    public sealed record AirChannelsUpdatedMessage(List<AirChannelModel> channels);
    public sealed record AirChannelSettingsUpdatedMessage(double diameter, double height);

    //requests
    public class AirChannelsRequestMessage : RequestMessage<List<AirChannelModel>> { }
    public class AirChannelDiameterRequestMessage : RequestMessage<double> { }
    public class AirChannelHeightRequestMessage : RequestMessage<double> { }

    #endregion

    public class AirChannelStore : RequestMessage<List<AirChannelModel>> {
        private List<AirChannelModel> _channels;

        private double _channelDiameter = 5.0f;
        private double _zHeightOffset = 10.0f;
        private double _maxZHeight {
            get {
                if (_bolus == null || _bolus.Geometry == null) return 0.0f;
                else return _bolus.Geometry.Bounds.Z + _bolus.Geometry.Bounds.SizeZ + _zHeightOffset;
            }
        }

        private BolusModel _bolus;

        public AirChannelStore() {
            _channels = new List<AirChannelModel>();

            //registering messages
            WeakReferenceMessenger.Default.Register<AddAirChannelMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<SetAirChannelDiameterMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearAirChannelsMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m) => { Update(m.bolus); });

            //request messages
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelsRequestMessage>(this, (r, m) => { m.Reply(r._channels); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelDiameterRequestMessage>(this, (r, m) => { m.Reply(r._channelDiameter); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelHeightRequestMessage>(this, (r, m) => { m.Reply(r._maxZHeight); });
        }

        private void SendChannelsUpdate() => WeakReferenceMessenger.Default.Send(new AirChannelsUpdatedMessage(_channels));
        private void SendSettingsUpdate() => WeakReferenceMessenger.Default.Send(new AirChannelSettingsUpdatedMessage(_channelDiameter, _maxZHeight));
        private void Update(BolusModel bolus) {
            _channels.Clear(); //changing the bolus means airholes no longer valid
            if (bolus == null || bolus.Geometry is null) return;
            _bolus = bolus;

            SendChannelsUpdate();
            SendSettingsUpdate();
        }

        #region Receiving
        private void Receive(AddAirChannelMessage message) {
            var point = message.point;
            _channels.Add(new AirChannelModel(point, _channelDiameter, _maxZHeight - point.Z));

            SendChannelsUpdate();
        }

        private void Receive(SetAirChannelDiameterMessage message) {
            _channelDiameter = message.diameter;

            SendSettingsUpdate();
        }

        private void Receive(ClearAirChannelsMessage message) {
            _channels.Clear();

            SendChannelsUpdate();
        }
        #endregion
    }
}
