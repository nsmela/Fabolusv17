using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using g3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Fabolus.Features.Smoothing {

    public partial class SmoothingViewModel : ViewModelBase {
        public override string ViewModelTitle => "smooth";
        public override MeshViewModelBase MeshViewModel => new SmoothingMeshViewModel();

        #region Properties and their Events

        //list of smoothing settings to use with the slider
        private List<PoissonSmoothModel> _poissonSmoothings = new List<PoissonSmoothModel> { 
            new PoissonSmoothModel{ Name = "rough", Depth = 9, Scale = 1.8f, SamplesPerNode = 2, EdgeLength = 1.0f },
            new PoissonSmoothModel{ Name = "standard", Depth = 9, Scale = 1.8f, SamplesPerNode = 1, EdgeLength = 0.6f },
            new PoissonSmoothModel{ Name = "smooth", Depth = 8, Scale = 1.4f, SamplesPerNode = 4, EdgeLength = 0.4f }
        };

        private PoissonSmoothModel Smoothing => _poissonSmoothings[SmoothingIndex];

        [ObservableProperty] private string _smoothingLabel;
        [ObservableProperty] private int _smoothingIndex;
        [ObservableProperty] private int _depth, _samplesPerNode;
        [ObservableProperty] private float _smoothScale, _edgeLength;
        [ObservableProperty] private bool _advancedMode = false;

        partial void OnSmoothingIndexChanged(int value) {
            SmoothingLabel = Smoothing.Name;
            Depth= Smoothing.Depth;
            SamplesPerNode = Smoothing.SamplesPerNode;
            SmoothScale = Smoothing.Scale;
            EdgeLength = Smoothing.EdgeLength;
        }

        #endregion

        private BolusModel _bolus;
        public SmoothingViewModel() {
            _bolus = new BolusModel();

            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m)=> { 
                _bolus = m.bolus;
            });

            _bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();

            SmoothingIndex = 1; //starts at standard
            Smoothing.Initialize(_bolus.Mesh);

        }

        #region Commands
        [RelayCommand] 
        public async Task Smooth() {
            if (_bolus.Mesh == null) return; //no bolus to smooth

            ClearSmoothed();//removes the old smoothed mesh

            DMesh3 mesh = await Task.Factory.StartNew(() => Smoothing.ToMesh());

            WeakReferenceMessenger.Default.Send(new AddNewBolusMessage(BolusModel.SMOOTHED_BOLUS_LABEL, mesh));
        }

        [RelayCommand] private void ClearSmoothed() => WeakReferenceMessenger.Default.Send(new RemoveBolusMessage(BolusModel.SMOOTHED_BOLUS_LABEL));
        [RelayCommand] private void ToggleAdvancedMode() => AdvancedMode = !_advancedMode; 

        #endregion

    }
}
