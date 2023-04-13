using Fabolus.Features.Bolus;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Contours {

    /// <summary>
    /// Generates the Geometry for a mold
    /// </summary>
    public abstract class ContourBase {
        public virtual string Name { get; }
        public virtual MeshGeometry3D Geometry { get; protected set; }
        public virtual DMesh3 Mesh { get; protected set; }
        public abstract void Calculate();
    }
}
