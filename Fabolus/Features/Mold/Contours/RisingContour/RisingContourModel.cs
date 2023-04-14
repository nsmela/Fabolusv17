using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    public class RisingContourModel : ContourModelBase {
        public RisingContourModel() {
            ContourType = typeof(RisingContour);

            Contour = new RisingContour {
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
