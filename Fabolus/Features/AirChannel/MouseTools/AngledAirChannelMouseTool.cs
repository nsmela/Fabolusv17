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

        private AirChannelShape Shape => new AirChannelAngled(_lastMousePosition, _normal, _depth, _diameter, _coneLength, _coneDiameter, _height);

        public override Geometry3D? ToolMesh =>
            _lastMousePosition == null || _lastMousePosition == new Point3D() ?
            null : Shape.Geometry;

        public AngledAirChannelMouseTool() {
            //messages
            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => { ChannelUpdated(m.channel);});

            _height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            ChannelBase channel = WeakReferenceMessenger.Default.Send<AirChannelToolRequestMessage>();
            ChannelUpdated(channel);
        }

        private void ChannelUpdated(ChannelBase channel) {
            if (channel.GetType() != typeof(AngledChannel)) return;
            var angledChannel = channel as AngledChannel;
            _depth = angledChannel.ChannelDepth;
            _diameter = angledChannel.ChannelDiameter;
            _coneLength = angledChannel.ConeLength;
            _coneDiameter = angledChannel.ConeDiameter;
        }

        #region Mouse Events
        public override void MouseDown(MouseEventArgs mouse) {
            _lastMousePosition = new Point3D(); //clears position, a successful hit will recreate it
            if (mouse.RightButton == MouseButtonState.Pressed) return;

            //get all hits
            var hits = GetHits(mouse);
            if (hits == null || hits.Count == 0) {
                //if nothing is clicked on
                WeakReferenceMessenger.Default.Send(new AirChannelSelectedMessage(null));
                return; //nothing was struck
            }
            foreach (var result in hits) {
                string name = result.Model.GetName();
                if (name == null) continue;

                if (name == BOLUS_LABEL) { //if clicked on bolus
                    _lastMousePosition = result.Position;
                    _normal= result.Normal;
                    var shape = Shape;
                    WeakReferenceMessenger.Default.Send(new AddAirChannelShapeMessage(shape));
                    return;
                }

                //if airchannel
                if (name.Contains(AIRCHANNEL_LABEL)) {
                    //number at end is the channel's index
                    var number = Regex.Match(result.Model.GetName(), @"\d+$", RegexOptions.RightToLeft).Value;
                    int index = int.Parse(number);
                    WeakReferenceMessenger.Default.Send(new AirChannelSelectedMessage(index));
                    return;
                }


            }
        }

        public override void MouseMove(MouseEventArgs mouse) {
            //abort if any mouse buttons are down
            if (mouse.LeftButton == MouseButtonState.Pressed) return;
            if (mouse.RightButton == MouseButtonState.Pressed) return;

            //reset values
            _lastMousePosition = new Point3D();
            _normal = new Vector3D();

            //which spot on the bolus is th mouse over, if not on an air channel
            var hits = GetHits(mouse);
            foreach (var hit in hits) {
                if (hit.Model == null || hit.Model.GetName() == null) continue;
                if (hit.Model.GetName().Contains(AIRCHANNEL_LABEL)) return; //abort, do nothing else
                
                if(hit.Model.GetName() == BOLUS_LABEL) {
                    _lastMousePosition = hit.Position;
                    _normal = hit.Normal;
                    return;
                }
            }
        }

        public override void MouseUp(MouseEventArgs mouse) {

        }

        #endregion


    }
}
