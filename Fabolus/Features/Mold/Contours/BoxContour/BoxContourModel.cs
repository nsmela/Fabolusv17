using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    public class BoxContourModel : ContourModelBase {
        public override float Offset => ((BoxContour)Contour).OffsetXY;
            
        public BoxContourModel() {
            ContourType = typeof(BoxContour);

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
