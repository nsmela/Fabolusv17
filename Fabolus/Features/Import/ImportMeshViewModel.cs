using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Import
{
    public partial class ImportMeshViewModel : MeshViewModelBase {
        [ObservableProperty] private bool? _zoomWhenLoaded = true;

        public ImportMeshViewModel() : base() {
        }

        #region Receive Messages

        #endregion
    }
}
