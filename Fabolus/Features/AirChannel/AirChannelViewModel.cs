using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public override MeshViewModelBase MeshViewModel => new AirChannelMeshViewModel();

        [ObservableProperty] private float _channelDiameter = 5.0f;
        partial void OnChannelDiameterChanged(float value) {
            //update airchannel store
        }

        #region Commands
        [RelayCommand]
        private void AddAirChannel() {

        }

        [RelayCommand]
        private void ClearAirChannels() {

        }
        #endregion
    }
}
