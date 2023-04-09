using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Bolus;
using g3;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    #region Messages
    public sealed record AddAirChannelMessage(Point3D point);
    public sealed record AddAirChannelShapeMessage(AirChannelShape shape);
    public sealed record SetAirChannelDiameterMessage(double diameter);
    public sealed record ClearAirChannelsMessage();
    public sealed record AirChannelsUpdatedMessage(List<AirChannelModel> channels, int? selectedIndex);
    public sealed record AirChannelSettingsUpdatedMessage(double diameter, double height, int? selectedIndex);
    public sealed record AirChannelSetMessage(int? channelIndex);

    //requests
    public class AirChannelsRequestMessage : RequestMessage<List<AirChannelModel>> { }
    public class AirChannelDiameterRequestMessage : RequestMessage<double> { }
    public class AirChannelHeightRequestMessage : RequestMessage<double> { }
    public class AirChannelSelectedRequestMessage : RequestMessage<int> { }
    public class AirChannelMeshRequestMessage : RequestMessage<DMesh3> { }

    #endregion

    public class AirChannelStore : RequestMessage<List<AirChannelModel>> {
        private List<AirChannelModel> _channels;

        private double _channelDiameter = 5.0f;
        private double _zHeightOffset = 20.0f;

        private double _maxZHeight {
            get {
                if (_bolus == null || _bolus.Geometry == null) return 0.0f;
                else return _bolus.Geometry.Bounds.Z + _bolus.Geometry.Bounds.SizeZ + _zHeightOffset;
            }
        }

        private BolusModel _bolus;
        private int _selectedChannel;
        private int? _currentId; //used to identify air channels

        public AirChannelStore() {
            _channels = new List<AirChannelModel>();
            _currentId = null;

            //registering messages
            WeakReferenceMessenger.Default.Register<AddAirChannelMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<AddAirChannelShapeMessage>(this, (r, m) => { AddAirChannel(m.shape); });
            WeakReferenceMessenger.Default.Register<SetAirChannelDiameterMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearAirChannelsMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m) => { Update(m.bolus); });
            WeakReferenceMessenger.Default.Register<AirChannelSetMessage>(this, (r, m) => {Update(m.channelIndex); });

            //request messages
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelsRequestMessage>(this, (r, m) => { m.Reply(r._channels); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelDiameterRequestMessage>(this, (r, m) => { m.Reply(r._channelDiameter); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelHeightRequestMessage>(this, (r, m) => { m.Reply(r._maxZHeight); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelSelectedRequestMessage>(this, (r,m) => { m.Reply(r._selectedChannel); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelMeshRequestMessage>(this, (r, m) => { m.Reply(r.ToMesh()); });
        }

        private void SendChannelsUpdate() => WeakReferenceMessenger.Default.Send(new AirChannelsUpdatedMessage(_channels, _selectedChannel));
        private void SendSettingsUpdate() => WeakReferenceMessenger.Default.Send(new AirChannelSettingsUpdatedMessage(_channelDiameter, _maxZHeight, _selectedChannel));
        private void Update(BolusModel bolus) {
            _channels.Clear(); //changing the bolus means airholes no longer valid
            _currentId = null; //clear id count

            if (bolus == null || bolus.Geometry is null) return;
            _bolus = bolus;

            SendChannelsUpdate();
            SendSettingsUpdate();
        }
        private void Update(int? selectedIndex) {
            if (selectedIndex== null) {
                selectedIndex = -1;
                return;
            }
            if (selectedIndex >= _channels.Count) {
                _selectedChannel = -1;
                return;
            }

            _selectedChannel = (int)selectedIndex;

            SendChannelsUpdate();
        }

        #region Receiving
        private void AddAirChannel(AirChannelShape shape) {
            if (_channels == null) _channels = new List<AirChannelModel>();

            if (_currentId == null) _currentId = 0;
            else _currentId++;

            _channels.Add(new AirChannelModel(shape, _currentId));
            _selectedChannel = _channels.Count - 1;

            SendChannelsUpdate();
        }

        private DMesh3 ToMesh() {
            if (_channels == null || _channels.Count <= 0) return null;

            var airHole = new MeshEditor(new DMesh3());
            foreach (var channel in _channels)
                if (channel.Geometry != null) airHole.AppendMesh(BolusUtility.MeshGeometryToDMesh(channel.Geometry)); //some reason, first channel is null

            return airHole.Mesh;
        }

        private void Receive(AddAirChannelMessage message) {
            var point = message.point;

            if (_currentId == null) _currentId = 0;
            else _currentId++;

            _channels.Add(new AirChannelModel(new AirChannelStraight(point, _channelDiameter, _maxZHeight), _currentId));

            SendChannelsUpdate();
        }

        private void Receive(SetAirChannelDiameterMessage message) {
            _channelDiameter = message.diameter;

            SendSettingsUpdate();
        }

        private void Receive(ClearAirChannelsMessage message) {
            _channels.Clear();
            _currentId = null;

            SendChannelsUpdate();
        }
        #endregion
    }
}
