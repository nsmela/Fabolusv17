using Fabolus.Features.Common;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Fabolus.Features.Bolus;

namespace Fabolus.Features.Smoothing
{
    public partial class SmoothingMeshViewModel : MeshViewModelBase {
        public SmoothingMeshViewModel() : base() {
        }

        #region Receive Messages
        

        #endregion

        #region Private Methods
        protected override void Update(BolusModel bolus) {
            if (bolus.Geometry == null) return;

            DisplayMesh.Children.Clear();

            //building geometry model
            //coloring the model when it's been smoothed
            GeometryModel3D geometryModel;
            if (bolus.HasMesh(BolusModel.SMOOTHED_BOLUS_LABEL)) geometryModel = MeshSkin.GetColouredModel(bolus.Geometry, MeshSkin.MeshColor.Smoothed);
            else  geometryModel = MeshSkin.GetColouredModel(bolus.Geometry, MeshSkin.MeshColor.Bolus);

            DisplayMesh.Children.Add(geometryModel);
        }

        #endregion
    }
}
