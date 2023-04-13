using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.Channels;
using Fabolus.Features.AirChannel.MouseTools;
using Fabolus.Features.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Fabolus.Features.AirChannel {
    public partial class AirChannelViewModel : ViewModelBase {
        public override string ViewModelTitle => "air channels";
        public override string? LeftMouseLabel => "Add/Edit Air Channel";
        public override MeshViewModelBase MeshViewModel => new AirChannelMeshViewModel();

        [ObservableProperty] private int _activeToolIndex;
        partial void OnActiveToolIndexChanged(int value) {
            WeakReferenceMessenger.Default.Send(new SetChannelTypeMessage(value));
        }

        [ObservableProperty] private ChannelViewModelBase _channelViewModel;

        public ObservableCollection<string> ToolNames;

        public AirChannelViewModel() {
            List<string> names = WeakReferenceMessenger.Default.Send<AirChannelsListRequestMessage>();
            ToolNames = new();
            names.ForEach(n => ToolNames.Add(n));
            ActiveToolIndex = (int)WeakReferenceMessenger.Default.Send<AirChannelToolIndexRequestMessage>();

            //messaging receiving
            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => { NewChannel(m.channel); });

            //viewmodel for air channel controls
            ChannelBase channel = WeakReferenceMessenger.Default.Send<AirChannelToolRequestMessage>();
            ChannelViewModel = channel.ViewModel;
            ChannelViewModel.Initialize(); //have to initialize instead of relying on constructor to avoid self-referencing loops
        }

        #region Commands

        [RelayCommand] private void ClearAirChannels() => WeakReferenceMessenger.Default.Send(new ClearAirChannelsMessage());
        [RelayCommand] private void SetAirChannelToolStraight() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(0));
        [RelayCommand] private void SetAirChannelToolAngled() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(1));
        [RelayCommand] private void SetAirChannelToolPath() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(2));
        [RelayCommand] private void DeleteSelectedChannel() => WeakReferenceMessenger.Default.Send(new RemoveSelectedChannelMessage());
        #endregion

        #region Private Methods
        private void NewChannel(ChannelBase channel) {
            //receive message
            //set the new ViewModel
            //initialize the new ViewModel
            ChannelViewModel = channel.ViewModel;
            ChannelViewModel.Initialize();
        }
        #endregion
    }
}
