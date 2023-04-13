using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    public partial class BoxContourViewModel : ContourViewModelBase {
        private bool _isFrozen;
        private BoxContour _contour;

        [ObservableProperty] private float _offsetXY, _offsetTop, _offsetBottom, _resolution;
        partial void OnOffsetXYChanged(float value) => UpdateSettings();
        partial void OnOffsetTopChanged(float value) => UpdateSettings();
        partial void OnOffsetBottomChanged(float value) => UpdateSettings();
        partial void OnResolutionChanged(float value) => UpdateSettings();

        public override void Initialize(BoxContour contour = null) {
            if (contour == null) return; //TODO: contour = WeakReferenceMessenger.Default.Send<ContourRequestMessage>();
            _contour = contour;
            _isFrozen = true;
            OffsetXY = contour.OffsetXY; 
            OffsetBottom = contour.OffsetBottom;
            OffsetTop = contour.OffsetTop;
            Resolution = contour.Resolution;

            _isFrozen = false;
        }

        private void UpdateSettings() {
            if(_isFrozen) return;
            _isFrozen = true;

            _contour.OffsetXY = OffsetXY;
            _contour.OffsetTop = OffsetTop;
            _contour.OffsetBottom = OffsetBottom;
            _contour.Resolution = Resolution;

            //send update WeakReferenceMessenger.Default.Send();

            _isFrozen= false;
        }
    }
}
