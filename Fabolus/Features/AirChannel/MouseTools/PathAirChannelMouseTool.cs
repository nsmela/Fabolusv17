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
using Fabolus.Features.AirChannel.Channels;
using CommunityToolkit.Mvvm.Input;

namespace Fabolus.Features.AirChannel.MouseTools {
    public sealed record AddPathAirChannelMessage();

    internal class PathAirChannelMouseTool : AirChannelMouseTool {
        private string BOLUS_LABEL => AirChannelMeshViewModel.BOLUS_LABEL;
        private string AIRCHANNEL_LABEL => AirChannelMeshViewModel.AIRCHANNEL_LABEL;

        private double _depth, _diameter, _height, _upperDiameter, _upperHeight;
        private List<Point3D> _pathPoints;
        private Point3D? _lastMousePosition;
        private MeshGeometry3D _mesh;

        public override Geometry3D? ToolMesh { 
            get {
                if (_lastMousePosition == null || _lastMousePosition == new Point3D()) return _mesh;
                var point = new Point3D(
                    ((Point3D)_lastMousePosition).X,
                    ((Point3D)_lastMousePosition).Y,
                    ((Point3D)_lastMousePosition).Z - _depth
                );

                var mesh = new MeshBuilder();
                mesh.AddSphere(point, _diameter / 2);
                if(_mesh != null) mesh.Append(_mesh);
                return mesh.ToMesh();
            } 
        }

        public PathAirChannelMouseTool() {
            _pathPoints = new();

            //messages
            WeakReferenceMessenger.Default.Register<ChannelUpdatedMessage>(this, (r, m) => { ChannelUpdated(m.channel); });
            WeakReferenceMessenger.Default.Register<AddPathAirChannelMessage>(this, (r, m) => { AddAirChannelPath(); });
            WeakReferenceMessenger.Default.Register<ClearAirChannelsMessage>(this, (r, m) => {
                _pathPoints.Clear();
                _mesh = ToMesh();
            });

            _height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            ChannelBase channel = WeakReferenceMessenger.Default.Send<AirChannelToolRequestMessage>();
            ChannelUpdated(channel);
        }

        #region Receiving Messages
        private void ChannelUpdated(ChannelBase channel) {
            if (channel.GetType() != typeof(PathChannel)) return;

            var pathChannel = (PathChannel)channel;
            _pathPoints = pathChannel.PathPoints;
            _depth = pathChannel.ChannelDepth;
            _diameter = pathChannel.ChannelDiameter;
            _upperDiameter = pathChannel.UpperDiameter;
            _upperHeight = pathChannel.UpperHeight;
            _mesh = ToMesh();
        }

        private void AddAirChannelPath() {
            //create shape
            var path = new List<Point3D>();
            _pathPoints.ForEach(p => path.Add(new Point3D(p.X, p.Y, p.Z - _depth))); //for testing bottom render
            var mesh = new AirChannelPath(path, _depth, _diameter, _height, _upperDiameter, _upperHeight);

            //clear relevant variables
            _pathPoints.Clear();
            _mesh = ToMesh();

            //add it to air channel store
            WeakReferenceMessenger.Default.Send(new AddAirChannelShapeMessage(mesh));

        }
        #endregion

        #region Mouse Commands
        public override void MouseDown(MouseEventArgs mouse) {
            if (mouse.RightButton == MouseButtonState.Pressed) return;

            //get all hits
            var hits = GetHits(mouse);
            if (hits == null || hits.Count == 0) {
                //if nothing is clicked on
                _pathPoints.Clear();
                _mesh = ToMesh();
                WeakReferenceMessenger.Default.Send(new AirChannelSelectedMessage(null));
                return; //nothing was struck
            }

            foreach (var result in hits) {
                string name = result.Model.GetName();
                if (name == null) continue;

                if (name == BOLUS_LABEL) { //if clicked on bolus
                    _pathPoints.Add(result.Position);
                    _lastMousePosition = null; //hides tool until moved

                    _mesh = ToMesh();
                    return;
                }

                //if airchannel
                if (name.Contains(AIRCHANNEL_LABEL)) {
                    //number at end is the channel's index
                    _lastMousePosition= null;
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

        #region Private Methods
        private MeshGeometry3D? ToMesh() {
            if (_pathPoints == null ) return null;
            var mesh = new MeshBuilder();

            foreach(var p in _pathPoints ) {
                mesh.AddSphere(p, _diameter / 2);
            }

            if (_pathPoints.Count > 1) {
                for(int i = 0; i< _pathPoints.Count - 1; i++) {
                    mesh.AddCylinder(_pathPoints[i], _pathPoints[i + 1], 0.5f);
                }
            }
            return mesh.ToMesh();

        }

        #endregion


    }
}

