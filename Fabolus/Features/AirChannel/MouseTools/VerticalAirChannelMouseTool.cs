﻿using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.AirChannel.Channels;
using HelixToolkit.Wpf;

namespace Fabolus.Features.AirChannel.MouseTools {
    public class VerticalAirChannelMouseTool : AirChannelMouseTool {
        private float _depth, _diameter, _height;
        private Point3D? _lastMousePosition;

        public override Geometry3D? ToolMesh =>
            _lastMousePosition == null || _lastMousePosition == new Point3D() ? 
            null : new VerticalChannelShape((Point3D)_lastMousePosition, _depth, _diameter, _height).Geometry;

        public VerticalAirChannelMouseTool() {
            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => { ChannelUpdated(m.channel); });

            _height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            ChannelBase channel = WeakReferenceMessenger.Default.Send<AirChannelToolRequestMessage>();
            ChannelUpdated(channel);
        }
        private void ChannelUpdated(ChannelBase channel) {
            if (channel.GetType() != typeof(VerticalChannel)) return;
            var verticalChannel = channel as VerticalChannel;
            if (verticalChannel is null) return;

            _depth = verticalChannel.ChannelDepth;
            _diameter = verticalChannel.ChannelDiameter;
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
                    Point3D point = (_lastMousePosition != null) ? (Point3D)_lastMousePosition : new Point3D();
                    var shape = new VerticalChannelShape(point, _depth, _diameter, _height);
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
            _lastMousePosition = new Point3D(); //resets the value if nothing is hit

            //which spot on the bolus is the mouse over, if not on an air channel
            //the first one hit is the important one
            var hits = GetHits(mouse);
            foreach (var h in hits) {
                if (h.Model == null || h.Model.GetName() == null) continue;
                if (h.Model.GetName().Contains(AIRCHANNEL_LABEL)) return;
                
                if (h.Model.GetName() == BOLUS_LABEL) {
                    _lastMousePosition = h.Position;
                    return;
                }
            }
        }

        public override void MouseUp(MouseEventArgs mouse) {
            
        }
        #endregion

    }
}
