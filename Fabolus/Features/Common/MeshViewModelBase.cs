using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Common
{
    [ObservableObject]
    public partial class MeshViewModelBase {
        //mesh to store visible bolus model
        [ObservableProperty] protected Model3DGroup _displayMesh;

        //Camera controls
        [ObservableProperty] protected PerspectiveCamera? _camera;
        [ObservableProperty] protected bool? _zoomWhenLoaded = false;

        public MeshViewModelBase(bool? zoom = false) {
            DisplayMesh = new Model3DGroup();
            ZoomWhenLoaded= zoom;

            //messages
            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Update(m.bolus); });

            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            Update(bolus);
        }

        public void OnClose() {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        #region Private Methods
        protected virtual void Update(BolusModel bolus) {
            if (bolus.Model3D == null) return;

            DisplayMesh.Children.Clear();

            //building geometry model
            DisplayMesh.Children.Add(bolus.Model3D);
        }
        #endregion  
    }


    public class BindingProxy : Freezable {
        protected override Freezable CreateInstanceCore() {
            return new BindingProxy();
        }

        public object Data {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
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

            element.MouseMove += new MouseEventHandler(element_MouseMove);
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
