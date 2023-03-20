
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Fabolus.Features.Common {
    public class ViewModelBase : ObservableObject {
        public virtual string? ViewModelTitle { get; set; }
        public virtual MeshViewModelBase? MeshViewModel { get; set; }

        //mouse controls display
        public virtual string? LeftMouseLabel => "N/A";
        public virtual string? RightMouseLabel => "Rotate View";
        public virtual string? CentreMouseLabel => "Move/Zoom View";

        //public Methods
        public void OnOpen() { }
        public void OnClose() {
            MeshViewModel.OnClose();
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
