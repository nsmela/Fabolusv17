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
    public sealed record BolusUpdatedMessage(string label, BolusModel bolus);
    public sealed record RequestBolusMessage(string label);
    public sealed class DoesBolusExistsMessage: RequestMessage<bool> {}
    public sealed record SendBolusMessage(string label, BolusModel bolus);

    //rotations
    public sealed record ApplyTransformMessage(Vector3D axis, double angle);
    public sealed record ClearTransformsMessage();

    #endregion

    public class BolusStore {
        #region Bolus Types
        public const string ORIGINAL_BOLUS_LABEL = "original"; //imported model
        public const string SMOOTHED_BOLUS_LABEL = "smoothed"; //smoothed model
        public const string DISPLAY_BOLUS_LABEL = "display"; //latest model
        #endregion

        private Dictionary<string, BolusModel> _boli;

        public BolusStore() {
            _boli = new Dictionary<string, BolusModel>();

            //registering received messages
            WeakReferenceMessenger.Default.Register<AddNewBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<RemoveBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<RequestBolusMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<DoesBolusExistsMessage>(this, (r, m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ApplyTransformMessage>(this, (r,m) => { Receive(m); });
            WeakReferenceMessenger.Default.Register<ClearTransformsMessage>(this, (r,m)=> { Receive(m); });
        }

        private void SendBolusUpdate(string label = DISPLAY_BOLUS_LABEL) {
            if (label == DISPLAY_BOLUS_LABEL || label == "") {
                if (_boli.ContainsKey(SMOOTHED_BOLUS_LABEL)) label = SMOOTHED_BOLUS_LABEL;
                else if (_boli.ContainsKey(ORIGINAL_BOLUS_LABEL)) label = ORIGINAL_BOLUS_LABEL;
            }

            BolusModel bolus = new BolusModel();
            if (_boli.ContainsKey(label))
                bolus = _boli[label];

            WeakReferenceMessenger.Default.Send(new BolusUpdatedMessage(label, bolus));
        }

        //messages
        public void Receive(AddNewBolusMessage message) {
            var label = message.label;
            var mesh = message.mesh;

            if (label == ORIGINAL_BOLUS_LABEL)
                _boli.Clear();

            if (_boli.ContainsKey(label)) 
                _boli.Remove(label);
            
            _boli.Add(label, new BolusModel(mesh));
            SendBolusUpdate(label);
        }

        public void Receive(RemoveBolusMessage message) {
            _boli.Remove(message.label);
            SendBolusUpdate(DISPLAY_BOLUS_LABEL);
        }

        public void Receive(ClearBolusMessage message) {
            _boli.Clear();
        }

        public void Receive(RequestBolusMessage message) {
            var label = message.label;
            SendBolusUpdate(label);
        }

        private void Receive(DoesBolusExistsMessage message) {
            var result = _boli != null && _boli.Count> 0;

            message.Reply(result);
        }

        private void Receive(ApplyTransformMessage message) {

        }


        private void Receive(ClearTransformsMessage message) => _transforms = new Transform3DGroup();


     }
}
