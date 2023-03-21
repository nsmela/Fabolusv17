using System;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using HelixToolkit.Wpf;
using MouseTool = Fabolus.Features.AirChannel.MouseTools.AirChannelMouseTool;

namespace Fabolus.Features.AirChannel.MouseTools {
    public class VerticalAirChannelMouseTool : AirChannelMouseTool {
        private string BOLUS_LABEL => AirChannelMeshViewModel.BOLUS_LABEL;
        private double _diameter, _height;
        private Point3D _lastMousePosition;

        public override Geometry3D? ToolMesh =>
            _lastMousePosition == null || _lastMousePosition == new Point3D() ? 
            null : new AirChannelStraight(_lastMousePosition, _diameter, _height).Geometry;

        public VerticalAirChannelMouseTool() { 

            //messages
            WeakReferenceMessenger.Default.Register<AirChannelSettingsUpdatedMessage>(this, (r, m) => {
                _diameter = m.diameter;
                _height = m.height; 
            });

            _diameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            _height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
        }

        public override void MouseDown(MouseEventArgs mouse) {
            if (mouse.RightButton == MouseButtonState.Pressed) return;
            _lastMousePosition = new Point3D(); //clears position, a successful hit will recreate it

            //get all hits
            var hits = GetHits(mouse);
            if (hits == null || hits.Count == 0) return; //nothing was struck
            foreach (var result in hits) {
                if (result.Model.GetName() == BOLUS_LABEL) { //if clicked on bolus
                    var shape = new AirChannelStraight(result.Position, _diameter, _height);
                    WeakReferenceMessenger.Default.Send(new AddAirChannelShapeMessage(shape));
                    return;
                }
            }
        }

        public override void MouseMove(MouseEventArgs mouse) {
            //abort if any mouse buttons are down
            if (mouse.LeftButton == MouseButtonState.Pressed) return;
            if (mouse.RightButton == MouseButtonState.Pressed) return;

            //calculate mouse hit and which model
            _lastMousePosition = MouseTool.GetHitSpot(mouse, BOLUS_LABEL);
        }

        public override void MouseUp(MouseEventArgs mouse) {
            
        }

    }
}
