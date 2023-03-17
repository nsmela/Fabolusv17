using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Common;
using Fabolus.Features.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.AirChannel {
    public partial class AirChannelViewModel : ViewModelBase {
        public override string ViewModelTitle => "air channels";
        public override string? LeftMouseLabel => "Add/Edit Air Channel";
        public override MeshViewModelBase MeshViewModel => new AirChannelMeshViewModel();

        [ObservableProperty] private double _channelDiameter; //saved in air channel store to share with mesh view

        public AirChannelViewModel() {
            _channelDiameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
        }

        partial void OnChannelDiameterChanged(double value) {
            //update airchannel store
            WeakReferenceMessenger.Default.Send(new SetAirChannelDiameterMessage(value));
        }

        #region Commands

        [RelayCommand]
        private void ClearAirChannels() => WeakReferenceMessenger.Default.Send(new ClearAirChannelsMessage());
        #endregion
    }
}
