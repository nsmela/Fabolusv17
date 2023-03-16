using Fabolus.Features.Bolus.Tools;
using g3;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Bolus {

    public class BolusModel {
        #region Bolus Types
        public const string ORIGINAL_BOLUS_LABEL = "original"; //imported model
        public const string SMOOTHED_BOLUS_LABEL = "smoothed"; //smoothed model
        public const string DISPLAY_BOLUS_LABEL = "display"; //latest model
        #endregion

        private Dictionary<string, DMesh3> _meshes;

        #region Properties and Fields
        private DMesh3 _mesh;
        private DMesh3 _transformedMesh;
        public DMesh3 RawMesh => _mesh;
        public DMesh3 Mesh {
            get {
                if (_transforms == null || _transforms.Count <= 0) return _mesh;
                return _transformedMesh;
            }
        }

        private MeshGeometry3D _geometry;
        public MeshGeometry3D Geometry => _geometry;

        private GeometryModel3D _model3D;
        public GeometryModel3D Model3D => _model3D;

        private List<Quaterniond> _transforms;
        #endregion

        #region Public Methods
        public BolusModel() {
            _transforms = new List<Quaterniond>();
        }

        public void SetModelColor(Color color, float opacity) {
            var skin = new DiffuseMaterial(new SolidColorBrush(color));
            skin.Brush.Opacity = opacity;

            _model3D = new GeometryModel3D(Geometry, skin);
            _model3D.BackMaterial = skin;
        }

        public BolusModel(DMesh3 mesh) {
            _mesh = mesh;
            UpdateGeometry();
        }

        public void AddTransform(Vector3d axis, double angle) {
            _transforms.Add(new Quaterniond(axis, angle));

            //applying transforms to mesh
            _transformedMesh = new DMesh3(_mesh);
            foreach (var q in _transforms) MeshTransforms.Rotate(_transformedMesh, Vector3d.Zero, q);
            UpdateGeometry();
        }

        public void ClearTransforms() {
            _transforms.Clear();
            UpdateGeometry();
        }

        #endregion

        #region Private Methods
        private void UpdateGeometry() {
            _geometry = MeshConversion.DMeshToMeshGeometry(Mesh);

            var color = Colors.Gray;
            var opacity = 1.0f;
            SetModelColor(color, opacity);
        }
        #endregion
    }
}
