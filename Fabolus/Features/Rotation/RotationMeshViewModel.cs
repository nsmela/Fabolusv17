using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Rotation {
    public partial class RotationMeshViewModel : MeshViewModelBase {
        [ObservableProperty] private Vector3D _rotationAxis;
        [ObservableProperty] private float _rotationAngle;
        [ObservableProperty] private Vector3D _overhangAxis;

        partial void OnRotationAngleChanged(float value) {
            UpdateTempModel();
        }

        private MeshGeometry3D _geometry;

        #region Mesh Skin Material
        private float _meshOpacity = 1.0f;
        private DiffuseMaterial _meshSkinMaterial;
        #endregion

        public RotationMeshViewModel() : base() {

        }

        #region Receive Messages

        #endregion

        #region Private Methods
        protected override void Update(BolusModel bolus) {
            if (_meshSkinMaterial == null) Initialize(); //if the viewmodel hasn't been initialized yet

            //for easy reading
            _geometry = bolus.Geometry;
            if (_geometry == null || _geometry.TriangleIndices.Count <= 0) return;

            DisplayMesh.Children.Clear();

            //building geometry model
            var model = GetOverhangsModel();
            DisplayMesh.Children.Add(model);
        }

        private void Initialize() {
            //material for mesh skin set ahead to prevent multiple calls
            //can allow editing in viewer later
            // _meshSkinMaterial = OverhangSettings.OVERHANG_SKIN;
            //_meshSkinMaterial.Brush.Opacity = _meshOpacity;
            _meshSkinMaterial = WeakReferenceMessenger.Default.Send<BolusOverhangMaterialRequestMessage>();

            WeakReferenceMessenger.Default.Register<BolusOverhangsUpdated>(this, (r, m) => {
                _meshSkinMaterial = m.material;
                UpdateTempModel();
            });

            _geometry = new MeshGeometry3D();

            OverhangAxis = new Vector3D(0, 0, -1); //pointing downwards
        }

        private void UpdateTempModel() {
            DisplayMesh.Children.Clear();

            //building geometry model
            var model = GetTempOverhangs();
            DisplayMesh.Children.Add(model);
        }

        private GeometryModel3D GetTempOverhangs() {
            //Overhangs are displayed using a gradient brush and vertex texture coordinates
            //The angle of the vertex's normal vs the reference angle determines how far along the gradient 

            //apply temp rotation to reference axis
            var rotation = new AxisAngleRotation3D(RotationAxis, -RotationAngle);
            var rotate = new RotateTransform3D(rotation);
            var transform = new Transform3DGroup();
            transform.Children.Add(rotate);
            var refAngle = transform.Transform(OverhangAxis);

            //using the MeshSkin static class to generate mesh materials
            return  MeshSkin.GetTempOverhangs(_geometry, refAngle);
        }

        private GeometryModel3D GetOverhangsModel() => MeshSkin.GetTempOverhangs(_geometry, OverhangAxis);

        #endregion
    }
}
