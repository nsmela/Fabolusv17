using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.Channels;
using Fabolus.Features.AirChannel.Controls;
using Fabolus.Features.AirChannel.MouseTools;
using Fabolus.Features.Common;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Fabolus.Features.AirChannel {
    public partial class AirChannelViewModel : ViewModelBase {
        private List<string> _toolNames = new List<string> {
            "vertical channel", "angled channel", "path channel"
        };

        public override string ViewModelTitle => "air channels";
        public override string? LeftMouseLabel => "Add/Edit Air Channel";
        public override MeshViewModelBase MeshViewModel => new AirChannelMeshViewModel();

        [ObservableProperty] private int _activeToolIndex;
        partial void OnActiveToolIndexChanged(int value) {
            //WeakReferenceMessenger.Default.Send(new SetAirChannelTool(value));
            WeakReferenceMessenger.Default.Send(new SetChannelTypeMessage(value));
            ChannelName = _toolNames[value];
        }

        [ObservableProperty] private double _channelDiameter; //saved in air channel store to share with mesh view
        [ObservableProperty] private double _channelDepth;
        [ObservableProperty] private string _channelName = "air channel";
        [ObservableProperty] private ChannelControlViewModel _channelViewModel;
        partial void OnChannelDiameterChanged(double value) =>  WeakReferenceMessenger.Default.Send(new SetAirChannelDiameterMessage(value));
        partial void OnChannelDepthChanged(double value) => WeakReferenceMessenger.Default.Send(new SetAirChannelDiameterMessage(value));

        public AirChannelViewModel() {
            ChannelDiameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            ActiveToolIndex = (int)WeakReferenceMessenger.Default.Send<AirChannelSelectedRequestMessage>();
            ChannelName = _toolNames[ActiveToolIndex];

            //messaging receiving
            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => { NewChannel(m.channel); });

            //viewmodel for air channel controls
            VerticalChannel channel = WeakReferenceMessenger.Default.Send<AirChannelVerticalRequestMessage>();
            ChannelViewModel = channel.ViewModel;
            ChannelViewModel.Initialize(); //have to initialize instead of relying on constructor to avoid self-referencing loops

        }

        #region Commands

        [RelayCommand] private void ClearAirChannels() => WeakReferenceMessenger.Default.Send(new ClearAirChannelsMessage());
        [RelayCommand] private void SetAirChannelToolStraight() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(0));
        [RelayCommand] private void SetAirChannelToolAngled() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(1));
        [RelayCommand] private void SetAirChannelToolPath() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(2));
        //testing
        [RelayCommand] private void AddAirChannelPath() => WeakReferenceMessenger.Default.Send(new AddPathAirChannelMessage());
        #endregion

        #region Private Methods
        private void NewChannel(ChannelBase channel) {
            //receive message
            //set the new ViewModel
            //initialize the new ViewModel
            ChannelViewModel = channel.ViewModel;
            ChannelViewModel.Initialize();
            ChannelName = channel.Name;
        }
        #endregion
    }
}
