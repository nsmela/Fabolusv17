using CommunityToolkit.Mvvm.Messaging;
using System;
using g3;
using System.Numerics;

namespace Fabolus.Features.Rotation
{
    #region Messages
    public sealed record ClearTransformsMessage();
    public sealed record RequestUpdatedTransformsMessage();
    public sealed record AddRotationMessage(Vector3 axis, float angle);
    public sealed record AddTempRotationMessage(Vector3 axis, float angle);
    public sealed record UpdatedTransformsMessage(double x, double y, double z, double w);
    #endregion

    public class TransformsStore
    {
        private Quaterniond _rotation;

        private Quaterniond _tempRotation;

        public TransformsStore() {
            _rotation = new Quaterniond();
            _tempRotation = new Quaterniond();

            //messages
            WeakReferenceMessenger.Default.Register<ClearTransformsMessage>(this, (r,m)=> { Receive(m); });
            WeakReferenceMessenger.Default.Register<RequestUpdatedTransformsMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<AddRotationMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<AddTempRotationMessage>(this, (r, m) => { Receive(m); });
        }

        private void SendUpdatedTransform() {
            var rotation =  _rotation + _tempRotation;
            rotation.Normalize();

            WeakReferenceMessenger.Default.Send(new UpdatedTransformsMessage(rotation.x, rotation.y, rotation.z, rotation.w));
        }

        #region Receive
        private void Receive(ClearTransformsMessage message) {
            _rotation = new Quaterniond();
            _tempRotation = new Quaterniond();

            SendUpdatedTransform();
        }

        private void Receive(RequestUpdatedTransformsMessage message) => SendUpdatedTransform();

        private void Receive(AddRotationMessage message) {
            //adding a rotation to save means the temp rotation is converted into a permenant rotation
            //clear the temp rotation, add the new one
            var axis = new Vector3d(message.axis.X, message.axis.Y, message.axis.Z);
            _rotation += new Quaterniond(axis, message.angle);
            _rotation.Normalize();

            _tempRotation = new Quaterniond();

            SendUpdatedTransform();
        }

        private void Receive(AddTempRotationMessage message) {
            var axis = new Vector3d(message.axis.X, message.axis.Y, message.axis.Z);
            _tempRotation = new Quaterniond(axis, message.angle);

            SendUpdatedTransform();
        }
        #endregion

        #region Private Methods

        //not sure if this is working right
        private static Vector3f ToEulerDegrees(Quaterniond q) {
            Vector3f angles = new();

            // roll / x
            double sinr_cosp = 2 * (q.w * q.x + q.y * q.z);
            double cosr_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
            angles.x = (float)Math.Atan2(sinr_cosp, cosr_cosp) * (float)MathUtil.Rad2Deg;

            // pitch / y
            double sinp = 2 * (q.w * q.y - q.z * q.x);
            if (Math.Abs(sinp) >= 1) {
                angles.y = (float)Math.CopySign(Math.PI / 2, sinp) * (float)MathUtil.Rad2Deg;
            } else {
                angles.y = (float)Math.Asin(sinp)* (float)MathUtil.Rad2Deg;
            }

            // yaw / z
            double siny_cosp = 2 * (q.w * q.z + q.x * q.y);
            double cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
            angles.z = (float)Math.Atan2(siny_cosp, cosy_cosp) * (float)MathUtil.Rad2Deg;

            return angles;
        }


        #endregion
    }
}
