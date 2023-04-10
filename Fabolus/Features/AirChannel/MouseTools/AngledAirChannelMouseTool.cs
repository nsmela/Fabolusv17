using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.Channels;
using Fabolus.Features.Common;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.MouseTools {
    internal class AngledAirChannelMouseTool : AirChannelMouseTool {
        private string BOLUS_LABEL => AirChannelMeshViewModel.BOLUS_LABEL;
        private string AIRCHANNEL_LABEL => AirChannelMeshViewModel.AIRCHANNEL_LABEL;

        private double _depth, _diameter, _height, _coneLength, _coneDiameter;
        private Point3D _lastMousePosition;
        private Vector3D _normal;

        public override Geometry3D? ToolMesh =>
            _lastMousePosition == null || _lastMousePosition == new Point3D() ?
            null : new AirChannelAngled(_lastMousePosition, _normal, _diameter, _height).Geometry;

        public AngledAirChannelMouseTool() {

            //messages
            WeakReferenceMessenger.Default.Register<AirChannelSettingsUpdatedMessage>(this, (r, m) => {
                _diameter = m.diameter;
                _height = m.height;
            });

            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => {
                if (m.channel.GetType() != typeof(AngledChannel)) return;
                var angledChannel = m.channel as AngledChannel;
                _depth = angledChannel.ChannelDepth;
                _diameter = angledChannel.ChannelDiameter;
                _coneLength = angledChannel.ConeLength;
                _coneDiameter = angledChannel.ConeDiameter;
            });

            _diameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            _height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
        }

        public override void MouseDown(MouseEventArgs mouse) {
            if (mouse.RightButton == MouseButtonState.Pressed) return;
            _lastMousePosition = new Point3D(); //clears position, a successful hit will recreate it

            //get all hits
            var hits = GetHits(mouse);
            if (hits == null || hits.Count == 0) {
                //if nothing is clicked on
                WeakReferenceMessenger.Default.Send(new AirChannelSetMessage(null));
                return; //nothing was struck
            }
            foreach (var result in hits) {
                string name = result.Model.GetName();
                if (name == null) continue;

                if (name == BOLUS_LABEL) { //if clicked on bolus
                    _lastMousePosition = result.Position;
                    _normal= result.Normal;
                    var shape = new AirChannelAngled(_lastMousePosition, _normal, _diameter, _height);
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
            var hit = GetHits(mouse, BOLUS_LABEL);
            _lastMousePosition = (hit == null) ? new Point3D() : hit.Position;
            _normal = (hit == null) ? new Vector3D() : hit.Normal;
        }

        public override void MouseUp(MouseEventArgs mouse) {

        }

    }
}
