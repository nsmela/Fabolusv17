﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    public class RisingContourModel : ContourModelBase {
        public override string Name => "rising contour";
        public override float Offset => ((RisingContour)Contour).OffsetXY;

        public RisingContourModel() {
            ContourType = typeof(RisingContour);

            Contour = new RisingContour {
                OffsetXY = 5.0f,
                OffsetBottom = 2.5f,
                OffsetTop = 2.5f,
                Resolution = 2.0f
            };
            ViewModel = new RisingContourViewModel();
            ViewModel.Initialize();
        }
    }
}
