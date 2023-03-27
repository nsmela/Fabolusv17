using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Tools {
    public static partial class MoldTools {
        public static DMesh3 MarchingCubesSmoothing(DMesh3 mesh, int numcells) {
            double cell_size = mesh.CachedBounds.MaxDim / (numcells);

            MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(mesh, cell_size);
            sdf.Compute();

            var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);

            MarchingCubes c = new MarchingCubes();
            c.Implicit = iso;
            c.Bounds = mesh.CachedBounds;
            c.CubeSize = c.Bounds.MaxDim / 32;
            c.Bounds.Expand(3 * c.CubeSize);

            c.Generate();
            return c.Mesh;
        }
    }
}
