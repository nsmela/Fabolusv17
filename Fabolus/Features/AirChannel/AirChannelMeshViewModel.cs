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
using Fabolus.Features.Rotation;

namespace Fabolus.Features.AirChannel
{
    public sealed record SetAirChannelTool(int? toolIndex);
    public partial class AirChannelMeshViewModel : MeshViewModelBase {
        public const string BOLUS_LABEL = "bolus"; //used to name the bolus model for hit detection
        public const string AIRCHANNEL_LABEL = "airchannel"; //names each airchannel for hit detection

        [ObservableProperty] private List<AirChannelModel> _airChannels;
        [ObservableProperty] private double _diameter, _height;
        [ObservableProperty] private Point3D _mouseHit;
        [ObservableProperty] private Model3DGroup _airChannelsMesh, _airChannelToolMesh, _testMesh;
        [ObservableProperty] private bool _showTool, _showMesh;
        [ObservableProperty] private int? _selectedAirChannel = null;
        [ObservableProperty] private Point3D? _pathStart, _pathEnd;

        private DiffuseMaterial _toolSkin, _channelsSkin, _selectedSkin, _overhangsSkin;
        private BolusModel _bolus;

        //controls what the mouse functions are in mesh view
        private AirChannelMouseTool _mouseTool;
        [ObservableProperty] private List<AirChannelMouseTool> _mouseTools;

        public AirChannelMeshViewModel() : base() {
            //messages
            WeakReferenceMessenger.Default.Register<AirChannelsUpdatedMessage>(this, (r, m) => { Update(m.channels, m.selectedIndex); });
            WeakReferenceMessenger.Default.Register<AirChannelSettingsUpdatedMessage>(this, (r, m) => { Update(m.diameter, m.height, m.selectedIndex); });
            WeakReferenceMessenger.Default.Register<SetAirChannelTool>(this, (r, m) => { _mouseTool = MouseTools[(int)m.toolIndex]; });

            //parse existing info when switching to this new MeshViewModel
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            Update(channels, null);

            //parsing required info
            double diameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            double height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            int? selectedIndex = null;
            Update(diameter, height, selectedIndex);

        }

        #region Private Methods
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
            MouseTools.Add(new PathAirChannelMouseTool());

            _mouseTool = MouseTools[0];

            //skin colours
            _toolSkin = SetSkin(Colors.MediumPurple, 0.5f);
            _channelsSkin = SetSkin(Colors.Purple, 1.0f);
            _selectedSkin = SetSkin(Colors.BlueViolet, 1.0f);

            var gradients = new Dictionary<Color, float>();
            gradients.Add(RotationMeshViewModel.OVERHANG_COLOR_GOOD, RotationMeshViewModel.OVERHANG_GOOD);
            gradients.Add(RotationMeshViewModel.OVERHANG_COLOR_WARNING, RotationMeshViewModel.OVERHANG_WARNING);
            gradients.Add(RotationMeshViewModel.OVERHANG_COLOR_FAULT, RotationMeshViewModel.OVERHANG_FAULT);
            SetOverhangSkin(gradients);
            Update(_bolus);
            //shortest path
            _pathStart = null;
            _pathEnd = null;
            TestMesh = new();
        }

        //when the bolus store sends an update
        protected override void Update(BolusModel bolus) { 
            DisplayMesh.Children.Clear();

            //building geometry model
            _bolus = bolus;
            var model = GetTempOverhangs();
            model.SetName(BOLUS_LABEL);
            DisplayMesh.Children.Add(model);

            //first time loading viewmodel needs to initialize values
            if (_toolSkin == null) Initialize();

            //generate meshes for air channels to display
            Update(AirChannels, null);

            //ensure mouse tool has 
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
            
            //update mouse tool mesh
            OnMouseMove();
        }

        private DiffuseMaterial SetSkin(Color colour, double opacity) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity= opacity;
            return new DiffuseMaterial(brush);
        }

        private void SetOverhangSkin(Dictionary<Color, float> gradients) {
            var gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new System.Windows.Point(0, 0);
            gradientBrush.EndPoint = new System.Windows.Point(0, 1);

            foreach(var g in gradients) {
                gradientBrush.GradientStops.Add(new GradientStop {
                    Color = g.Key,
                    Offset = g.Value
                });
            }

            _overhangsSkin = new DiffuseMaterial(gradientBrush);
        }

        private GeometryModel3D GetTempOverhangs() {
            //Overhangs are displayed using a gradient brush and vertex texture coordinates
            //The angle of the vertex's normal vs the reference angle determines how far along the gradient 

            //apply temp rotation to reference axis
            var refAngle = new Vector3D(0, 0, -1);

            //using the transformed referance angle, generate texture coordinates to use with the gradient brush
            var texture = GetTextureCoords(_bolus.Geometry, refAngle);
            _bolus.Geometry.TextureCoordinates = texture;

            //get temp model
            GeometryModel3D geometryModel = new GeometryModel3D(_bolus.Geometry, _overhangsSkin);
            geometryModel.BackMaterial = _overhangsSkin;
            return geometryModel;
        }

        private PointCollection GetTextureCoords(MeshGeometry3D mesh, Vector3D refAxis) {
            var refAngle = 180.0f;
            var normals = mesh.Normals;

            PointCollection textureCoords = new PointCollection();
            foreach (var normal in normals) {
                double difference = Math.Abs(Vector3D.AngleBetween(normal, refAxis));

                while (difference > refAngle) difference -= refAngle;

                var ratio = difference / refAngle;

                textureCoords.Add(new System.Windows.Point(0, ratio));
            }

            return textureCoords;
        }


        private void OnMouseMove() {
            AirChannelToolMesh.Children.Clear();
            if (_mouseTool.ToolMesh == null) return;

            //update the tool mesh in meshview
            var mesh = new GeometryModel3D(_mouseTool.ToolMesh, _toolSkin);
            AirChannelToolMesh.Children.Add(mesh);

            //TODO: instead of making the mesh over, maybe adjust the points instead? or transform the points?
        }

        //occurs after the mouse tool has processed mouse down
        private void OnMouseDown() {

        }

        private void OnMouseUp() {

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
            _mouseTool.MouseUp(e);
            OnMouseUp();
        }

        [RelayCommand]
        private void MouseMove(MouseEventArgs e) {
            _mouseTool.MouseMove(e);
            OnMouseMove();
        }
        #endregion

    }

}
