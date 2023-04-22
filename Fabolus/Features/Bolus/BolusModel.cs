using Fabolus.Features.Bolus;
using Fabolus.Features.Helpers;
using g3;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Bolus {

    public partial class BolusModel {
        #region Bolus Types
        public const string ORIGINAL_BOLUS_LABEL = "original"; //imported model
        public const string SMOOTHED_BOLUS_LABEL = "smoothed"; //smoothed model
        public const string DISPLAY_BOLUS_LABEL = "display"; //latest model
        public const string REFINED_BOLUS_LABEL = "refined"; //mesh has been reduced
        #endregion

        private Dictionary<string, DMesh3> _meshes;

        //bolus mesh skin info to skip generating it every time
        private Color _meshSkinColor = Colors.Gray;
        private float _meshOpacity = 1.0f;
        private DiffuseMaterial _meshSkinMaterial;

        //experiemental, used to quicken spatial inquiries
        //private DMeshAABBTree3 _spatial;
        public Bitmap3 Map { get; private set; }

        #region Properties and Fields
        //the latest mesh
        public DMesh3 Mesh {
            get {
                if (_meshes.ContainsKey(REFINED_BOLUS_LABEL)) return _meshes[REFINED_BOLUS_LABEL];
                if (_meshes.ContainsKey(SMOOTHED_BOLUS_LABEL)) return _meshes[SMOOTHED_BOLUS_LABEL];
                if (_meshes.ContainsKey(ORIGINAL_BOLUS_LABEL)) return _meshes[ORIGINAL_BOLUS_LABEL];
                return null;
            }
        }

        public DMesh3 RawMesh => _meshes.ContainsKey(ORIGINAL_BOLUS_LABEL) ? _meshes[ORIGINAL_BOLUS_LABEL] : new DMesh3();
        private DMesh3 _transformedMesh;
        public DMesh3 TransformedMesh => _transformedMesh;

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

            SetModelColor(_meshSkinColor, _meshOpacity);
        }
        public BolusModel(DMesh3 mesh) {
            mesh = BolusUtility.OrientationCentre(mesh);
            _transforms = new List<Quaterniond>();
            _meshes = new Dictionary<string, DMesh3>();
            SetModelColor(_meshSkinColor, _meshOpacity);

            AddMesh(ORIGINAL_BOLUS_LABEL, mesh);
        }

        public void AddMesh(string label, DMesh3 mesh) {
            //centres the mesh
            mesh = BolusUtility.OrientationCentre(mesh);

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
            _meshSkinMaterial = new DiffuseMaterial(new SolidColorBrush(color));
            _meshSkinMaterial.Brush.Opacity = opacity;

            GenerateModel();
        }

        public void AddTransform(Vector3d axis, double angle) {
            _transforms.Add(new Quaterniond(axis, angle));
            UpdateGeometry();
        }

        public void ClearTransforms() {
            _transforms.Clear();
            UpdateGeometry();
        }

        public List<Point3D> GetGeoDist(Point3D startPoint, int startTriangle, Point3D endPoint, int endTriangle) { 
            return ShortestPath(startPoint, startTriangle, endPoint, endTriangle);
        }

        public List<Point3D> GetDirectPath(Point3D start, Point3D end, int startTriangleIndex, int endTriangleIndex) {
            var startV = new Vector3d(start.X, start.Y, start.Z);
            var endV = new Vector3d(end.X, end.Y, end.Z);

            var path = BolusUtility.GetDirectPath(TransformedMesh, startV, endV, startTriangleIndex, endTriangleIndex);
            return null;
        }
        #endregion

        #region Private Methods
        private void UpdateGeometry() {
            //creates transformed mesh
            _transformedMesh = new DMesh3();
            _transformedMesh.Copy(Mesh);

            foreach(var q in _transforms) MeshTransforms.Rotate(_transformedMesh, Vector3d.Zero, q);

            //creates new MeshGeometry3D
            _geometry = TransformedMesh.ToGeometry(); 
            GenerateModel();
        }

        private void GenerateModel() {
            _model3D = new GeometryModel3D(Geometry, _meshSkinMaterial);
            _model3D.BackMaterial = _meshSkinMaterial;
        }


        #endregion
    }
}
