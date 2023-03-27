﻿using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Bolus {
    public static partial class BolusUtility {
        public static DMesh3 OrientationCentre(DMesh3 mesh) {
            double x = mesh.CachedBounds.Center.x * -1;
            double y = mesh.CachedBounds.Center.y * -1;
            double z = mesh.CachedBounds.Center.z * -1;
            MeshTransforms.Translate(mesh, x, y, z);
            return mesh;
        }
    }
}
