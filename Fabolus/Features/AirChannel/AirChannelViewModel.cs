using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.MouseTools;
using Fabolus.Features.Common;

namespace Fabolus.Features.AirChannel {
    public partial class AirChannelViewModel : ViewModelBase {
        public override string ViewModelTitle => "air channels";
        public override string? LeftMouseLabel => "Add/Edit Air Channel";
        public override MeshViewModelBase MeshViewModel => new AirChannelMeshViewModel();

        [ObservableProperty] private double _channelDiameter; //saved in air channel store to share with mesh view
        partial void OnChannelDiameterChanged(double value) =>  WeakReferenceMessenger.Default.Send(new SetAirChannelDiameterMessage(value));

        public AirChannelViewModel() {
            _channelDiameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
        }

        #region Commands

        [RelayCommand] private void ClearAirChannels() => WeakReferenceMessenger.Default.Send(new ClearAirChannelsMessage());
        [RelayCommand] private void SetAirChannelToolStraight() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(0));
        [RelayCommand] private void SetAirChannelToolAngled() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(1));
        [RelayCommand] private void SetAirChannelToolPath() => WeakReferenceMessenger.Default.Send(new SetAirChannelTool(2));
        //testing
        [RelayCommand] private void AddAirChannelPath() => WeakReferenceMessenger.Default.Send(new AddPathAirChannelMessage());
        #endregion
    }
}
