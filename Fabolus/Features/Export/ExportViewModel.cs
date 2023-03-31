using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Export {
    public partial class ExportViewModel : ViewModelBase {
        public override string? ViewModelTitle => "export";
        public override MeshViewModelBase? MeshViewModel => new ExportMeshViewModel();
    }
}
