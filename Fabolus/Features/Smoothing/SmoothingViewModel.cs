using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Smoothing.Tools;
using g3;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Fabolus.Features.Smoothing {
    public enum SmoothingValue {
        [Description("Rough")] rough = 0,
        [Description("Standard")] standard = 1,
        [Description("Smoothest")] smoothest = 2
    }

    public static class SmoothEnumExtension {
        public static string ToDescriptionString(this SmoothingValue value) {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])value
                .GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }

    struct PoissonSettings {
        public float Scale;
        public int Degrees, Depth, SamplesPerNode;
    }

    struct MarchingCubesSettings {
        public float EdgeSize, SmoothSpeed;
        public int Iterations, MarchingCubes;
    }

    public partial class SmoothingViewModel : ViewModelBase {
        public override string ViewModelTitle => "smooth";
        public override MeshViewModelBase MeshViewModel => new SmoothingMeshViewModel();

        #region Properties and their Events
        //Smoothing Mode Selection using Radio buttons
        [ObservableProperty] private bool _smoothingPoissonMode, _smoothingMarchingCubesMode;
        partial void OnSmoothingPoissonModeChanged(bool value) {
            if (!value) return;
            
            SmoothingMarchingCubesMode = false;
            OnSmoothSettingChanged(SmoothSetting);
        }
        partial void OnSmoothingMarchingCubesModeChanged(bool value) {
            if (!value) return;
            
            SmoothingPoissonMode = false;
            OnSmoothSettingChanged(SmoothSetting);
        }

        [ObservableProperty] private int _smoothSetting; //rough, standard or smooth
        partial void OnSmoothSettingChanged(int value) {
            //for the label
            SmoothingValue label = (SmoothingValue)value;
            SmoothingLabel = label.ToDescriptionString();

            if (SmoothingMarchingCubesMode) {
                var settings = _marchingCubesSettings[value];
                
                MarchingEdgeSize = settings.EdgeSize;
                MarchingSmoothSpeed = settings.SmoothSpeed;
                MarchingIterations = settings.Iterations;
                MarchingCubesCount = settings.MarchingCubes;
                
                return;
            }

            if(SmoothingPoissonMode) {
                var settings = _poissonSettings[value];

                PoissonDegrees = settings.Degrees;
                PoissonDepth= settings.Depth;
                PoissonScale= settings.Scale;
                PoissonSamplesPerNode = settings.SamplesPerNode;

                return;
            }
        }

        [ObservableProperty] private string _smoothingLabel;

        [ObservableProperty] private int _poissonDegrees, _poissonDepth, _poissonSamplesPerNode;
        [ObservableProperty] private float _poissonScale;
        private List<PoissonSettings> _poissonSettings = new List<PoissonSettings> {
            new PoissonSettings{ Degrees = 1, Depth = 9, Scale= 1.8f, SamplesPerNode=2 }, //rough
            new PoissonSettings{ Degrees = 1, Depth = 9, Scale= 1.8f, SamplesPerNode=1 }, //standard
            new PoissonSettings{ Degrees = 1, Depth = 8, Scale= 1.4f, SamplesPerNode=4 } //smooth
        };

        [ObservableProperty] private int _marchingIterations, _marchingCubesCount;
        [ObservableProperty] private float _marchingEdgeSize, _marchingSmoothSpeed;
        private List<MarchingCubesSettings> _marchingCubesSettings = new List<MarchingCubesSettings> {
            new MarchingCubesSettings{ EdgeSize = 0.2f, SmoothSpeed=0.2f, Iterations=1, MarchingCubes=32 }, //rough
            new MarchingCubesSettings{ EdgeSize = 0.4f, SmoothSpeed=0.2f, Iterations=1, MarchingCubes=64 }, //standard
            new MarchingCubesSettings{ EdgeSize = 0.6f, SmoothSpeed=0.4f, Iterations=2, MarchingCubes=128 } //smooth
        };


        #endregion

        private BolusModel _bolus;
        public SmoothingViewModel() {
            _bolus = new BolusModel();

            SmoothingMarchingCubesMode = true;
            SmoothSetting = 1;

            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m)=> { _bolus = m.bolus; });

            _bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
        }

        private DMesh3 SmoothingByMarchingCubes() {
            var smoothing = new MarchingCubesSmoothing {
                EdgeLength = MarchingEdgeSize,
                SmoothSpeed = MarchingSmoothSpeed,
                Iterations = MarchingIterations,
                Cells = MarchingCubesCount,
                OriginalMesh = _bolus.Mesh
            };

            return smoothing.Smooth();
        }

        private DMesh3 SmoothingByPoisson() {
            var smoothing = new PoissonSmoothing {
                Depth = PoissonDepth,
                Scale = PoissonScale,
                SamplesPerNode = PoissonSamplesPerNode,
                OriginalMesh = _bolus.Mesh
            };

            return smoothing.Smooth();
        }

        #region Commands
        [RelayCommand] 
        public async Task Smooth() {
            if (_bolus.Mesh == null) return; //no bolus to smooth

            ClearSmoothed();//removes the old smoothed mesh

            DMesh3 mesh = new DMesh3();
            if (SmoothingMarchingCubesMode) mesh = await Task.Factory.StartNew(() => SmoothingByMarchingCubes());
            else mesh = await Task.Factory.StartNew(() => SmoothingByPoisson());

            WeakReferenceMessenger.Default.Send(new AddNewBolusMessage(BolusModel.SMOOTHED_BOLUS_LABEL, mesh));
        }

        [RelayCommand]
        public void ClearSmoothed() {
            WeakReferenceMessenger.Default.Send(new RemoveBolusMessage(BolusModel.SMOOTHED_BOLUS_LABEL));
        }


        #endregion

        #region Private Methods

        #endregion

    }
}
