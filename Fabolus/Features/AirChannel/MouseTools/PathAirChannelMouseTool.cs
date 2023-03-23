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
    public sealed record AddPathAirChannelMessage();

    internal class PathAirChannelMouseTool : AirChannelMouseTool {
        private string BOLUS_LABEL => AirChannelMeshViewModel.BOLUS_LABEL;
        private string AIRCHANNEL_LABEL => AirChannelMeshViewModel.AIRCHANNEL_LABEL;

        private double _diameter, _height;
        private List<Point3D> _pathPoints;
        private List<Point3D>? _path;
        private List<int> _pathTriangles; //triangles closest to the path point. used to set up pat finding
        private Point3D? _lastMousePosition;
        private BolusModel _bolus;
        private MeshGeometry3D _mesh;

        public override Geometry3D? ToolMesh { 
            get {
                var mesh = new MeshBuilder();
                if(_mesh != null) mesh.Append(_mesh);
                if (_lastMousePosition != null) mesh.AddSphere((Point3D)_lastMousePosition, _diameter / 2);
                return mesh.ToMesh();
            } 
        }

        public PathAirChannelMouseTool() {
            _pathPoints = new();
            _pathTriangles = new();

            //messages
            WeakReferenceMessenger.Default.Register<AirChannelSettingsUpdatedMessage>(this, (r, m) => {
                _diameter = m.diameter;
                _height = m.height;
            });

            WeakReferenceMessenger.Default.Register<ClearAirChannelsMessage>(this, (r, m) => {
                //if nothing is clicked on
                _pathPoints.Clear();
                _pathTriangles.Clear();
                _mesh = ToMesh();
            });

            WeakReferenceMessenger.Default.Register<AddPathAirChannelMessage>(this, (r, m) => { AddAirChannelPath(); });

            _diameter = WeakReferenceMessenger.Default.Send<AirChannelDiameterRequestMessage>();
            _height = WeakReferenceMessenger.Default.Send<AirChannelHeightRequestMessage>();
            
        }

        #region Mouse Commands
        public override void MouseDown(MouseEventArgs mouse) {
            if (mouse.RightButton == MouseButtonState.Pressed) return;
            _bolus = (BolusModel)WeakReferenceMessenger.Default.Send<BolusRequestMessage>();

            //get all hits
            var hits = GetHits(mouse);
            if (hits == null || hits.Count == 0) {
                //if nothing is clicked on
                _pathPoints.Clear();
                _pathTriangles.Clear();
                _mesh = ToMesh();
                WeakReferenceMessenger.Default.Send(new AirChannelSetMessage(null));
                return; //nothing was struck
            }

            foreach (var result in hits) {
                string name = result.Model.GetName();
                if (name == null) continue;

                if (name == BOLUS_LABEL) { //if clicked on bolus
                    //if(_pathPoints.Count > 1) {
                    //    _pathPoints.RemoveAt(1);
                     //   _pathTriangles.RemoveAt(1);
                    //}

                    _pathPoints.Add(result.Position);

                    //var t1 = result.RayHit.VertexIndex1;
                    //var t2 = result.RayHit.VertexIndex2;
                    //var t3 = result.RayHit.VertexIndex3;
                    //var triangleIndex = _bolus.TransformedMesh.FindTriangle(t1, t2, t3);
                    //_pathTriangles.Add(triangleIndex);

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

            /*
            if(_pathPoints.Count > 1 ) {
                var path = GetPath();
                if (path != null && path.Count > 0) {
                    for(int i = 1; i < path.Count; i++) {
                        mesh.AddCylinder(path[i - 1], path[i], 0.5f, 8);
                    }
                } 
            }
            */
            return mesh.ToMesh();

        }

        private List<Point3D> GetPath() {
            if (_pathPoints.Count <= 1) return new List<Point3D>();

            var paths = new List<Point3D>();

            for(int i = 1; i < _pathPoints.Count; i++) {
                Point3D start = _pathPoints[i-1];
                Point3D end = _pathPoints[i];
                int startTri = _pathTriangles[i - 1];
                int endTri = _pathTriangles[i];

                var path = _bolus.GetGeoDist(start, startTri, end, endTri);
                if (path == null) continue;

                path.Add(start);
                path.ForEach(paths.Add);
                path.Add(end);
            }

            return paths;
        }

        private void AddAirChannelPath() {
            //create shape
            var mesh = new AirChannelPath(_pathPoints, _diameter, _height);

            //clear relevant variables
            _pathPoints.Clear();
            _mesh = ToMesh();

            //add it to air channel store
            WeakReferenceMessenger.Default.Send(new AddAirChannelShapeMessage(mesh));

        }

        #endregion


    }
}

