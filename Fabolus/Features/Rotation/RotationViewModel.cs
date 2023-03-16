using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Rotation {
    public partial class RotationViewModel : ViewModelBase {
        private bool _isLocked = false; //used to prevent events from triggering
        public override string ViewModelTitle => "rotation";
        public override MeshViewModelBase MeshViewModel => _meshViewModel;
        private RotationMeshViewModel _meshViewModel = new RotationMeshViewModel();

        [ObservableProperty] private float _xAxisAngle, _yAxisAngle, zAxisAngle;
        partial void OnXAxisAngleChanged(float value) => SendTempRotation(new Vector3D(1, 0, 0), value);
        partial void OnYAxisAngleChanged(float value) => SendTempRotation(new Vector3D(0, 1, 0), value);
        partial void OnZAxisAngleChanged(float value) => SendTempRotation(new Vector3D(0, 0, 1), value);

        //to save a temp rotation as a permenant rotation when slider is let go
        private void SendUpdatedRotation(Vector3 axis, float angle) {
            WeakReferenceMessenger.Default.Send(new AddRotationMessage(axis, angle));
        }

        //to save a temp roation while slider is active
        private void SendTempRotation(Vector3D axis, float angle) {
            if (_isLocked) return;

            _meshViewModel.RotationAxis = axis;
            _meshViewModel.RotationAngle = angle;
            //WeakReferenceMessenger.Default.Send(new AddTempRotationMessage(axis, angle));
        }

        private void ResetValues() {
            _isLocked = true;
            //setting slider values
            XAxisAngle = 0.0f;
            YAxisAngle = 0.0f;
            ZAxisAngle = 0.0f;
            _isLocked = false;

            _meshViewModel.RotationAxis = new Vector3D(0, 0, 0);
            _meshViewModel.RotationAngle = 0.0f;
        }

        #region Commands
        [RelayCommand]
        private Task ClearRotation() {
            ResetValues();

            return Task.Factory.StartNew(() => {
                WeakReferenceMessenger.Default.Send(new ClearTransformsMessage());
            });

        }

        [RelayCommand]
        private Task SaveRotation() {
            Vector3 axis = Vector3.Zero;
            float angle = 0;

            //selecting the axis that was changed
            if (XAxisAngle != 0) {
                axis = Vector3.UnitX;
                angle = XAxisAngle;
            }

            if (YAxisAngle != 0) {
                axis = Vector3.UnitY; 
                angle = YAxisAngle;
            }

            if (ZAxisAngle != 0) {
                axis = Vector3.UnitZ;
                angle = ZAxisAngle;
            }

            ResetValues();
            return Task.Factory.StartNew(() => {
                WeakReferenceMessenger.Default.Send(new AddRotationMessage(axis, angle));
            });

        }
        #endregion

        /* 
         Sliders first change the local transforms
        When a slider is let go, the transform is then saved to the bolus store
        then the bolus store's new transform is used
         
         */


        /* 
         // Create and apply a transformation that rotates the object.
        RotateTransform3D myRotateTransform3D = new RotateTransform3D();
        AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
        myAxisAngleRotation3d.Axis = new Vector3D(0,3,0);
        myAxisAngleRotation3d.Angle = 40;
        myRotateTransform3D.Rotation = myAxisAngleRotation3d;

        // Add the rotation transform to a Transform3DGroup
        Transform3DGroup myTransform3DGroup = new Transform3DGroup();
        myTransform3DGroup.Children.Add(myRotateTransform3D);
         
         
         */
    }

}
