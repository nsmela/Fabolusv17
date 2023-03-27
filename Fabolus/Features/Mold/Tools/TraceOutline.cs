using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold.Tools {
    public static partial class MoldTools {

        public static List<Point3D> TraceOutline(MeshGeometry3D mesh, Vector3D refAxis) {
            //start point

            //centre point

            //start from left

            //next id

            //break count?

            //get list of edges

            //arrange list left to right, top to bottom

            //out points

            //while loop
            //increment
            //if length is 0, skip?
            //using vector a, check vector b for the next nearest clockwise
            //if next vector equals start id, break



            //until implemented correctly, will only return the bounding box
            var boundry = mesh.Bounds;

            List<Point3D> points = new();
            var bottomOrigin = new Point3D(boundry.X, boundry.Y,boundry.Z);
            points.Add(bottomOrigin);
            points.Add(new Point3D(bottomOrigin.X, bottomOrigin.Y + boundry.SizeY, bottomOrigin.Z));
            points.Add(new Point3D(bottomOrigin.X + boundry.SizeX, bottomOrigin.Y + boundry.SizeY, bottomOrigin.Z));
            points.Add(new Point3D(bottomOrigin.X + boundry.SizeX, bottomOrigin.Y, bottomOrigin.Z));

            return points;
        }


    }
}
