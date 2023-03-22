
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Common {
    public abstract class MouseTool {
        public abstract Geometry3D? ToolMesh { get; } //Mesh visual just for the mouse tool in the mesh view
        public abstract void MouseDown(MouseEventArgs mouse);
        public abstract void MouseUp(MouseEventArgs mouse);
        public abstract void MouseMove(MouseEventArgs mouse);

        protected static IList<Viewport3DHelper.HitResult> GetHits(MouseEventArgs e) {
            //test if bolus mesh is hit
            var mousePosition = e.GetPosition((IInputElement)e.Source);
            var viewport = ((HelixViewport3D)e.Source).Viewport;

            return Viewport3DHelper.FindHits(viewport, mousePosition);
        }

        protected static  Viewport3DHelper.HitResult GetHits(MouseEventArgs e, string filterLabel) {
            var hits = GetHits(e);

            if (hits == null) return null; //nothing found

            foreach (var hit in hits) {
                if (hit.Model == null) continue;
                if (hit.Model.GetName() == filterLabel) return hit;
            }

            return null;//nothing found
        }

        protected static Point3D? GetHitSpot(MouseEventArgs e, string filterLabel) {
            var hit = GetHits(e, filterLabel);
            if (hit == null) return null;
            else return hit.Position;
        }
    }
}
