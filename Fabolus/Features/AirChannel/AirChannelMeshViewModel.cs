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
using Fabolus.Features.AirChannel.MouseTools;

namespace Fabolus.Features.AirChannel
{
    public partial class AirChannelMeshViewModel : MeshViewModelBase {
        public const string BOLUS_LABEL = "bolus"; //used to name the bolus model for hit detection
        public const string AIRCHANNEL_LABEL = "airchannel"; //names each airchannel for hit detection

        //controls what the mouse functions are in mesh view
        private AirChannelMouseTool _mouseTool;

        [ObservableProperty] private List<AirChannelModel> _airChannels;
        [ObservableProperty] private double _diameter, _height;
        [ObservableProperty] private Point3D _mouseHit;
        [ObservableProperty] private Model3DGroup _airChannelsMesh, _airChannelToolMesh, _testMesh;
        [ObservableProperty] private bool _showTool, _showMesh;
        [ObservableProperty] private int? _selectedAirChannel = null;
        [ObservableProperty] private Point3D? _pathStart, _pathEnd;
        [ObservableProperty] private List<AirChannelMouseTool> _mouseTools;
        private DiffuseMaterial _toolSkin, _channelsSkin, _selectedSkin;
        private BolusModel _bolus;

        public AirChannelMeshViewModel() : base() {
            //messages
            WeakReferenceMessenger.Default.Register<AirChannelsUpdatedMessage>(this, (r, m) => { Update(m.channels, m.selectedIndex); });
            WeakReferenceMessenger.Default.Register<AirChannelSettingsUpdatedMessage>(this, (r, m) => { Update(m.diameter, m.height, m.selectedIndex); });

            //parse existing info when switching to this new MeshViewModel
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            Update(channels, null);

            double diameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            double height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            int? selectedIndex = null;
            Update(diameter, height, selectedIndex);

        }

        #region Private Methods
        protected override void Update(BolusModel bolus) { 
            DisplayMesh.Children.Clear();

            //building geometry model
            _bolus = bolus;
            var model = _bolus.Model3D;
            model.SetName(BOLUS_LABEL);
            DisplayMesh.Children.Add(model);

            //first time loading viewmodel needs to initialize values
            if (_toolSkin == null) Initialize();

            //generate meshes for air channels to display
            Update(AirChannels, null);

            //ensure mouse tool has 
        }

        private void OnMouseMove() {
            AirChannelToolMesh.Children.Clear();
            if (_mouseTool.ToolMesh == null) return;

            //update the tool mesh in meshview
            var mesh = new GeometryModel3D(_mouseTool.ToolMesh, _toolSkin);
            AirChannelToolMesh.Children.Add(mesh);

            //TODO: instead of making the mesh over, maybe adjust the points instead? or transform the points?
        }

        private void OnMouseDown() {

        }

        //to update the size and position of the air channel tool
        private void Update(Point3D anchor) {
            //generate mesh for air channel tool
            AirChannelToolMesh.Children.Clear();
            if (anchor != new Point3D()) {
                var tool = new AirChannelStraight(anchor, Diameter, Height);
                var mesh = new GeometryModel3D(tool.Geometry, _toolSkin);
                AirChannelToolMesh.Children.Clear();
                AirChannelToolMesh.Children.Add(mesh);
            }
        }

        //to update the list of air channels
        private void Update(List<AirChannelModel> airChannels, int? selectedIndex) {
            if (airChannels == null) return; //no need to update

            AirChannels = airChannels;

            //generates mesh for saved air channels in air channel store
            AirChannelsMesh.Children.Clear();
            if (AirChannels.Count > 0) {
                for(int i = 0; i < AirChannels.Count; i++) {
                    bool isSelected = (selectedIndex != null &&  i == selectedIndex); //color the selected air channel differently
                    var skin = isSelected ? _selectedSkin : _channelsSkin;
                    var model = new GeometryModel3D(AirChannels[i].Geometry, skin);
                    model.SetName(AIRCHANNEL_LABEL + i.ToString()); //adding a unique label for hit detection
                    AirChannelsMesh.Children.Add(model); //TODO: load all at once? has to stay seperate to detect
                }
            }
        }

        //to update the settings
        private void Update(double diameter, double height, int? selectedIndex) {
            //tool needs to be updated
            Diameter = diameter;
            Height = height;
            Update(MouseHit);
            
            //update mouse tool mesh
            OnMouseMove();
        }

        private void Initialize() {
            AirChannelsMesh = new Model3DGroup();
            AirChannelToolMesh = new Model3DGroup();
            ShowTool = false;
            ShowMesh = true;

            MouseHit = new Point3D();
            //mouse tools
            MouseTools = new();
            MouseTools.Add(new VerticalAirChannelMouseTool());
            MouseTools.Add(new AngledAirChannelMouseTool());

            _mouseTool = MouseTools[0];

            //skin colours
            _toolSkin = SetSkin(Colors.MediumPurple, 0.5f);
            _channelsSkin = SetSkin(Colors.Purple, 1.0f);
            _selectedSkin = SetSkin(Colors.BlueViolet, 1.0f);

            //shortest path
            _pathStart = null;
            _pathEnd = null;
            TestMesh = new();
        }

        private DiffuseMaterial SetSkin(Color colour, double opacity) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity= opacity;
            return new DiffuseMaterial(brush);
        }

        private void UpdateAngledPointer(MouseEventArgs e) {
            TestMesh = new Model3DGroup();

            var hit = GetHits(e, BOLUS_LABEL);
            if (hit == null) return;

            Point3D anchor = hit.Position;
            var airchannel = new AirChannelAngled(anchor, hit.Normal, Diameter, Height);
            TestMesh.Children.Add(new GeometryModel3D(airchannel.Geometry, _toolSkin));
        }

        private void UpdateShortestPath(Point3D point) {
            if(point == null){ //clear the results
                _pathStart= null;
                _pathEnd= null;
                return;
            }

            if(_pathStart == null) {
                _pathStart = point;
                return;
            }

            _pathEnd = point;

            //calculate shortest geodist path
            var path = _bolus.GetGeoDist((Point3D)_pathStart, (Point3D)_pathEnd);
            var pathMesh = new AirChannelPath(path, 1.0f);

            TestMesh.Children.Clear();
            var model = new GeometryModel3D(pathMesh.Geometry, _selectedSkin);
            TestMesh.Children.Add(model);
        }

        #endregion

        #region Receive
        #endregion

        #region Commands
        [RelayCommand]
        private void MouseDown(MouseEventArgs e) {
            _mouseTool.MouseDown(e);
            OnMouseDown();
        }

        [RelayCommand]
        private void MouseUp(MouseEventArgs e) {

        }

        [RelayCommand]
        private void MouseMove(MouseEventArgs e) {
            _mouseTool.MouseMove(e);
            OnMouseMove();
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
