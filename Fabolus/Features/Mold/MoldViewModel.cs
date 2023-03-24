using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold {
    public partial class MoldViewModel : ViewModelBase {
        public override string? ViewModelTitle => "mold";
        public override MeshViewModelBase MeshViewModel => new MoldMeshViewModel();

        private MoldStore.MoldSettings _settings;

        public MoldViewModel() {
            _settings = WeakReferenceMessenger.Default.Send<MoldSettingsRequestMessage>();
        }

        #region Commands

        #endregion

    }
}
