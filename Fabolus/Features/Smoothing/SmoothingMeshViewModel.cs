using CommunityToolkit.Mvvm.ComponentModel;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Import;

namespace Fabolus.Features.Smoothing
{
    public partial class SmoothingMeshViewModel : MeshViewModelBase {

        #region Mesh Skin Material
        private Color _meshSkinColor = Colors.Blue;
        private float _meshOpacity = 1.0f;
        private DiffuseMaterial _meshSkinMaterial;
        #endregion

        public SmoothingMeshViewModel() {
                DisplayMesh = new Model3DGroup();

                //material for mesh skin set ahead to prevent multiple calls
                //can allow editing in viewer later
                _meshSkinMaterial = new DiffuseMaterial(new SolidColorBrush(_meshSkinColor));
                _meshSkinMaterial.Brush.Opacity = _meshOpacity;

                //messages
                WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Receive(m); });

                //updated display mesh if one existed before switching to this view
                WeakReferenceMessenger.Default.Send<RequestBolusMessage>(new RequestBolusMessage(BolusStore.DISPLAY_BOLUS_LABEL));
        }

        #region Receive Messages
        private void Receive(BolusUpdatedMessage message) {
            //for easy reading
            var label = message.label;
            var bolus = message.bolus;
            if (bolus.Geometry == null) return;

            DisplayMesh.Children.Clear();

            //building geometry model
            //coloring the model when it's been smoothed
            GeometryModel3D geometryModel;
            if (label == BolusStore.SMOOTHED_BOLUS_LABEL) {
                geometryModel = new GeometryModel3D(bolus.Geometry, _meshSkinMaterial);
                geometryModel.BackMaterial = _meshSkinMaterial;
            }else  geometryModel = bolus.Model3D; 

            DisplayMesh.Children.Add(geometryModel);
        }

        #endregion
    }
}
