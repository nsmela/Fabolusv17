using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Smoothing.Tools;
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
        private List<PoissonSmoothModel> _poissonSmoothings = new List<PoissonSmoothModel> { 
            new PoissonSmoothModel{ Name = "rough", Depth = 9, Scale = 1.8f, SamplesPerNode = 2 },
            new PoissonSmoothModel{ Name = "standard", Depth = 9, Scale = 1.8f, SamplesPerNode = 1 },
            new PoissonSmoothModel{ Name = "smooth", Depth = 8, Scale = 1.4f, SamplesPerNode = 4 }
        };

        private PoissonSmoothModel Smoothing => _poissonSmoothings[SmoothingIndex];

        [ObservableProperty] private string _smoothingLabel;
        [ObservableProperty] private int _smoothingIndex = -1;
        [ObservableProperty] private int _depth, _samplesPerNode;
        [ObservableProperty] private float _smoothScale;

        partial void OnSmoothingIndexChanged(int value) {
            SmoothingLabel = Smoothing.Name;
            Depth= Smoothing.Depth;
            SamplesPerNode = Smoothing.SamplesPerNode;
            SmoothScale = Smoothing.Scale;
        }

        #endregion

        private BolusModel _bolus;
        public SmoothingViewModel() {
            _bolus = new BolusModel();

            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r,m)=> { 
                _bolus = m.bolus;
                TrianglesLabel = "Mesh Info: " + _bolus.Mesh.TriangleCount;
            });

            _bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            TrianglesLabel = "Mesh Info: " + _bolus.Mesh.TriangleCount;

            SmoothingIndex = 0;
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

        [RelayCommand] public void ClearSmoothed() => WeakReferenceMessenger.Default.Send(new RemoveBolusMessage(BolusModel.SMOOTHED_BOLUS_LABEL));
        [RelayCommand] public void ReduceMesh() => Remesh();


        #endregion

        #region Remeshing
        [ObservableProperty] private string _trianglesLabel;

        private void Remesh() {
            if(_bolus == null || _bolus.Mesh == null) return;

            //remesh the smooth if exists
            var mesh = _bolus.Mesh;
            var reducer = new Reducer(mesh);

            //add constraint to boundries
            reducer.SetExternalConstraints(new MeshConstraints());
            MeshConstraintUtil.FixAllBoundaryEdges(reducer.Constraints, mesh);

            //ensure surface is along original mesh
            var tree = new DMeshAABBTree3(new DMesh3(mesh));
            tree.Build();
            reducer.SetProjectionTarget(new MeshProjectionTarget(tree.Mesh, tree));
            reducer.ProjectionMode = Reducer.TargetProjectionMode.Inline;

            reducer.ReduceToEdgeLength(2.0f);
            var resultMesh = reducer.Mesh;

            WeakReferenceMessenger.Default.Send(new AddNewBolusMessage(BolusModel.REFINED_BOLUS_LABEL, resultMesh));
        }
        #endregion

    }
}
