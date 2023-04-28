using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Common;
using g3;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Bolus {

    #region Messages
    //importing file
    public sealed record AddNewBolusMessage(string label, DMesh3 mesh, string filepath = "");
    public sealed record RemoveBolusMessage(string label);
    public sealed record ClearBolusMessage();

    //utility
    public sealed record BolusUpdatedMessage(BolusModel bolus);

    //rotations
    public sealed record ApplyRotationMessage(Vector3 axis, double angle);
    public sealed record ClearRotationsMessage();
    public sealed record ApplyOverhangSettingsMessage(float lower, float upper);
    public sealed record BolusOverhangsUpdated(DiffuseMaterial material);

    //request messages
    public class BolusRequestMessage : RequestMessage<BolusModel> { }
    public class BolusFilePathRequestMessage : RequestMessage<string> { }
    public class BolusOverhangMaterialRequestMessage : RequestMessage<DiffuseMaterial> { }
    public class BolusOverhangSettingsRequestMessage : RequestMessage<float[]> { }

    #endregion

    public class BolusStore {
        private BolusModel _bolus;
        private string _bolusFilePath;
        private DiffuseMaterial _overhangMaterial;
        private float _lowerOverhang, _upperOverhang;

        public BolusStore() {
            _bolus = new BolusModel();

            _lowerOverhang = 60.0f;
            _upperOverhang = 70.0f;
            _overhangMaterial = MeshSkin.GetOverHangSkin(_lowerOverhang, _upperOverhang);

            //registering received messages
            WeakReferenceMessenger.Default.Register<AddNewBolusMessage>(this, (r, m) => { AddBolus(m.label, m.mesh, m.filepath); });
            WeakReferenceMessenger.Default.Register<RemoveBolusMessage>(this, (r, m) => { RemoveBolus(m.label); });
            WeakReferenceMessenger.Default.Register<ClearBolusMessage>(this, (r, m) => { ClearBolus(); });
            WeakReferenceMessenger.Default.Register<ApplyRotationMessage>(this, (r,m) => { ApplyRotation(m.axis, m.angle); });
            WeakReferenceMessenger.Default.Register<ClearRotationsMessage>(this, (r,m)=> { ClearRotations(); });
            WeakReferenceMessenger.Default.Register<ApplyOverhangSettingsMessage>(this, (r, m) => { SetOverhangs(m.lower, m.upper); });

            //request messages
            WeakReferenceMessenger.Default.Register<BolusStore, BolusRequestMessage>(this, (r, m) => { m.Reply(r._bolus); });
            WeakReferenceMessenger.Default.Register<BolusStore, BolusFilePathRequestMessage>(this, (r,m) => { m.Reply(r._bolusFilePath); });
            WeakReferenceMessenger.Default.Register<BolusStore, BolusOverhangMaterialRequestMessage>(this, (r, m) => { m.Reply(r._overhangMaterial); });
            WeakReferenceMessenger.Default.Register<BolusStore, BolusOverhangSettingsRequestMessage>(this, (r, m) => {
                float[] settings = new float[] { r._lowerOverhang, r._upperOverhang };
                m.Reply(settings);
            });
        }

        private void SendBolusUpdate() => WeakReferenceMessenger.Default.Send(new BolusUpdatedMessage(_bolus));

        //messages
        private void AddBolus(string label, DMesh3 mesh, string filepath) {
            _bolus.AddMesh(label, mesh);
            if(filepath != "") _bolusFilePath= filepath;

            SendBolusUpdate();
        }

        private void RemoveBolus(string label) {
            _bolus.RemoveMesh(label);
            if (label == BolusModel.ORIGINAL_BOLUS_LABEL) _bolusFilePath = "";

            SendBolusUpdate();
        }

        private void ClearBolus() {
            _bolus = new BolusModel();
            _bolusFilePath = "";
            SendBolusUpdate();
        }

        private void ApplyRotation(Vector3 axis, double angle) {
            var a = new Vector3d {
                x = axis.X,
                y = axis.Y,
                z = axis.Z
            };

            _bolus.AddTransform(a, angle);
            SendBolusUpdate();
        }


        private void ClearRotations() {
            _bolus.ClearTransforms();
            SendBolusUpdate();
        }

        private void SetOverhangs(float lower, float upper) {
            _lowerOverhang = lower;
            _upperOverhang = upper;
            _overhangMaterial = MeshSkin.GetOverHangSkin(_lowerOverhang, _upperOverhang);
            WeakReferenceMessenger.Default.Send(new BolusOverhangsUpdated(_overhangMaterial));
        }
     }
}
