using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.Messaging;
using HelixToolkit.Wpf;
using Fabolus.Features.Common;
using Fabolus.Features.Bolus;

namespace Fabolus.Features.AirChannel.MouseTools {
    internal class PathAirChannelMouseTool : AirChannelMouseTool {
        private string BOLUS_LABEL => AirChannelMeshViewModel.BOLUS_LABEL;
        private string AIRCHANNEL_LABEL => AirChannelMeshViewModel.AIRCHANNEL_LABEL;
        private double _diameter, _height;
        private List<Point3D> _pathPoints;

        public override Geometry3D? ToolMesh => null; //TODO
            //_lastMousePosition == null || _lastMousePosition == new Point3D() ?
           // null : null; //TODO

        public PathAirChannelMouseTool() {
            _pathPoints = new();
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

            //get all hits
            var hits = GetHits(mouse);
            if (hits == null || hits.Count == 0) {
                //if nothing is clicked on
                _pathPoints.Clear();
                WeakReferenceMessenger.Default.Send(new AirChannelSetMessage(null));
                return; //nothing was struck
            }
            foreach (var result in hits) {
                string name = result.Model.GetName();
                if (name == null) continue;

                if (name == BOLUS_LABEL) { //if clicked on bolus
                    var t1 = result.RayHit.VertexIndex1;
                    var t2 = result.RayHit.VertexIndex2;
                    var t3 = result.RayHit.VertexIndex3;
                    var bolus = WeakReferenceMessenger.Default.Send<BolusRequestMessage>();


                    _lastMousePosition = result.Position;
                    var shape = new AirChannelStraight(_lastMousePosition, _diameter, _height);
                    WeakReferenceMessenger.Default.Send(new AddAirChannelShapeMessage(shape));
                    return;
                }

                //if airchannel
                if (name.Contains(AIRCHANNEL_LABEL)) {
                    //number at end is the channel's index
                    var number = Regex.Match(result.Model.GetName(), @"\d+$", RegexOptions.RightToLeft).Value;
                    int index = int.Parse(number);
                    WeakReferenceMessenger.Default.Send(new AirChannelSetMessage(index));
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

