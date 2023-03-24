using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold {
    public partial class MoldViewModel : ViewModelBase {
        public override string? ViewModelTitle => "mold";
        public override MeshViewModelBase MeshViewModel => new MoldMeshViewModel();

        [ObservableProperty] private double offsetXY;
        [ObservableProperty] private double offsetTop;
        [ObservableProperty] private double offsetBottom;
        [ObservableProperty] private int _resolution;

        partial void OnOffsetXYChanged(double value) {
            _settings.OffsetXY = value;
            WeakReferenceMessenger.Default.Send(new MoldSetSettingsMessage(_settings));
        }

        private MoldStore.MoldSettings _settings;

        public MoldViewModel() {
            _settings = WeakReferenceMessenger.Default.Send<MoldSettingsRequestMessage>();
            UpdateSettings(_settings);
        }

        #region Commands
        private void UpdateSettings(MoldStore.MoldSettings settings) {
            OffsetXY = settings.OffsetXY;
            OffsetTop = settings.OffsetTop; 
            OffsetBottom = settings.OffsetBottom;
            Resolution = settings.Resolution;
        }
        #endregion

    }
}
