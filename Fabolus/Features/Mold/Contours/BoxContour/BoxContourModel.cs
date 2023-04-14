using Fabolus.Features.Mold.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    public class BoxContourModel : ContourModelBase {
        public override Type ContourType => typeof(BoxContour);
        public override ContourBase Contour { get; set; }
        public override ContourViewModelBase ViewModel { get; protected set; }

        public BoxContourModel() {
            Contour = new BoxContour {
                OffsetXY = 5.0f,
                OffsetBottom = 2.5f,
                OffsetTop = 2.5f,
                Resolution = 2.0f
            };
            ViewModel = new BoxContourViewModel();
            ViewModel.Initialize();
        }
    }
}
