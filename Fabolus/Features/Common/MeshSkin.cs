using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using System.Windows;

namespace Fabolus.Features.Common {
    public static class MeshSkin {
        public enum MeshColor {
            Bolus,
            Smoothed,
            Warning,
            Fault,
            AirChannelTool,
            AirChannelSelected,
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
                case MeshColor.AirChannelTool: return Colors.MediumPurple;
                case MeshColor.AirChannelSelected: return Colors.Turquoise;
                case MeshColor.AirChannel: return Colors.Violet;
                case MeshColor.MoldTemp: return Colors.Pink; 
                case MeshColor.MoldFinal: return Colors.Red;
            }
            return Colors.WhiteSmoke;
        }

        private static DiffuseMaterial GetOverHangSkin(float[] settings) {
            if(settings.Length != 2) {
                throw new NotImplementedException();
            }

            return GetOverHangSkin(settings[0], settings[1]);
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

        public static DiffuseMaterial GetMeshSkin(MeshColor colour, float opacity) {
            var brush = new SolidColorBrush(MeshSkinColor(colour));
            brush.Opacity = opacity;
            return new DiffuseMaterial(brush);
        }

        public static GeometryModel3D GetTempOverhangs(MeshGeometry3D mesh, Vector3D refAngle) {
            //Overhangs are displayed using a gradient brush and vertex texture coordinates
            //The angle of the vertex's normal vs the reference angle determines how far along the gradient 

            //using the transformed referance angle, generate texture coordinates to use with the gradient brush
            var texture = GetTextureCoords(mesh, refAngle);
            mesh.TextureCoordinates = texture;

            //get temp model
            float[] overhangSettings = WeakReferenceMessenger.Default.Send<BolusOverhangSettingsRequestMessage>();
            DiffuseMaterial skin = GetOverHangSkin(overhangSettings);
            var geometryModel = new GeometryModel3D(mesh, skin);
            geometryModel.BackMaterial = skin;
            return geometryModel;
        }

        private static PointCollection GetTextureCoords(MeshGeometry3D mesh, Vector3D refAxis) {
            if (mesh == null) return new PointCollection();

            var refAngle = 180.0f;
            var normals = mesh.Normals;

            PointCollection textureCoords = new PointCollection();
            foreach (var normal in normals) {
                double difference = Math.Abs(Vector3D.AngleBetween(normal, refAxis));

                while (difference > refAngle) difference -= refAngle;

                var ratio = difference / refAngle;

                textureCoords.Add(new Point(0, ratio));
            }

            return textureCoords;
        }
    }
}
