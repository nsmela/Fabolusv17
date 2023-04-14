using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Mold.Contours;
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
        [ObservableProperty] private Model3DGroup _moldMesh, __airChannelsMesh, _finalMesh;
        private DiffuseMaterial _moldPreviewSkin, _moldSkin, _channelsSkin;
        private MoldShape _moldShape;

        public MoldMeshViewModel() : base () {
            MoldMesh = new();

            _moldPreviewSkin = SetSkin(Colors.Aqua, 0.4f);
            _moldSkin = SetSkin(Colors.Red, 0.8f);
            _channelsSkin = SetSkin(Colors.Purple);

            WeakReferenceMessenger.Default.Register<MoldContourUpdatedMessage>(this, (r, m) => { UpdateContour(m.contour); });
            WeakReferenceMessenger.Default.Register<MoldFinalUpdatedMessage>(this, (r,m) => { UpdateFinalMold(m.mesh); });

            AirChannelsMesh = new();
            FinalMesh = new();
            
            var airChannels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            UpdateAirchannels(airChannels);
            ContourModelBase contour = WeakReferenceMessenger.Default.Send<MoldContourRequestMessage>();
            UpdateContour(contour);
        }

        #region Receiving Messages
        private void UpdateContour(ContourModelBase contour) {
            var geometry = contour.Contour.Geometry;

            MoldMesh.Children.Clear();
            var model = new GeometryModel3D(geometry, _moldPreviewSkin);
            model.BackMaterial = _moldPreviewSkin;
            MoldMesh.Children.Add(model);
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

                //var shape = WeakReferenceMessenger.Default.Send<MoldShapeRequestMessage>();
                //UpdateMold(shape);

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
