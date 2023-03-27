﻿using CommunityToolkit.Mvvm.ComponentModel;
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

        [ObservableProperty] private double _offsetXY;
        [ObservableProperty] private double _offsetTop;
        [ObservableProperty] private double _offsetBottom;
        [ObservableProperty] private int _resolution;

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
            OffsetXY = settings.OffsetXY;
            OffsetTop = settings.OffsetTop; 
            OffsetBottom = settings.OffsetBottom;
            Resolution = settings.Resolution;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the Mold Settings in the Mold Store
        /// </summary>
        private void UpdateMoldSettings() {
            _settings.OffsetXY= OffsetXY;
            _settings.OffsetTop= OffsetTop;
            _settings.OffsetBottom= OffsetBottom;
            _settings.Resolution= Resolution;

            WeakReferenceMessenger.Default.Send(new MoldSetSettingsMessage(_settings));
        }

        #endregion


    }
}
