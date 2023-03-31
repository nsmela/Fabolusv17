using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Common {
    public static class OverhangSettings {
        public static float OVERHANG_GOOD => 0.75f;
        public static float OVERHANG_WARNING => 0.80f;
        public static float OVERHANG_FAULT => 0.90f;
        public static Color OVERHANG_COLOR_GOOD => Colors.Gray;
        public static Color OVERHANG_COLOR_WARNING => Colors.Yellow;
        public static Color OVERHANG_COLOR_FAULT => Colors.Red;

        public static DiffuseMaterial OVERHANG_SKIN {
            get {
                //gradient color used for overhang display texture
                var gradientBrush = new LinearGradientBrush();
                gradientBrush.StartPoint = new System.Windows.Point(0, 0);
                gradientBrush.EndPoint = new System.Windows.Point(0, 1);

                gradientBrush.GradientStops.Add(new GradientStop {
                    Color = OverhangSettings.OVERHANG_COLOR_GOOD,
                    Offset = OverhangSettings.OVERHANG_GOOD
                });
                gradientBrush.GradientStops.Add(new GradientStop {
                    Color = OverhangSettings.OVERHANG_COLOR_WARNING,
                    Offset = OverhangSettings.OVERHANG_WARNING
                }); ;
                gradientBrush.GradientStops.Add(new GradientStop {
                    Color = OverhangSettings.OVERHANG_COLOR_FAULT,
                    Offset = OverhangSettings.OVERHANG_FAULT
                });
                return new DiffuseMaterial(gradientBrush);
            }
        }
    }
}
