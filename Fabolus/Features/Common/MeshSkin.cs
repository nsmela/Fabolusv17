using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Fabolus.Features.Common {
    public static class MeshSkin {
        public enum MeshColor {
            Bolus,
            Smoothed,
            Warning,
            Fault,
            AirChannelTemp,
            AirChannel,
            MoldTemp,
            MoldFinal
        }

        public static Color MeshSkinColor(MeshColor color) {
            switch (color) {
                case MeshColor.Bolus: return Colors.Gray;
                case MeshColor.Smoothed: return Colors.Blue;
                case MeshColor.Warning: return Colors.Yellow; 
                case MeshColor.Fault: return Colors.Red;
                case MeshColor.AirChannelTemp: return Colors.Turquoise;
                case MeshColor.AirChannel: return Colors.Violet;
                case MeshColor.MoldTemp: return Colors.Pink; 
                case MeshColor.MoldFinal: return Colors.Red;
            }
            return Colors.WhiteSmoke;
        }

        public static DiffuseMaterial GetOverHangSkin(float warningAngle, float faultAngle) {
            float lowerOffset = warningAngle / 90.0f;
            float upperOffset = faultAngle / 90.0f;
            float startingOffset = MathF.Max(lowerOffset - 0.1f, 0.0f);

            //gradient color used for overhang display texture
            var gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new System.Windows.Point(0, 0);
            gradientBrush.EndPoint = new System.Windows.Point(0, 1);

            gradientBrush.GradientStops.Add(new GradientStop {
                Color = MeshSkinColor(MeshColor.Bolus),
                Offset = startingOffset
            });
            gradientBrush.GradientStops.Add(new GradientStop {
                Color = MeshSkinColor(MeshColor.Warning),
                Offset = lowerOffset
            }); ;
            gradientBrush.GradientStops.Add(new GradientStop {
                Color = MeshSkinColor(MeshColor.Fault),
                Offset = upperOffset
            });
            return new DiffuseMaterial(gradientBrush);
            
        }
    }
}
