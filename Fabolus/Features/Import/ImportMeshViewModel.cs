using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Import
{
    public partial class ImportMeshViewModel : MeshViewModelBase {

        public ImportMeshViewModel() {
            DisplayMesh = new Model3DGroup();
            ZoomsWhenLoaded = true;

            //messages
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearBolusMessage>(this, (r, m) => { Receive(m); });

            //updated display mesh if one existed before switching to this view
            WeakReferenceMessenger.Default.Send<RequestBolusMessage>(new RequestBolusMessage(BolusStore.DISPLAY_BOLUS_LABEL));
        }

        #region Receive Messages
        private void Receive(BolusUpdatedMessage message) {
            //for easy reading
            var bolus = message.bolus;
            if(bolus.Model3D == null) return;

            DisplayMesh.Children.Clear();

            //building geometry model
            var geometryModel = bolus.Model3D;
            DisplayMesh.Children.Add(geometryModel);
        }

        private void Receive(ClearBolusMessage message) => DisplayMesh.Children.Clear();
        #endregion
    }
}
