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

        [ObservableProperty] private float _xAxisAngle, _yAxisAngle, _zAxisAngle, _lowerOverhang, _upperOverhang;
        partial void OnXAxisAngleChanged(float value) => SendTempRotation(new Vector3D(1, 0, 0), value);
        partial void OnYAxisAngleChanged(float value) => SendTempRotation(new Vector3D(0, 1, 0), value);
        partial void OnZAxisAngleChanged(float value) => SendTempRotation(new Vector3D(0, 0, 1), value);

        //to save a temp roation while slider is active
        private void SendTempRotation(Vector3D axis, float angle) {
            if (_isLocked) return;

            _meshViewModel.RotationAxis = axis;
            _meshViewModel.RotationAngle = angle;
        }

        private bool _isOverhangsFrozen;
        partial void OnLowerOverhangChanged(float value) => SendOverhangRange();
        partial void OnUpperOverhangChanged(float value) => SendOverhangRange();
        private void SendOverhangRange() {
            if(_isOverhangsFrozen) return;
            _isOverhangsFrozen = true;

            var lowerValue = LowerOverhang;
            var upperValue = UpperOverhang;
            
            //send the value
            WeakReferenceMessenger.Default.Send(new ApplyOverhangSettingsMessage(lowerValue, upperValue));
            _isOverhangsFrozen= false;
        }

        public RotationViewModel() {
            //grab existing overhang values
            float[] settings = WeakReferenceMessenger.Default.Send<BolusOverhangSettingsRequestMessage>();
            _isOverhangsFrozen = true; //preventing updating the bolus store with values it already has
            LowerOverhang = settings[0];
            UpperOverhang = settings[1];
            _isOverhangsFrozen = false;
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
        private void ClearRotation() {
            ResetValues();
            WeakReferenceMessenger.Default.Send(new ClearRotationsMessage());
        }

        [RelayCommand]
        private void SaveRotation() {
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
            WeakReferenceMessenger.Default.Send(new ApplyRotationMessage(axis, angle));
        }
        #endregion
    }

}
