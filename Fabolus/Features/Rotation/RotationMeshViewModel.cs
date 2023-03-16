using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using gs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Rotation {
    public partial class RotationMeshViewModel : MeshViewModelBase {
        [ObservableProperty] private float _xAxisAngle, _yAxisAngle, zAxisAngle;
        [ObservableProperty] private Transform3DGroup _transformsGroup;
        [ObservableProperty] private Quaternion _rotationQuaternion;
        [ObservableProperty] private Vector3D _rotationAxis;
        [ObservableProperty] private float _rotationAngle;

        public RotationMeshViewModel() {
            DisplayMesh = new Model3DGroup();
            TransformsGroup = new Transform3DGroup();

            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<UpdatedTransformsMessage>(this, (r, m) => { Receive(m); });

            //updated display mesh if one existed before switching to this view
            WeakReferenceMessenger.Default.Send<RequestBolusMessage>(new RequestBolusMessage(BolusStore.DISPLAY_BOLUS_LABEL));
        }

        #region Receive Messages
        private void Receive(BolusUpdatedMessage message) {
            //for easy reading
            var bolus = message.bolus;
            if (bolus.Model3D == null) return;

            DisplayMesh.Children.Clear();

            //building geometry model
            var geometryModel = bolus.Model3D;
            DisplayMesh.Children.Add(geometryModel);
        }

        private void Receive(UpdatedTransformsMessage message) {
            RotationQuaternion = new Quaternion(message.x, message.y, message.z, message.w);
        }
        #endregion
    }
}
