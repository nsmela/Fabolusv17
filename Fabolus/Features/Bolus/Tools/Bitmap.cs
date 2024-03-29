﻿using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Bolus {
    public static partial class BolusUtility {
        public static Bitmap3 MeshBitmap(DMesh3 mesh, int numcells) {
            //create voxel mesh
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, autoBuild: true);
            AxisAlignedBox3d bounds = mesh.CachedBounds;

            double cellsize = bounds.MaxDim / numcells;
            ShiftGridIndexer3 indexer = new ShiftGridIndexer3(bounds.Min, cellsize);

            Bitmap3 bmp = new Bitmap3(new Vector3i(numcells, numcells, numcells));
            //foreach (Vector3i idx in bmp.Indices()) {
            //   Vector3d v = indexer.FromGrid(idx);
            //    bmp.Set(idx, spatial.IsInside(v));
            //}
            Parallel.ForEach(bmp.Indices(), idx => {
                Vector3d v = indexer.FromGrid(idx);
                bmp.Set(idx, spatial.IsInside(v));
            });
            return bmp;
        }

        public static Bitmap3 MeshBitmap(DMesh3 mesh, float cellsize) {
            //create voxel mesh
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, autoBuild: true);
            AxisAlignedBox3d bounds = mesh.CachedBounds;
            int cells = (int)(bounds.MaxDim / cellsize);
            int cellsX = (int)(bounds.Width / cellsize);
            int cellsY = (int)(bounds.Depth/ cellsize);
            int cellsZ = (int)(bounds.Height / cellsize);

            ShiftGridIndexer3 indexer = new ShiftGridIndexer3(bounds.Min, cellsize);

            Bitmap3 bmp = new Bitmap3(new Vector3i(cells, cells, cells));
            foreach (Vector3i idx in bmp.Indices()) {
                Vector3d v = indexer.FromGrid(idx);
                bmp.Set(idx, spatial.IsInside(v));
            }
            return bmp;
        }
    }
}
