using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Shapes {
    public partial class ContourBoxViewModel : MoldShapeViewModelBase {
        //settings for the contourbox shape
        [ObservableProperty] private float _offsetXY, _offsetBottom, _offsetTop, _resolution;
        partial void OnOffsetXYChanged(float value) => UpdateSettings();
        partial void OnOffsetBottomChanged(float value) => UpdateSettings();
        partial void OnOffsetTopChanged(float value) => UpdateSettings();
        partial void OnResolutionChanged(float value) => UpdateSettings();

        private MoldBox _mold;

        public override void Initialize() {
            _isFrozen = true;
            //get the latest mold

            //parse the settings into the properties
            OffsetXY = _mold.OffsetXY;
            OffsetBottom = _mold.OffsetBottom;
            OffsetTop = _mold.OffsetTop;
            Resolution = _mold.Resolution;

            _isFrozen = false;
        }

        private void UpdateSettings() {
            if (_isFrozen) return;
            _isFrozen = true;

            _mold.OffsetXY = OffsetXY;
            _mold.OffsetBottom= OffsetBottom;
            _mold.OffsetTop= OffsetTop;
            _mold.Resolution = Resolution;

            //send updated shape to MoldStore

            _isFrozen= false;
        }
    }
}
