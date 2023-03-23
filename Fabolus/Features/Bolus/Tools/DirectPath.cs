using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

namespace Fabolus.Features.Bolus {
    public partial class BolusUtility {

        public static List<Point3D> GetDirectPath(DMesh3 mesh, Vector3d startV, Vector3d endV, int startT, int endT){
            //for each point 
            //get the direction of the end point
            //find the edge that will be intersected
            //if no edge, then go directly to the end point and end loop
            //find the point where the point, direction and edge all intersect
            //add a point

            //OR

            //get next triangle
            //create plane from triangle
            //rayhit that plane
            //hit creates point
            //add point to list

            var path = new List<Point3D> {
                new Point3D(startV.x, startV.y, startV.z)
            };

            bool endPointReached = false;
            int tID = startT;
            
            while (!endPointReached) {
                //create triangle plane
                var plane = new Plane3d();
                //rayhit that plane
                //convert hit into point on edge
                //add point to edge
                
                //get next triangle
                //check if next point is end point
            }

            return null;
        }

        private static Vector3d Point3DToVector3d(Point3D p) => new Vector3d(p.X, p.Y, p.Z); 
    }
}
