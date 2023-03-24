using Fabolus.Features.Bolus;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold
{
    public abstract class MoldShape
    {
        public abstract string Name { get; }
        public MoldStore.MoldSettings? Settings { get; protected set; }
        public BolusModel Bolus { get; protected set; }
        public Geometry3D Geometry { get; protected set; }
        public DMesh3 Mesh { get; protected set; }

        public abstract void ToMesh();

        public void SetSettings(MoldStore.MoldSettings settings) {
            Settings = settings;
            ToMesh();
        }

        public void SetBolus(BolusModel bolus) {
            Bolus = bolus;
            ToMesh();
        }
    }

}
