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
        [ObservableProperty] private bool _infoVisible, _meshLoaded;
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
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Task.FromResult(BolusUpdated(m.bolus)); });

            InfoVisible = false;
            MeshLoaded = false;
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

        private async Task BolusUpdated(BolusModel bolus) {
            if(bolus is null) {
                MeshLoaded = false;
                return;
            }
            
            await UpdateMeshInfo(bolus);
            //await Update Imported Model

            MeshLoaded = true;
        }

        private Task UpdateMeshInfo(BolusModel bolus) {
            string filepath = WeakReferenceMessenger.Default.Send<BolusFilePathRequestMessage>();
            FilePath = string.Empty;
            InfoVisible = false;

            if (!string.IsNullOrEmpty(filepath)) {
                var fileInfo = new FileInfo(filepath);
                FileSize = string.Format("{0:0,0.00} KB", (fileInfo.Length / 1024));
                FilePath = fileInfo.Name;
                InfoVisible = true;
            }
            else {
                FileSize = "N/A";
            }

            if (bolus.Mesh is not null)
                TriangleCount = string.Format("{0:0,0}", bolus.Mesh.TriangleCount);
            else
                TriangleCount = "N/A";

            //volumes
            if (bolus.Mesh != null) {
                VolumeText = string.Empty;

                //original import
                if (bolus.HasMesh(BolusModel.ORIGINAL_BOLUS_LABEL)) 
                    VolumeText += $"Raw: {GetVolume(bolus.GetMesh(BolusModel.ORIGINAL_BOLUS_LABEL))}";

                //smoothed bolus
                if (bolus.HasMesh(BolusModel.SMOOTHED_BOLUS_LABEL)) 
                    VolumeText += $"\r\nSmoothed: {GetVolume(bolus.GetMesh(BolusModel.SMOOTHED_BOLUS_LABEL))} ";

            }
            else {
                VolumeText = "No Mesh loaded";
            }

            return Task.CompletedTask;
        }

        private string GetVolume(DMesh3 mesh) {
            var volumeArea = MeshMeasurements.VolumeArea(
                mesh, 
                mesh.TriangleIndices(), 
                mesh.GetVertex
            );

            return string.Format("{0:0,0.0} mL", (volumeArea.x / 1000));
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
