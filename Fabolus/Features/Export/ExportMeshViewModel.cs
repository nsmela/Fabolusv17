using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Mold;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Export {
    public partial class ExportMeshViewModel : MeshViewModelBase {

        protected override void Update(BolusModel bolus) {
            //inherits from MeshViewModelBase, whose constructor automatically loads the bolus  DisplayMesh
            //need to have display mesh use a different mesh
            MeshGeometry3D? mesh = WeakReferenceMessenger.Default.Send<MoldFinalRequestMessage>();
            if (mesh == null) { base.Update(bolus); return; }

            _displayMesh.Children.Clear();

            var skin = new DiffuseMaterial(new SolidColorBrush {
                Color = Colors.IndianRed,
                Opacity = 0.8f
            });

            var model = new GeometryModel3D(mesh, skin);
            model.BackMaterial = skin;
            DisplayMesh.Children.Add(model);
        }

    }
}
