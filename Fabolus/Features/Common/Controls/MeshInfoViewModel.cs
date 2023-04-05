using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Common.Controls {
    public partial class MeshInfoViewModel : ViewModelBase {
        public override MeshViewModelBase? MeshViewModel => null;

        [ObservableProperty] private string _filePath, _fileSize, _triangleCount;

        public MeshInfoViewModel() {
            FilePath = string.Empty;
            FileSize = string.Empty;
            TriangleCount = string.Empty;

            //messaging
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { Update(m.bolus); });

            BolusModel bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();
            Update(bolus);
        }

        private void Update(BolusModel bolus) {
            string filepath = WeakReferenceMessenger.Default.Send<BolusFilePathRequestMessage>();
            FilePath = string.Empty;

            if (filepath != null && filepath != string.Empty) {
                var fileInfo = new FileInfo(filepath);
                FileSize = (fileInfo.Length / 1000).ToString("0.00") + " KB";
                FilePath = fileInfo.Name;
            } else FileSize = "N/A";

            if (bolus.Mesh != null) TriangleCount = bolus.Mesh.TriangleCount.ToString("N0");
            else TriangleCount = "N/A";
        }
    }
}
