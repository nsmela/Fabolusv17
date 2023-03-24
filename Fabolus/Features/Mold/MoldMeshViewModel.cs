using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold {
    public partial class MoldMeshViewModel : MeshViewModelBase {
        [ObservableProperty] private Model3DGroup _moldMesh;

        private DiffuseMaterial _moldPreviewSkin, _moldSkin;
        private MoldShape _moldShape;

        public MoldMeshViewModel() : base () {
            MoldMesh = new();

            _moldPreviewSkin = SetSkin(Colors.AliceBlue, 0.4f);
            _moldSkin = SetSkin(Colors.Red);

            WeakReferenceMessenger.Default.Register<MoldShapeUpdatedMessage>(this, (r,m) => { UpdateMold(m.shape); });

            var shape = WeakReferenceMessenger.Default.Send<MoldShapeRequestMessage>();
            UpdateMold(shape);
        }

        #region Receiving Messages
        private void UpdateMold(MoldShape shape) {
            _moldShape = shape;

            MoldMesh.Children.Clear();
            var model = new GeometryModel3D(_moldShape.Geometry, _moldPreviewSkin);
            model.BackMaterial = _moldPreviewSkin;
            MoldMesh.Children.Add(model);
        }

        #endregion

        #region Private Methods
        private DiffuseMaterial SetSkin(System.Windows.Media.Color colour, float opacity = 1.0f) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity = opacity;
            return new DiffuseMaterial(brush);
        }

            #endregion

        }
}
