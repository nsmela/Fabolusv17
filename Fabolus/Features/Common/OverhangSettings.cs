using System.Windows.Media;

namespace Fabolus.Features.Common {
    public static class OverhangSettings {
        public static float OVERHANG_GOOD => 0.75f;
        public static float OVERHANG_WARNING => 0.80f;
        public static float OVERHANG_FAULT => 0.90f;
        public static Color OVERHANG_COLOR_GOOD => Colors.Gray;
        public static Color OVERHANG_COLOR_WARNING => Colors.Yellow;
        public static Color OVERHANG_COLOR_FAULT => Colors.Red;
    }
}
