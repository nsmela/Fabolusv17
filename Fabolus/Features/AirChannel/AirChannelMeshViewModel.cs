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
        [ObservableProperty] private List<AirChannelModel> _airChannels;
        [ObservableProperty] private double _diameter, _height;
        [ObservableProperty] Point3D _mouseHit;
        [ObservableProperty] private Model3DGroup _airChannelsMesh, _airChannelToolMesh;
        [ObservableProperty] private bool _showTool, _showMesh;

        private DiffuseMaterial _toolSkin, _selectedSkin, _channelsSkin; 

        public AirChannelMeshViewModel() {
            DisplayMesh = new Model3DGroup();
            AirChannelsMesh= new Model3DGroup();
            AirChannelToolMesh= new Model3DGroup();
            ShowTool = false;
            ShowMesh = true;

            MouseHit = new Point3D();

            //skin colours
            _toolSkin = SetSkin(Colors.DarkBlue, 1.0f);
            _selectedSkin = SetSkin(Colors.MediumPurple, 0.5f);
            _channelsSkin = SetSkin(Colors.Purple, 0.4f);

            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<AirChannelsUpdatedMessage>(this, (r, m) => { Receive(m); });

            //updated display mesh if one existed before switching to this view
            WeakReferenceMessenger.Default.Send<RequestBolusMessage>(new RequestBolusMessage());
            WeakReferenceMessenger.Default.Send<RequestAirChannelsMessage>(new RequestAirChannelsMessage());
        }

        #region Private Methods
        private void UpdateAirChannelTool() { //TODO: optimize to only handle things that have changed. This triggers on any change
            //generate meshes for air channels to display

            UpdateAirchannelTool();
            
            //generates mesh for saved air channels in air channel store
            AirChannelsMesh.Children.Clear();
            if (_airChannels.Count > 0) {
                foreach(var a in _airChannels) 
                    AirChannelsMesh.Children.Add(new GeometryModel3D(a.Geometry, _channelsSkin)); //TODO: load all at once? has to stay seperate to detect
            }

            //look -272, 263, -301 up -0.448, 0.432, 0.783 pos 272, -263, 301 target 0,0,0
        }

        private void UpdateAirchannelTool() {
            //generate mesh for air channel tool
            AirChannelToolMesh.Children.Clear();
            if (MouseHit != new Point3D()) {
                var tool = new AirChannelModel(MouseHit, Diameter, Height - MouseHit.Z);
                var mesh = new GeometryModel3D(tool.Geometry, _toolSkin);
                AirChannelToolMesh.Children.Clear();
                AirChannelToolMesh.Children.Add(mesh);
            }
        }

        private DiffuseMaterial SetSkin(Color colour, double opacity) {
            var brush = new SolidColorBrush(colour);
            brush.Opacity= opacity;
            return new DiffuseMaterial(brush);
        }

        
        #endregion

        #region Receive
        private void Receive(BolusUpdatedMessage message) {
            var bolus = message.bolus;
            DisplayMesh.Children.Clear();

            //building geometry model
            var model = bolus.Model3D;
            model.SetName("bolus");
            DisplayMesh.Children.Add(model);
        }

        private void Receive(AirChannelsUpdatedMessage message) {
            _airChannels = message.channels;
            _diameter = message.diameter;
            _height = message.height;

            UpdateAirChannelTool();
        }
        #endregion

        #region Commands
        [RelayCommand]
        private void MouseDown(MouseEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) return;
            MouseHit = new Point3D(); //clears position, a successful hit will recreate it

            //test if bolus mesh is hit
            var mousePosition = e.GetPosition((IInputElement)e.Source);
            var hitParams = new RayHitTestParameters(
                new Point3D(mousePosition.X, mousePosition.Y, 0),
                new Vector3D(mousePosition.X, mousePosition.Y, 10)
                );

            var viewport = ((HelixViewport3D)e.Source).Viewport;

            var hits = Viewport3DHelper.FindHits(viewport, mousePosition);
            if (hits == null || hits.Count == 0) return;

            foreach (var hit in hits) {
                if (hit.Model == null) continue;
                if (hit.Model.GetName() != "bolus") continue;

                WeakReferenceMessenger.Default.Send(new AddAirChannelMessage(hit.Position));
                UpdateAirChannelTool();
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

            MouseHit = new Point3D(); //clears position, a successful hit will recreate it
            
            //test if bolus mesh is hit
            var mousePosition = e.GetPosition((IInputElement)e.Source);
            var hitParams = new RayHitTestParameters(
                new Point3D(mousePosition.X, mousePosition.Y, 0), 
                new Vector3D(mousePosition.X, mousePosition.Y, 10)
                );

            var viewport = ((HelixViewport3D)e.Source).Viewport;
            
            var hits = Viewport3DHelper.FindHits(viewport, mousePosition);
            if (hits == null || hits.Count == 0) { //mouse isn't over the bolus mesh
                UpdateAirChannelTool();
                return;
            }

                foreach (var hit in hits) {
                if(hit.Model == null ) continue;
                if (hit.Model.GetName() != "bolus") continue;

                MouseHit = hit.Position;
                UpdateAirChannelTool();
                return;
            }

            //check if an air channel is mouse over
        }
        #endregion

        #region Ray Hit Tests
        private RayMeshGeometry3DHitTestResult _meshHit;

        private HitTestResultBehavior MeshHitsCallback(HitTestResult result) {
            RayHitTestResult rayResult = result as RayHitTestResult;
            if (rayResult == null) return HitTestResultBehavior.Continue;

            RayMeshGeometry3DHitTestResult meshResult = rayResult as RayMeshGeometry3DHitTestResult;
            if(meshResult == null) return HitTestResultBehavior.Continue;
            
            var label = meshResult.ModelHit.GetName();
            if(label != "bolus") return HitTestResultBehavior.Continue;

            //if fits all creiteria
            MouseHit = meshResult.PointHit;
            return HitTestResultBehavior.Stop;
            
        }
        #endregion

    }

    #region MouseBehavior
    //for mouse interactions
    public class MouseBehaviour {
        #region Mouse Up
        public static readonly DependencyProperty MouseUpCommandProperty =
        DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(new PropertyChangedCallback(MouseUpCommandChanged)));

        private static void MouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseUp += new MouseButtonEventHandler(element_MouseUp);
        }

        static void element_MouseUp(object sender, MouseButtonEventArgs e) {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseUpCommand(element);

            command.Execute(e);
        }

        public static void SetMouseUpCommand(UIElement element, ICommand value) {
            element.SetValue(MouseUpCommandProperty, value);
        }

        public static ICommand GetMouseUpCommand(UIElement element) {
            return (ICommand)element.GetValue(MouseUpCommandProperty);
        }
        #endregion

        #region Mouse Down
        public static readonly DependencyProperty MouseDownCommandProperty =
        DependencyProperty.RegisterAttached("MouseDownCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(new PropertyChangedCallback(MouseDownCommandChanged)));

        private static void MouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseDown += new MouseButtonEventHandler(element_MouseDown);
        }

        static void element_MouseDown(object sender, MouseButtonEventArgs e) {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseDownCommand(element);

            command.Execute(e);
        }

        public static void SetMouseDownCommand(UIElement element, ICommand value) {
            element.SetValue(MouseDownCommandProperty, value);
        }

        public static ICommand GetMouseDownCommand(UIElement element) {
            return (ICommand)element.GetValue(MouseDownCommandProperty);
        }
        #endregion

        #region Mouse Move
        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.RegisterAttached("MouseMoveCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(new PropertyChangedCallback(MouseMoveCommandChanged)));

        private static void MouseMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseMove += new MouseEventHandler(element_MouseMove);    //MouseButtonEventHandler(element_MouseMove);
        }

        static void element_MouseMove(object sender, MouseEventArgs e) {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseMoveCommand(element);

            command.Execute(e);
        }
        public static void SetMouseMoveCommand(UIElement element, ICommand value) {
            element.SetValue(MouseMoveCommandProperty, value);
        }

        public static ICommand GetMouseMoveCommand(UIElement element) {
            return (ICommand)element.GetValue(MouseMoveCommandProperty);
        }
        #endregion
    }
    #endregion
}
