using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Tools;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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

            Geometry = MoldTools.OffsetMesh(Bolus.TransformedMesh, Settings.OffsetXY);
            return;
            //testing
            _bottomPoints = BottomContour();

            var mesh = new MeshBuilder();

            if (_bottomPoints == null) return;
            var topPoints = TopContour(_bottomPoints);
            mesh.AddPolygon(_bottomPoints);
            mesh.AddPolygon(topPoints);

            //stitch the two contours together
            for (int i = 0; i < _bottomPoints.Count; i++) {
                int index = (i + 1 >= _bottomPoints.Count) ? 0 : i + 1; //to link up final face
                var face = new List<Point3D>{
                    _bottomPoints[i],
                    _bottomPoints[index],
                    topPoints[index],
                    topPoints[i]
                };
                mesh.AddPolygon(face);
            }

            Geometry = mesh.ToMesh();
        }

        public override void UpdateMesh() {
            if (Bolus == null || Bolus.Mesh == null || Bolus.Mesh.VertexCount == 0) return;


        }

        #region Fields related to the box contour
        private List<Point3D> _bottomPoints;

        #endregion


        private List<Point3D> BottomContour() {
            var points = MoldTools.TraceOutline(Bolus.Geometry, new Vector3D(0, 0, -1));
            points.Reverse(); //changes direction of the normal
            var result = new List<Point3D>();

            points.ForEach(p => {
                result.Add(new Point3D(p.X, p.Y, p.Z - Settings.OffsetBottom));
            });


            return result;
        }

        private List<Point3D> TopContour(List<Point3D> contour) {
            List<Point3D> points = new();

            var zHeight = Bolus.Geometry.Bounds.SizeZ + Settings.OffsetTop + Bolus.Geometry.Bounds.Location.Z;
            foreach ( var p in contour) points.Add( new Point3D(p.X, p.Y, zHeight));

            return points;
        }

    }
}
