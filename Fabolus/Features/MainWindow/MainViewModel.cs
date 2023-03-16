using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Import;
using Fabolus.Features.Rotation;
using Fabolus.Features.Smoothing;
using g3;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.MainWindow {
    public partial class MainViewModel: ViewModelBase {

        private BolusStore _bolusStore;
        private TransformsStore _transformsStore;

        [ObservableProperty] private ViewModelBase? _currentViewModel;
        [ObservableProperty] private MeshViewModelBase _currentMeshView;
        [ObservableProperty] private string? _currentViewTitle;
        [ObservableProperty] private string? _bolusVolume;

        //mouse controls display
        [ObservableProperty] private string? _leftMouseLabel;
        [ObservableProperty] private string? _rightMouseLabel;
        [ObservableProperty] private string? _centreMouseLabel;


        #region Messages
        public sealed record NavigateToMessage(ViewModelBase viewModel);
        #endregion

        public MainViewModel() {
            _bolusStore= new BolusStore();
            _transformsStore= new TransformsStore();
            NavigateTo(new ImportViewModel());

            //messages
            WeakReferenceMessenger.Default.Register<NavigateToMessage>(this, (r,m) => { NavigateTo(m.viewModel); });
        }

        private void NavigateTo(ViewModelBase viewModel) {
            //copying camera position
            var meshView = viewModel.MeshViewModel;

            if (CurrentMeshView == null) {
                meshView.Camera = new PerspectiveCamera();
                meshView.Camera.Reset();
            } else meshView.Camera = CurrentMeshView.Camera;

            CurrentViewModel = viewModel;
            CurrentMeshView = meshView;
            CurrentViewTitle = viewModel.ViewModelTitle;

            //mouse control labels
            LeftMouseLabel= viewModel.LeftMouseLabel;
            RightMouseLabel= viewModel.RightMouseLabel;
            CentreMouseLabel = viewModel.CentreMouseLabel;
        }

        #region Commands
        [RelayCommand] public async Task SwitchToSmoothingView() => NavigateTo(new SmoothingViewModel());
        [RelayCommand] public async Task SwitchToImportView() => NavigateTo(new ImportViewModel());
        [RelayCommand] public async Task SwitchToRotationView() => NavigateTo(new RotationViewModel());

        #endregion
    }
}
