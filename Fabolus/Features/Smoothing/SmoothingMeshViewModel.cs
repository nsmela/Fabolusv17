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

        public SmoothingMeshViewModel() : base() {
        }

        #region Receive Messages
        

        #endregion

        #region Private Methods
        protected override void Update(BolusModel bolus) {
            if (bolus.Geometry == null) return;
            if (_meshSkinMaterial == null) UpdateSkin();

            DisplayMesh.Children.Clear();

            //building geometry model
            //coloring the model when it's been smoothed
            GeometryModel3D geometryModel;
            if (bolus.HasMesh(BolusModel.SMOOTHED_BOLUS_LABEL)) {
                geometryModel = new GeometryModel3D(bolus.Geometry, _meshSkinMaterial);
                geometryModel.BackMaterial = _meshSkinMaterial;
            } else geometryModel = bolus.Model3D;

            DisplayMesh.Children.Add(geometryModel);
        }

        private void UpdateSkin() {
            //material for mesh skin set ahead to prevent multiple calls
            //can allow editing in viewer later
            _meshSkinMaterial = new DiffuseMaterial(new SolidColorBrush(_meshSkinColor));
            _meshSkinMaterial.Brush.Opacity = _meshOpacity;
        }
        #endregion
    }
}
