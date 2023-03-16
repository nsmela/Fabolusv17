using Fabolus.Features.Bolus.Tools;
using g3;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        //the latest mesh
        public DMesh3 Mesh {
            get {
                if (_meshes.ContainsKey(SMOOTHED_BOLUS_LABEL)) return _meshes[SMOOTHED_BOLUS_LABEL];
                else return _meshes[ORIGINAL_BOLUS_LABEL];
            }
        }

        public DMesh3 TransformedMesh {
            get {
                var mesh = new DMesh3();
                mesh.Copy(Mesh);
                foreach (var q in _transforms) MeshTransforms.Rotate(mesh, Vector3d.Zero, q);
                return mesh;
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
            _meshes = new Dictionary<string, DMesh3>();
        }
        public BolusModel(DMesh3 mesh) {
            _transforms = new List<Quaterniond>();
            _meshes = new Dictionary<string, DMesh3>();

            AddMesh(ORIGINAL_BOLUS_LABEL, mesh);
        }

        public void AddMesh(string label, DMesh3 mesh) {
            //display meshes are not to be set
            if (label == DISPLAY_BOLUS_LABEL) return; //TODO: throw an error

            //if an original bolus, clear all other meshes
            if (label == ORIGINAL_BOLUS_LABEL) _meshes.Clear();

            //if no meshes exist, this mesh will be the original mesh
            if (_meshes.Count <= 0) label = ORIGINAL_BOLUS_LABEL;

            //replace mesh if already exists
            if (_meshes.ContainsKey(label)) _meshes[label] = mesh;
            else _meshes.Add(label, mesh);

            UpdateGeometry();
        }

        public void RemoveMesh(string label) {
            _meshes.Remove(label);
            UpdateGeometry();
        }

        public DMesh3 GetMesh(string label) => _meshes[label];

        public bool HasMesh(string label) => _meshes.ContainsKey(label);

        public void SetModelColor(Color color, float opacity) {
            var skin = new DiffuseMaterial(new SolidColorBrush(color));
            skin.Brush.Opacity = opacity;

            _model3D = new GeometryModel3D(Geometry, skin);
            _model3D.BackMaterial = skin;
        }

        public void AddTransform(Vector3d axis, double angle) {
            _transforms.Add(new Quaterniond(axis, angle));
            UpdateGeometry();
        }

        public void ClearTransforms() {
            _transforms.Clear();
            UpdateGeometry();
        }

        #endregion

        #region Private Methods
        private void UpdateGeometry() {
            _geometry = MeshConversion.DMeshToMeshGeometry(TransformedMesh);

            var color = Colors.Gray;
            var opacity = 1.0f;
            SetModelColor(color, opacity);
        }

        #endregion
    }
}
