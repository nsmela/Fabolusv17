using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Export;
using Fabolus.Features.Import;
using Fabolus.Features.Mold;
using Fabolus.Features.Rotation;
using Fabolus.Features.Smoothing;
using g3;
using HelixToolkit.Wpf;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.MainWindow {
    public partial class MainViewModel: ViewModelBase {

        //stores
        private BolusStore _bolusStore;
        private AirChannelStore _airChannelsStore;
        private MoldStore _moldStore;

        [ObservableProperty] private ViewModelBase? _currentViewModel;
        [ObservableProperty] private MeshViewModelBase _currentMeshView;
        [ObservableProperty] private string? _currentViewTitle;

        //mouse controls display
        [ObservableProperty] private string? _leftMouseLabel;
        [ObservableProperty] private string? _rightMouseLabel;
        [ObservableProperty] private string? _centreMouseLabel;

        //mesh info
        [ObservableProperty] private bool _infoVisible;
        [ObservableProperty] private string _filePath, _fileSize, _triangleCount, _volumeText;

        #region Messages
        public sealed record NavigateToMessage(ViewModelBase viewModel);
        #endregion

        public MainViewModel() {
            _bolusStore= new BolusStore();
            _airChannelsStore= new AirChannelStore();
            _moldStore= new MoldStore();
            NavigateTo(new ImportViewModel());

            //messages
            WeakReferenceMessenger.Default.Register<NavigateToMessage>(this, (r,m) => { NavigateTo(m.viewModel); });
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { BolusUpdated(m.bolus); });

            InfoVisible = false;
            FilePath = string.Empty;
            FileSize = "No model loaded";
            TriangleCount = "No model loaded";
            VolumeText = "No model loaded";
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

        private void BolusUpdated(BolusModel bolus) {
            string filepath = WeakReferenceMessenger.Default.Send<BolusFilePathRequestMessage>();
            FilePath = string.Empty;
            InfoVisible= false;

            if (filepath != null && filepath != string.Empty) {
                var fileInfo = new FileInfo(filepath);
                FileSize = string.Format("{0:0,0.00} KB", (fileInfo.Length / 1024));
                FilePath = fileInfo.Name;
                InfoVisible = true;
            }
            else FileSize = "N/A";

            if (bolus.Mesh != null) TriangleCount = string.Format("{0:0,0}", bolus.Mesh.TriangleCount);
            else TriangleCount = "N/A";

            //volumes
            if (bolus.Mesh != null) {
                var volumeArea = MeshMeasurements.VolumeArea(bolus.Mesh, bolus.Mesh.TriangleIndices(), bolus.Mesh.GetVertex);
                VolumeText = string.Format("{0:0,0.0} mL", (volumeArea.x / 1000));//BolusUtility.VolumeToText(bolus.Mesh);
            } else VolumeText = "No Mesh loaded";
        }

        #region Commands
        [RelayCommand] public async Task SwitchToSmoothingView() => NavigateTo(new SmoothingViewModel());
        [RelayCommand] public async Task SwitchToImportView() => NavigateTo(new ImportViewModel());
        [RelayCommand] public async Task SwitchToRotationView() => NavigateTo(new RotationViewModel());
        [RelayCommand] public async Task SwitchToAirChannelView() => NavigateTo(new AirChannelViewModel());
        [RelayCommand] public async Task SwitchToMoldView() => NavigateTo(new MoldViewModel());
        [RelayCommand] public async Task SwitchToExportView() => NavigateTo(new ExportViewModel());

        #endregion
    }
}
