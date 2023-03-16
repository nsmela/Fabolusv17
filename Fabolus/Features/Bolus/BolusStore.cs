using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Bolus {

    #region Messages
    //importing file
    public sealed record AddNewBolusMessage(string label, DMesh3 mesh);
    public sealed record RemoveBolusMessage(string label);
    public sealed record ClearBolusMessage();

    //utility
    public sealed record BolusUpdatedMessage(BolusModel bolus);
    public sealed record RequestBolusMessage();

    //rotations
    public sealed record ApplyTransformMessage(Vector3D axis, double angle);
    public sealed record ClearTransformsMessage();

    #endregion

    public class BolusStore {
        private BolusModel _bolus;

        public BolusStore() {
            _bolus = new BolusModel();

            //registering received messages
            WeakReferenceMessenger.Default.Register<AddNewBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<RemoveBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<RequestBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ApplyTransformMessage>(this, (r,m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearTransformsMessage>(this, (r,m)=> { Receive(m); });
        }

        private void SendBolusUpdate() => WeakReferenceMessenger.Default.Send(new BolusUpdatedMessage(_bolus));

        //messages
        public void Receive(AddNewBolusMessage message) {
            var label = message.label;
            var mesh = message.mesh;

            _bolus.AddMesh(label, mesh);

            SendBolusUpdate();
        }

        public void Receive(RemoveBolusMessage message) {
            _bolus.RemoveMesh(message.label);
            SendBolusUpdate();
        }

        public void Receive(ClearBolusMessage message) {
            _bolus = new BolusModel();
            SendBolusUpdate();
        }

        public void Receive(RequestBolusMessage message) => SendBolusUpdate();

        private void Receive(ApplyTransformMessage message) {
            var axis = new Vector3d {
                x = message.axis.X,
                y = message.axis.Y,
                z = message.axis.Z
            };
            var angle = message.angle;

            _bolus.AddTransform(axis, angle);
            SendBolusUpdate();
        }


        private void Receive(ClearTransformsMessage message) {
            _bolus.ClearTransforms();
            SendBolusUpdate();
        }


     }
}
