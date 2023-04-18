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
using Fabolus.Features.AirChannel.MouseTools;
using Fabolus.Features.AirChannel.Channels;
using System.Threading.Channels;

namespace Fabolus.Features.AirChannel
{
    public sealed record SetAirChannelTool(int? toolIndex);
    public partial class AirChannelMeshViewModel : MeshViewModelBase {
        public const string BOLUS_LABEL = "bolus"; //used to name the bolus model for hit detection
        public const string AIRCHANNEL_LABEL = "airchannel"; //names each airchannel for hit detection

        [ObservableProperty] private List<AirChannelModel> _airChannels;
        [ObservableProperty] private float _diameter, _height;
        [ObservableProperty] private Point3D _mouseHit;
        [ObservableProperty] private Model3DGroup _airChannelsMesh, _airChannelToolMesh;
        [ObservableProperty] private bool _showTool, _showMesh;
        [ObservableProperty] private int? _selectedAirChannel = null;

        private DiffuseMaterial _toolSkin, _channelsSkin, _selectedSkin, _overhangsSkin;
        private BolusModel _bolus;

        //controls what the mouse functions are in mesh view
        private AirChannelMouseTool _mouseTool;
        [ObservableProperty] private List<AirChannelMouseTool> _mouseTools;

        public AirChannelMeshViewModel() : base() {
            //messages
            WeakReferenceMessenger.Default.Register<AirChannelsUpdatedMessage>(this, (r, m) => { Update(m.channels, m.selectedIndex); });
            WeakReferenceMessenger.Default.Register<SetAirChannelTool>(this, (r, m) => { _mouseTool = MouseTools[(int)m.toolIndex]; });
            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => { ChannelChanged(m.channel); });

            //parse existing info when switching to this new MeshViewModel
            List<AirChannelModel> channels = WeakReferenceMessenger.Default.Send<AirChannelsRequestMessage>();
            Update(channels, null);

            //parsing required info
            double height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            int? selectedIndex = null;
        }

        #region Receiving Messeges
        private void ChannelChanged(ChannelBase channel) {

            if (_mouseTool != null && channel.MouseToolType == _mouseTool.GetType()) {
                OnMouseMove();
                return;
            }
            var mouseTool = Activator.CreateInstance(channel.MouseToolType);

            _mouseTool = (AirChannelMouseTool)mouseTool;
            OnMouseMove();//updates the tool if needed
        }

        #endregion

        #region Private Methods
        private void Initialize() {
            AirChannelsMesh = new Model3DGroup();
            AirChannelToolMesh = new Model3DGroup();
            ShowTool = false;
            ShowMesh = true;

            MouseHit = new Point3D();

            //mouse tools
            ChannelBase channel = WeakReferenceMessenger.Default.Send<AirChannelToolRequestMessage>();
            ChannelChanged(channel);

            //skin colours
            _toolSkin = SetSkin(Colors.MediumPurple, 0.5f);
            _channelsSkin = SetSkin(Colors.Purple, 1.0f);
            _selectedSkin = SetSkin(Colors.Cyan, 1.0f);
            _overhangsSkin = OverhangSettings.OVERHANG_SKIN;

            Update(_bolus);
        }

        //when the bolus store sends an update
        protected override void Update(BolusModel bolus) { 
            DisplayMesh.Children.Clear();
            if (bolus.TransformedMesh == null || bolus.TransformedMesh.TriangleCount == 0) return;
            //building geometry model
            _bolus = bolus;
            var model = GetTempOverhangs();
            model.SetName(BOLUS_LABEL);
            DisplayMesh.Children.Add(model);

            //first time loading viewmodel needs to initialize values
            if (_toolSkin == null) Initialize();

            //generate meshes for air channels to display
            Update(AirChannels, null);
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

        private DiffuseMaterial SetSkin(Color colour, double opacity) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity= opacity;
            return new DiffuseMaterial(brush);
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
            if (mesh == null) return new PointCollection();

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
