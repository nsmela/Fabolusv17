using Fabolus.Features.Bolus;
using Fabolus.Features.Helpers;
using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.AirChannel.Channels {
    public class PathChannelShape : ChannelShape {
        public override Type ChannelType => typeof(PathChannel);

        private float _depth, _radius, _height, _upperRadius, _upperHeight;

        private List<Point3D> _path { get; set; }


        public PathChannelShape(List<Point3D> pathAnchors, float depth, float diameter, float height, float upperDiameter, float upperHeight) {
            _path = pathAnchors;
            UpdateSettings(depth, diameter, height, upperDiameter, upperHeight);
        }


        public override ChannelBase GetChannelSettings() => 
            new PathChannel { ChannelDepth = _depth, ChannelDiameter = _radius* 2, UpperDiameter=_upperRadius * 2, UpperHeight = _upperHeight};

        public override void UpdateSettings(ChannelBase channel) {
            if (channel.GetType() != ChannelType) return;

            var pathChannel = channel as PathChannel;
            if (pathChannel == null) return;

            UpdateSettings(pathChannel.ChannelDepth, pathChannel.ChannelDiameter, pathChannel.Height, pathChannel.UpperDiameter, pathChannel.UpperHeight);
        }

        public override DMesh3 OffsetMesh(float offset, float height) => BolusUtility.MeshGeometryToDMesh(GenerateGeometry(offset, height));

        private void UpdateSettings(float depth, float diameter, float height, float upperDiameter, float upperHeight) {
            _depth = depth;
            _radius = diameter / 2;
            _height = height;
            _upperRadius = MathF.Max( upperDiameter / 2, _radius);
            _upperHeight = upperHeight;

            Geometry = GenerateGeometry();
            Mesh = BolusUtility.MeshGeometryToDMesh(Geometry);
        }

        private MeshGeometry3D GenerateGeometry(float offset = 0, float heightOffset = 0) {
            if (_path == null || _path.Count < 2) return new MeshGeometry3D();

            var mesh = new MeshBuilder(false, false);

            BuildPathChannel(ref mesh, _path, offset, heightOffset);
            return mesh.ToMesh();
        }


        private void BuildPathChannel(ref MeshBuilder mesh, List<Point3D> path, float offset, float heightOffset) {
            if (mesh is null) return;
            if (path is null || path.Count < 2) return;

            var radius = _radius + offset;
            var upperRadius = _upperRadius + offset;
            var topHeight = _height - heightOffset;
            var diff = upperRadius - radius;

            //collect all points
            var lowerPoints = GetPathOutlinePoints(path, radius, -_depth);
            var midLowerPoints = ExtrudePoints(lowerPoints, _depth + _upperHeight);
            var upperPoints = GetPathOutlinePoints(path, upperRadius, _upperHeight + diff);
            var topPoints = new List<Point3D>();
            upperPoints.ForEach(p => topPoints.Add(new Point3D(p.X, p.Y, topHeight)));

            //mesh those points
            CapContour(ref mesh, _path, lowerPoints, true);
            JoinPoints(ref mesh, lowerPoints, midLowerPoints);
            JoinPoints(ref mesh, midLowerPoints, upperPoints);
            JoinPoints(ref mesh, upperPoints, topPoints);
            CapContour(ref mesh, _path, topPoints);
        }

        private Vector3D GetDirection(Point3D start, Point3D end) {
            var direction = end - start;
            direction.Z = 0;
            direction.Normalize();

            return direction;
        }

        private List<Point3D> GetPathOutlinePoints(IList<Point3D> path, float radius, float verticalOffset = 0.0f) {
            if (path is null || path.Count < 2) return null;

            var direction = new Vector3D();
            var horizontalOffset = new Vector3D();
            List<Point3D> left = new(), right = new();

            //generate the straight paths on both sides
            for (int i = 0; i < path.Count; i++) {
                if (i + 1 < path.Count) direction = GetDirection(path[i], path[i + 1]);
                horizontalOffset = Vector3D.CrossProduct(direction, new Vector3D(0, 0, 1));
                left.Add(path[i] + horizontalOffset * radius);
                right.Add(path[i] - horizontalOffset * radius);
            }

            //create endcaps
            int last = left.Count - 1;
            var startArcPoints = ArcPoints(left[0], right[0]);
            var endArcPoints = ArcPoints(right[last], left[last]);

            //collect all points
            var points = new List<Point3D>(); //store the points to make up the new mesh
            left.ForEach(p => points.Add(p));
            endArcPoints.ForEach(p => points.Add(p));
            right.Reverse();
            right.ForEach(p => points.Add(p));
            startArcPoints.ForEach(p => points.Add(p));

            if (verticalOffset == 0) return points;

            var vertOffset = new Vector3D(0, 0, verticalOffset);
            var offsetPoints = new List<Point3D>();
            points.ForEach(p => offsetPoints.Add(p + vertOffset));
            return offsetPoints;

        }

        private List<Point3D> ExtrudePoints(List<Point3D> points, float height) {
            if (points is null || points.Count < 2) return null;

            var verticalOffset = new Vector3D(0, 0, height);
            var upperPoints = new List<Point3D>();
            points.ForEach(p => upperPoints.Add(p + verticalOffset));

            return upperPoints;
        }

        private void JoinPoints(ref MeshBuilder mesh, List<Point3D> lowerPoints, List<Point3D> upperPoints) {
            if (lowerPoints is null || lowerPoints.Count < 2) return;
            if (upperPoints is null || upperPoints.Count < 2) return;

            for (int i = 0; i < lowerPoints.Count; i++) {
                var next = (i + 1 < lowerPoints.Count) ? i + 1 : 0;
                var p1 = upperPoints[i];
                var p2 = lowerPoints[i];
                var p3 = lowerPoints[next];
                var p4 = upperPoints[next];
                mesh.AddQuad(p1, p2, p3, p4);
            }
        }

        //calculating triangles via indices instead of adding polygons
        private Int32Collection JoinPoints(List<Point3D> lowerPoints, List<Point3D> upperPoints) {
            if (lowerPoints is null || lowerPoints.Count < 2) return null;
            if (upperPoints is null || upperPoints.Count < 2) return null;

            var indices = new Int32Collection();
            var lowerCount = lowerPoints.Count;
            for (int i = 0; i < lowerPoints.Count; i++) {
                var next = (i + 1 < lowerPoints.Count) ? i + 1 : 0;
                indices.Append(lowerCount + i);
                indices.Append(i);
                indices.Append(next);
                indices.Append(lowerCount + next);
            }
            return indices;
        }

        private void CapContour(ref MeshBuilder mesh, List<Point3D> path, List<Point3D> points, bool reverse = false) {
            var pathCount = path.Count;
            var arcCount = SEGMENTS / 2; 

            int left = 0; //starts at 0
            int endArc = left + pathCount - 1;
            int right = endArc + arcCount + 1;
            int startArc = right + pathCount - 1;

            //taking the points relevant to the component
            var leftPoints = new List<Point3D>();
            for(int i = left; i <= endArc; i++) leftPoints.Add(points[i]);

            var endPoints = new List<Point3D>();
            for (int i = endArc; i <= right; i++) endPoints.Add(points[i]);

            var rightPoints = new List<Point3D>();
            for (int i = right; i <= startArc; i++) rightPoints.Add(points[i]);

            var startPoints = new List<Point3D>();
            for (int i = startArc; i < points.Count; i++) startPoints.Add(points[i]);
            startPoints.Add(points[0]); //first point also included

            //flips the normals to point downward
            if (reverse) {
                endPoints.Reverse();
                startPoints.Reverse();
            }

            mesh.AddPolygon(endPoints);
            mesh.AddPolygon(startPoints);

            for (int i = 0; i < pathCount - 1; i++) {
                var p1 = points[startArc - i];
                var p2 = points[startArc - i - 1];
                var p3 = points[left + i];
                var p4 = points[left + i + 1];

                if (reverse) { //flip the normals
                    mesh.AddTriangle(p4, p3, p1);
                    mesh.AddTriangle(p1, p2, p4);
                } else {
                    mesh.AddTriangle(p1, p3, p4);
                    mesh.AddTriangle(p4, p2, p1);
                }
            }

        }
 
        /// <summary>
        /// Creates a list of points in half-circle arc going clockwise starting with the first point
        /// </summary>
        /// <param name="start">From where the clockwise points start</param>
        /// <param name="end">Where the clockwise arc ends</param>
        /// <returns>List of 3D Points in an arc</returns>
        private List<Point3D> ArcPoints(Point3D start, Point3D end) {
            //https://stackoverflow.com/questions/14096138/find-the-point-on-a-circle-with-given-center-point-radius-and-degree
            //goes clockwise from the starting point to the end point
            
            var axis = end - start;
            var radius = axis.Length / 2;
            axis.Normalize();

            var tangent = Vector3D.CrossProduct(new Vector3D(0,0,1), axis);
            var centre = start + axis * radius;
            var startVector = start - centre;
            var pointVector = startVector;

            var rotate = new AxisAngleRotation3D(new Vector3D(0, 0, 1), -1 * 360 / SEGMENTS); //negative to make it go clockwise
            var rotationMatrix = new RotateTransform3D(rotate, centre);

            var points = new List<Point3D>();
            for(int i = 0; i < SEGMENTS / 2; i++) {
                pointVector = rotationMatrix.Transform(pointVector);
                var point = centre + pointVector;
                points.Add(point);
            }

            points.Reverse(); //why does this amke it work?

            return points;
        }
    }
}
