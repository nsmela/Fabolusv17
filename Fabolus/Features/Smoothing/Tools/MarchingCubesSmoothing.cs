using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Smoothing.Tools
{
    public class MarchingCubesSmoothing
    {
        public DMesh3 OriginalMesh { get; set; }
        public float EdgeLength { get; set; }
        public float SmoothSpeed { get; set; }
        public float Iterations { get; set; }
        public float Cells { get; set; }

        public MarchingCubesSmoothing()
        {

        }


        public DMesh3 Smooth()
        {
            //Use the Remesher class to do a basic remeshing
            DMesh3 mesh = new DMesh3(OriginalMesh);
            Remesher r = new Remesher(mesh);
            r.PreventNormalFlips = true;
            r.SetTargetEdgeLength(EdgeLength);
            r.SmoothSpeedT = SmoothSpeed;
            r.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));
            for (int k = 0; k < Iterations; k++)
                r.BasicRemeshPass();

            //marching cubes
            int num_cells = (int)Cells;
            DMesh3 smoothMesh = new();
            if (Cells > 0)
            {
                double cell_size = mesh.CachedBounds.MaxDim / num_cells;

                MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(mesh, cell_size);
                sdf.Compute();

                var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);

                MarchingCubes c = new MarchingCubes();
                c.Implicit = iso;
                c.Bounds = mesh.CachedBounds;
                c.CubeSize = c.Bounds.MaxDim / Cells;
                c.Bounds.Expand(3 * c.CubeSize);

                c.Generate();

                smoothMesh = c.Mesh;
            }

            if (smoothMesh == null)
                return null;

            return smoothMesh;
        }
    }
}
