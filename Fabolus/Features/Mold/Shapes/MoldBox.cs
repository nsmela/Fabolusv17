using Fabolus.Features.Bolus;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold {
    public class MoldBox : MoldShape {
        public override string Name => "mold box";

        public MoldBox(MoldStore.MoldSettings settings, BolusModel bolus = null) {
            Bolus = bolus;
            Settings = settings;

            //should this subscribe to the bolusupdated and moldsettings update messages? No, models are meant to be data storage object. Let MoldStore update the current mold shape
        }

        public override void ToMesh() {
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;

            //calculate
            var mesh = new MeshBuilder();
            mesh.AddSphere(new Point3D(0, 0, 0), Settings.OffsetXY * 10.0f);
            Geometry = mesh.ToMesh();
        }

    }
}
