using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using HelixToolkit.Wpf;
using g3;

namespace Fabolus.Features.AirChannel
{
    public partial class AirChannelMeshViewModel : MeshViewModelBase {
        private const string BOLUS = "bolus";

        [ObservableProperty] private List<AirChannelModel> _airChannels;
        [ObservableProperty] private double _diameter, _height;
        [ObservableProperty] Point3D _mouseHit;
        [ObservableProperty] private Model3DGroup _airChannelsMesh, _airChannelToolMesh;
        [ObservableProperty] private bool _showTool, _showMesh;

        private DiffuseMaterial _toolSkin, _selectedSkin, _channelsSkin; 

        public AirChannelMeshViewModel() : base() {
            //messages
            WeakReferenceMessenger.Default.Register<AirChannelsUpdatedMessage>(this, (r, m) => { Update(m.channels); });
            WeakReferenceMessenger.Default.Register<AirChannelSettingsUpdatedMessage>(this, (r, m) => { Update(m.diameter, m.height); });

            //parse existing info when switching to this new MeshViewModel
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            Update(channels);

            double diameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            double height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            Update(diameter, height);
        }

        #region Private Methods
        protected override void Update(BolusModel bolus) { 
            DisplayMesh.Children.Clear();

            //building geometry model
            var model = bolus.Model3D;
            model.SetName(BOLUS);
            DisplayMesh.Children.Add(model);

            //first time loading viewmodel needs to initialize values
            if (_toolSkin == null) Initialize();

            //generate meshes for air channels to display
            Update(AirChannels);
        }

        //to update the size and position of the air channel tool
        private void Update(Point3D anchor) {
            //generate mesh for air channel tool
            AirChannelToolMesh.Children.Clear();
            if (anchor != new Point3D()) {
                var tool = new AirChannelModel(anchor, Diameter, Height - anchor.Z);
                var mesh = new GeometryModel3D(tool.Geometry, _toolSkin);
                AirChannelToolMesh.Children.Clear();
                AirChannelToolMesh.Children.Add(mesh);
            }
        }

        //to update the list of air channels
        private void Update(List<AirChannelModel> airChannels) {
            if (airChannels == null || airChannels.Count == AirChannelsMesh.Children.Count()) return; //no need to update
            AirChannels = airChannels;

            //generates mesh for saved air channels in air channel store
            AirChannelsMesh.Children.Clear();
            if (AirChannels.Count > 0) {
                foreach (var a in AirChannels) {
                    AirChannelsMesh.Children.Add(new GeometryModel3D(a.Geometry, _channelsSkin)); //TODO: load all at once? has to stay seperate to detect
                }
            }
        }

        //to update the settings
        private void Update(double diameter, double height) {
            //tool needs to be updated
            Diameter = diameter;
            Height = height;
            Update(MouseHit);
        }

        private void Initialize() {
            AirChannelsMesh = new Model3DGroup();
            AirChannelToolMesh = new Model3DGroup();
            ShowTool = false;
            ShowMesh = true;

            MouseHit = new Point3D();

            //skin colours
            _toolSkin = SetSkin(Colors.MediumPurple, 0.5f);
            _selectedSkin = SetSkin(Colors.Purple, 0.4f);
            _channelsSkin = SetSkin(Colors.Purple, 1.0f);
        }

        private DiffuseMaterial SetSkin(Color colour, double opacity) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity= opacity;
            return new DiffuseMaterial(brush);
        }

        
        #endregion

        #region Receive
        #endregion

        #region Commands
        [RelayCommand]
        private void MouseDown(MouseEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) return;
            MouseHit = new Point3D(); //clears position, a successful hit will recreate it

            //test if bolus mesh is hit
            var mousePosition = e.GetPosition((IInputElement)e.Source);
            var viewport = ((HelixViewport3D)e.Source).Viewport;

            var hits = Viewport3DHelper.FindHits(viewport, mousePosition);
            if (hits == null || hits.Count == 0) return;

            foreach (var hit in hits) {
                if (hit.Model == null) continue;
                if (hit.Model.GetName() != BOLUS) continue;

                WeakReferenceMessenger.Default.Send(new AddAirChannelMessage(hit.Position));
                return;
            }
        }

        [RelayCommand]
        private void MouseUp(MouseEventArgs e) {

        }

        [RelayCommand]
        private void MouseMove(MouseEventArgs e) {
            //abort if any mouse buttons are down
            if (e.LeftButton == MouseButtonState.Pressed) return;
            if (e.RightButton == MouseButtonState.Pressed) return;

            MouseHit = GetHitSpot(e, BOLUS);
            Update(MouseHit);

            //check if an air channel is mouse over
        }
        #endregion

        #region Ray Hit Tests
        private IList<Viewport3DHelper.HitResult> GetHits(MouseEventArgs e) {
            //test if bolus mesh is hit
            var mousePosition = e.GetPosition((IInputElement)e.Source);
            var viewport = ((HelixViewport3D)e.Source).Viewport;

            return Viewport3DHelper.FindHits(viewport, mousePosition);
        }

        private Viewport3DHelper.HitResult GetHits(MouseEventArgs e, string filterLabel) {
            var hits = GetHits(e);

            if (hits == null) return null; //nothing found

            foreach (var hit in hits) {
                if (hit.Model == null) continue;
                if (hit.Model.GetName() == filterLabel) return hit;
            }

            return null;//nothing found
        }

        private Point3D GetHitSpot(MouseEventArgs e, string filterLabel) {
            var hit = GetHits(e, filterLabel);
            if (hit == null) return new Point3D();
            else return hit.Position;
        }

        #endregion

    }

}
