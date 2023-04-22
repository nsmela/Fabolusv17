using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Common;
using Fabolus.Features.Mold.Contours;
using Fabolus.Features.Mold.Tools;
using System.Collections.Generic;
using Fabolus.Features.Bolus;
using System.Diagnostics;
using System;
using System.Windows;
using Fabolus.Features.Helpers;

namespace Fabolus.Features.Mold {
    public partial class MoldViewModel : ViewModelBase {
        public override string? ViewModelTitle => "mold";
        public override MeshViewModelBase MeshViewModel => new MoldMeshViewModel();

        [ObservableProperty] private ContourViewModelBase _contourViewModel;
        [ObservableProperty] private int _activeContourIndex;
        [ObservableProperty] private List<string> _shapeNames;
        private ContourModelBase _contour;

        public MoldViewModel() {
            ShapeNames = WeakReferenceMessenger.Default.Send<MoldListRequestMessage>();
            _contour = WeakReferenceMessenger.Default.Send<MoldContourRequestMessage>();
            ActiveContourIndex = ShapeNames.FindIndex(s => s == _contour.Name);
            ContourViewModel = _contour.ViewModel;
            ContourViewModel.Initialize(_contour.Contour);

            //messaging receiving
            WeakReferenceMessenger.Default.Register<MoldContourUpdatedMessage>(this, (r, m) => {
                _contour = m.contour;
                ContourViewModel = _contour.ViewModel;
            });
        }

        #region Messages
        private void SetContour(int index) => WeakReferenceMessenger.Default.Send(new MoldSetContourIndexMessage(index));
        
        
        #endregion

        #region Private Methods


        #endregion

        #region Commands
        [RelayCommand] private void GenerateMold() {
            //how to create a shape with MoldShape and have it save and sent to MoldMeshView?
            //mold store has a generate mold message? it stores it? /clears it if airholes, rotation, smoothing, or mold settings change?
            //does the shape hold it?
            //mesh view needs to know if one exists when opening
            //MoldShape shape = WeakReferenceMessenger.Default.Send<MoldShapeRequestMessage>();
            
            var mesh = MoldUtility.GenerateMold(_contour.Contour);
            var geometry = mesh.ToGeometry(); 
            WeakReferenceMessenger.Default.Send(new MoldSetFinalShapeMessage(geometry));
         }

        #endregion



    }
}
