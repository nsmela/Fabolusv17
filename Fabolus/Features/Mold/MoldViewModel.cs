using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Common;
using Fabolus.Features.Mold.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold {
    public partial class MoldViewModel : ViewModelBase {
        public override string? ViewModelTitle => "mold";
        public override MeshViewModelBase MeshViewModel => new MoldMeshViewModel();

        [ObservableProperty] private double _offsetXY;
        [ObservableProperty] private double _offsetTop;
        [ObservableProperty] private double _offsetBottom;
        [ObservableProperty] private int _resolution;
        [ObservableProperty] private bool _isBusy = false;

        partial void OnOffsetXYChanged(double value) => UpdateMoldSettings();
        partial void OnOffsetTopChanged(double value) => UpdateMoldSettings();
        partial void OnOffsetBottomChanged(double value) => UpdateMoldSettings();
        partial void OnResolutionChanged(int value) => UpdateMoldSettings();

        private MoldStore.MoldSettings _settings;

        public MoldViewModel() {
            _settings = WeakReferenceMessenger.Default.Send<MoldSettingsRequestMessage>();
            UpdateSettings(_settings);
        }

        #region Messages
        private void UpdateSettings(MoldStore.MoldSettings settings) {
            if (IsBusy) return;
            IsBusy= true;
            OffsetXY = settings.OffsetXY;
            OffsetTop = settings.OffsetTop; 
            OffsetBottom = settings.OffsetBottom;
            Resolution = settings.Resolution;

            IsBusy= false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the Mold Settings in the Mold Store
        /// </summary>
        private void UpdateMoldSettings() {
            if(IsBusy) return;
            _settings.OffsetXY= OffsetXY;
            _settings.OffsetTop= OffsetTop;
            _settings.OffsetBottom= OffsetBottom;
            _settings.Resolution= Resolution;

            WeakReferenceMessenger.Default.Send(new MoldSetSettingsMessage(_settings));
        }

        #endregion

        #region Commands
        [RelayCommand] private void GenerateMold() {
            MoldShape shape = WeakReferenceMessenger.Default.Send<MoldShapeRequestMessage>();
            //how to create a shape with MoldShape and have it save and sent to MoldMeshView?
            //mold store has a generate mold message? it stores it? /clears it if airholes, rotation, smoothing, or mold settings change?
            //does the shape hold it?
            //mesh view needs to know if one exists when opening
        }

        #endregion



    }
}
