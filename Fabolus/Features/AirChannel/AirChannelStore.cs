using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.AirChannel.Channels;
using Fabolus.Features.Bolus;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel {
    #region Messages
    public sealed record AddAirChannelShapeMessage(AirChannelShape shape);
    public sealed record ClearAirChannelsMessage();
    public sealed record AirChannelsUpdatedMessage(List<AirChannelModel> channels, int? selectedIndex);
    public sealed record AirChannelSelectedMessage(int? channelIndex);

    //air channels
    public sealed record SetChannelMessage(ChannelBase channel);
    public sealed record SetChannelTypeMessage(int index);
    public sealed record ChannelUpdatedMessage(ChannelBase channel);

    //requests
    public class AirChannelsListRequestMessage : RequestMessage<List<string>> { }
    public class AirChannelsRequestMessage : RequestMessage<List<AirChannelModel>> { }
    public class AirChannelHeightRequestMessage : RequestMessage<double> { }
    public class AirChannelSelectedRequestMessage : RequestMessage<int> { }
    public class AirChannelMeshRequestMessage : RequestMessage<DMesh3> { }
    

    //air channel requests
    public class AirChannelToolRequestMessage : RequestMessage<ChannelBase> { }
    public class AirChannelToolIndexRequestMessage : RequestMessage<int> { }
    public class AirChannelVerticalRequestMessage : RequestMessage<VerticalChannel> { }
    public class AirChannelAngledRequestMessage : RequestMessage<AngledChannel> { }
    public class AirChannelPathRequestMessage : RequestMessage<PathChannel> { }

    #endregion

    public class AirChannelStore : RequestMessage<List<AirChannelModel>> {
        private List<AirChannelModel> _channels;
        private List<ChannelBase> _tools;

        private double _zHeightOffset = 20.0f;

        private double _maxZHeight {
            get {
                if (_bolus == null || _bolus.Geometry == null) return 0.0f;
                else return _bolus.Geometry.Bounds.Z + _bolus.Geometry.Bounds.SizeZ + _zHeightOffset;
            }
        }

        private BolusModel _bolus;
        private int _selectedChannel, _activeTool; //indexes 
        private int? _currentId; //used to identify air channels
        private ChannelBase _editChannel;

        public AirChannelStore() {
            _channels = new List<AirChannelModel>();
            _currentId = null;

            _tools = new List<ChannelBase> {
                new VerticalChannel(),
                new AngledChannel(),
                new PathChannel()
            };

            //registering messages
            WeakReferenceMessenger.Default.Register<AddAirChannelShapeMessage>(this, (r, m) => { AddAirChannel(m.shape); });
            WeakReferenceMessenger.Default.Register<AirChannelSelectedMessage>(this, (r, m) => { SetSelectedChannel(m.channelIndex); });
            WeakReferenceMessenger.Default.Register<ClearAirChannelsMessage>(this, (r, m) => { ClearChannels(); });
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m) => { UpdateBolus(m.bolus); });
            WeakReferenceMessenger.Default.Register<SetChannelMessage>(this, (r, m) => { UpdateChannel(m.channel); });
            WeakReferenceMessenger.Default.Register<SetChannelTypeMessage>(this, (r, m) => { SetActiveTool(m.index); });

            //request messages
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelsListRequestMessage>(this, (r, m) => {
                var result = new List<string>();
                r._tools.ForEach(t => result.Add(t.Name));
                m.Reply(result);
            });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelsRequestMessage>(this, (r, m) => { m.Reply(r._channels); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelHeightRequestMessage>(this, (r, m) => { m.Reply(r._maxZHeight); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelSelectedRequestMessage>(this, (r,m) => { m.Reply(r._selectedChannel); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelMeshRequestMessage>(this, (r, m) => { m.Reply(r.ToMesh()); });

            //air channel types
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelToolRequestMessage>(this, (r, m) => { m.Reply(_tools[_activeTool]); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelToolIndexRequestMessage>(this, (r, m) => { m.Reply(_activeTool); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelVerticalRequestMessage>(this, (r, m) => { m.Reply((VerticalChannel)_tools[0]); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelAngledRequestMessage>(this, (r,m) => { m.Reply((AngledChannel)_tools[1]); });
            WeakReferenceMessenger.Default.Register<AirChannelStore, AirChannelPathRequestMessage>(this, (r, m) => { m.Reply((PathChannel)_tools[2]); });

        }

        private void SendChannelsUpdate() => WeakReferenceMessenger.Default.Send(new AirChannelsUpdatedMessage(_channels, _selectedChannel));
        private void SendSettingsUpdate() => WeakReferenceMessenger.Default.Send(new ChannelUpdatedMessage(_tools[_activeTool]));

        #region Receiving
        private void UpdateBolus(BolusModel bolus) {
            _channels.Clear(); //changing the bolus means airholes no longer valid
            _currentId = null; //clear id count

            if (bolus == null || bolus.Geometry is null) return;
            _bolus = bolus;

            SendChannelsUpdate();
            SendSettingsUpdate();
        }

        //when clicking on an existing air channel or on empty air
        private void SetSelectedChannel(int? selectedIndex) {
            if (selectedIndex== null || selectedIndex >= _channels.Count) selectedIndex = -1; ;

            _selectedChannel = (int)selectedIndex;

            SendChannelsUpdate();

            //automatically set active tool to the type of the selected channel
            if (_selectedChannel < 0) return; //if no channels are selected, then don't do anything else
            //should this also return a tool view back to the default values?
            Type channelType = _channels[_selectedChannel].Shape.ChannelType;
            int index = _tools.FindIndex(t => t.GetType() == channelType);
            _activeTool= (int)index;
            _tools[_activeTool] = _channels[_selectedChannel].Shape.GetChannelSettings();
            SendSettingsUpdate();
            //from the selected channel, figure out the ChannelBase type
            //create a channelbase with the settings from the selected air channel shape
            //set the active tool index to that ChannelBase in _tools
            //the channel in _tools is updated with the settings from the selected channel model
        }

        private void SetActiveTool(int toolIndex) {
            if (toolIndex < 0 || toolIndex >= _tools.Count()) return;

            _activeTool = toolIndex;
            SendSettingsUpdate();

            //removing the selected channel for editing
            _selectedChannel = -1;
            SendChannelsUpdate();
        }

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

        private void UpdateChannel(ChannelBase channel) {
            int index = _tools.FindIndex(t => t.GetType() == channel.GetType());
            //TODO: if _selectedIndex isn't -1, there's a channel selected
            //set the temp channel instead and send that
            _tools[index] = channel;
            WeakReferenceMessenger.Default.Send(new ChannelUpdatedMessage(channel));

            if (_selectedChannel >= 0) {
                _channels[_selectedChannel].Shape.UpdateSettings(channel);
                WeakReferenceMessenger.Default.Send(new AirChannelsUpdatedMessage(_channels, _selectedChannel));
            }
        }

        private void ChangeChannelType(int index) {
            if (index == _selectedChannel) return;
            if(index >= _tools.Count) return;

            _selectedChannel = index;

            WeakReferenceMessenger.Default.Send(new ChannelUpdatedMessage(_tools[_selectedChannel]));
        }


        private void ClearChannels() {
            _channels.Clear();
            _currentId = null;

            SendChannelsUpdate();
        }

        #endregion
    }
}
