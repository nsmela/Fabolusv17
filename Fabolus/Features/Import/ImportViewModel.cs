using CommunityToolkit.Mvvm.Input;
using Fabolus.Features.Common;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using g3;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;

namespace Fabolus.Features.Import {
    public partial class ImportViewModel : ViewModelBase {
        public override string ViewModelTitle => "import";
        public override MeshViewModelBase MeshViewModel => new ImportMeshViewModel();

        //commands
        [RelayCommand]
        public async Task ImportFile() {
            //clear the bolus
            WeakReferenceMessenger.Default.Send(new ClearBolusMessage());

            //open file dialog box
            OpenFileDialog openFile = new() {
                Filter = "STL Files (*.stl)|*.stl|All Files (*.*)|*.*",
                Multiselect = false
            };

            //if successful, create mesh
            if (openFile.ShowDialog() == false) return;

            var filepath = openFile.FileName;
            if (!File.Exists(filepath)) {
                System.Windows.MessageBox.Show("Unable to find: " + filepath);
                return;
            }

            var mesh = new DMesh3(await Task.Factory.StartNew(() => StandardMeshReader.ReadMesh(filepath)), false, true);

            //if mesh isn't good
            if (mesh == null) {
                System.Windows.MessageBox.Show( filepath + " was an invalid mesh!");
                return;
            }

            WeakReferenceMessenger.Default.Send(new AddNewBolusMessage(BolusModel.ORIGINAL_BOLUS_LABEL, mesh));
        }

        
    }
}
