using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using Fabolus.Features.Common;
using Fabolus.Features.Mold;
using g3;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Export {
    public partial class ExportViewModel : ViewModelBase {
        public override string? ViewModelTitle => "export";
        public override MeshViewModelBase? MeshViewModel => new ExportMeshViewModel();

        #region Commands
        [RelayCommand] private void ExportMesh() {
            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            if (bolus == null) return;

            SaveFileDialog saveFile = new() {
                Filter = "STL Files (*.stl)|*.stl|All Files (*.*)|*.*"
            };

            //if successful, create mesh
            if (saveFile.ShowDialog() != true)
                return;

            var filepath = saveFile.FileName;

            var mesh = new DMesh3();
            MeshGeometry3D? mold = WeakReferenceMessenger.Default.Send<MoldFinalRequestMessage>();
            mesh = BolusUtility.MeshGeometryToDMesh( mold != null ? mold : bolus.Geometry);
            StandardMeshWriter.WriteMesh(filepath, mesh, WriteOptions.Defaults);
        }

        #endregion
    }
}
