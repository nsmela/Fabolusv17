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
            _meshSkinMaterial = OverhangSettings.OVERHANG_SKIN;
            _meshSkinMaterial.Brush.Opacity = _meshOpacity;

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

            //using the transformed referance angle, generate texture coordinates to use with the gradient brush
            var texture = GetTextureCoords(_geometry, refAngle);
            _geometry.TextureCoordinates = texture;

            //get temp model
            GeometryModel3D geometryModel = new GeometryModel3D(_geometry, _meshSkinMaterial);
            geometryModel.BackMaterial = _meshSkinMaterial;
            return geometryModel;
        }

        private GeometryModel3D GetOverhangsModel() {
            var texture = GetTextureCoords(_geometry, OverhangAxis);
            _geometry.TextureCoordinates = texture;

            GeometryModel3D geometryModel = new GeometryModel3D(_geometry, _meshSkinMaterial);
            geometryModel.BackMaterial = _meshSkinMaterial;
            return geometryModel;
        }

        private PointCollection GetTextureCoords(MeshGeometry3D mesh, Vector3D refAxis) {
            var refAngle = 180.0f;
            var normals = mesh.Normals;

            PointCollection textureCoords= new PointCollection();
            foreach(var normal in normals) {
                double difference = Math.Abs(Vector3D.AngleBetween(normal, refAxis));

                while (difference > refAngle) difference -= refAngle;

                var ratio = difference / refAngle;

                textureCoords.Add(new System.Windows.Point(0, ratio));
            }

            return textureCoords;
        }
        #endregion
    }
}
