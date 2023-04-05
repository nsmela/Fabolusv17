using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Mold.Tools;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold {
    public partial class MoldMeshViewModel : MeshViewModelBase {
        [ObservableProperty] private Model3DGroup _moldMesh, __airChannelsMesh, _finalMesh, _testMesh;
        private DiffuseMaterial _moldPreviewSkin, _moldSkin, _channelsSkin;
        private MoldShape _moldShape;

        public MoldMeshViewModel() : base () {
            MoldMesh = new();

            _moldPreviewSkin = SetSkin(Colors.Aqua, 0.4f);
            _moldSkin = SetSkin(Colors.Red, 0.8f);
            _channelsSkin = SetSkin(Colors.Purple);

            WeakReferenceMessenger.Default.Register<MoldShapeUpdatedMessage>(this, (r,m) => { UpdateMold(m.shape); });
            WeakReferenceMessenger.Default.Register<MoldFinalUpdatedMessage>(this, (r,m) => { UpdateFinalMold(m.mesh); });

            AirChannelsMesh = new();
            FinalMesh = new();
            TestMesh = new();
            
            var airChannels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            UpdateAirchannels(airChannels);
            var shape = WeakReferenceMessenger.Default.Send<MoldShapeRequestMessage>();
            UpdateMold(shape);
        }

        #region Receiving Messages
        private void UpdateMold(MoldShape shape) {
            _moldShape = shape;

            MoldMesh.Children.Clear();
            var model = new GeometryModel3D(_moldShape.Geometry, _moldPreviewSkin);
            model.BackMaterial = _moldPreviewSkin;
            MoldMesh.Children.Add(model);

            //testing
            TestMesh.Children.Clear();
            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            var contour = MoldTools.ContourMesh(bolus.Mesh);
            model = new GeometryModel3D(contour, _channelsSkin);
            model.BackMaterial = _channelsSkin;
            TestMesh.Children.Add(model);
        }

        private void UpdateAirchannels(List<AirChannelModel> airchannels) {
            if (airchannels == null) return;
            AirChannelsMesh.Children.Clear();
            if (airchannels.Count > 0) {
                for (int i = 0; i < airchannels.Count; i++) {
                    var model = new GeometryModel3D(airchannels[i].Geometry, _channelsSkin);
                    AirChannelsMesh.Children.Add(model); 
                }
            }
        }

        private void UpdateFinalMold(MeshGeometry3D? mesh) {
            if (mesh == null) {
                FinalMesh.Children.Clear();

                BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
                Update(bolus);

                var airChannels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
                UpdateAirchannels(airChannels);

                var shape = WeakReferenceMessenger.Default.Send<MoldShapeRequestMessage>();
                UpdateMold(shape);

                return;
            }


            //remove all other meshes aside from the final mesh
            DisplayMesh.Children.Clear();
            AirChannelsMesh.Children.Clear();
            MoldMesh.Children.Clear();
            FinalMesh.Children.Clear();
            var model = new GeometryModel3D(mesh, _moldSkin);
            model.BackMaterial = _moldSkin;
            FinalMesh.Children.Add(model);
        }
        #endregion

        #region Private Methods
        private DiffuseMaterial SetSkin(System.Windows.Media.Color colour, float opacity = 1.0f) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity = opacity;
            return new DiffuseMaterial(brush);
        }

            #endregion

        }
}
