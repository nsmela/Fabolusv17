using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Common
{
    [ObservableObject]
    public partial class MeshViewModelBase {
        //mesh to store visible bolus model
        [ObservableProperty] private Model3DGroup _displayMesh;

        //Camera controls
        [ObservableProperty] private PerspectiveCamera? _camera;
        [ObservableProperty] private bool? _zoomsWhenLoaded = false;

        //public camera position
        public virtual void OnOpen() { }
        public virtual void OnClose() { }

        public MeshViewModelBase(bool? zoom = false) {
            ZoomsWhenLoaded= zoom;
        }
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
}
