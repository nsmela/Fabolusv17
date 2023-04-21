using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Helpers { 
    public static class Extensions {
        public static Vector3d ToVector3d(this Vector3D vector) => new Vector3d(vector.X, vector.Y, vector.Z);
        public static Vector3d ToVector3d(this Point3D point) => new Vector3d(point.X, point.Y, point.Z);
    }
}
