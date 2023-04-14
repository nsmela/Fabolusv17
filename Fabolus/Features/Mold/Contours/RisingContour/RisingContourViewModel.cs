using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    public partial class RisingContourViewModel : ContourViewModelBase {
        private bool _isFrozen;
        private RisingContour _contour;

        [ObservableProperty] private float _offsetXY, _offsetTop, _offsetBottom, _resolution;
        partial void OnOffsetXYChanged(float value) => UpdateSettings();
        partial void OnOffsetTopChanged(float value) => UpdateSettings();
        partial void OnOffsetBottomChanged(float value) => UpdateSettings();
        partial void OnResolutionChanged(float value) => UpdateSettings();

        public override void Initialize(ContourBase contour = null) {
            if (contour == null) return;
            if (contour.GetType() != typeof(RisingContour)) return;

            _contour = contour as RisingContour;
            _isFrozen = true;
            OffsetXY = _contour.OffsetXY;
            OffsetBottom = _contour.OffsetBottom;
            OffsetTop = _contour.OffsetTop;
            Resolution = _contour.Resolution;
            _contour.Calculate();

            _isFrozen = false;
        }

        //TODO: convert to an async task? cancelable if the same request is made?
        //moving the slider would cancel the existing calculation
        private void UpdateSettings() {
            if (_isFrozen) return;
            _isFrozen = true;

            _contour.OffsetXY = OffsetXY;
            _contour.OffsetTop = OffsetTop;
            _contour.OffsetBottom = OffsetBottom;
            _contour.Resolution = Resolution;
            _contour.Calculate();

            WeakReferenceMessenger.Default.Send(new MoldSetContourMessage(_contour));

            _isFrozen = false;
        }
    }
}
