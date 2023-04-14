using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Contours;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold
{

    #region Messages
    //set messages
    public sealed record MoldSetShapeMessage(MoldShape shape);
    public sealed record MoldSetContourIndexMessage(int index);
    public sealed record MoldSetContourMessage(ContourBase contour);
    public sealed record MoldSetSettingsMessage(MoldStore.MoldSettings settings);
    public sealed record MoldSetFinalShapeMessage(MeshGeometry3D? mesh = null);

    //updates
    public sealed record MoldContourUpdatedMessage(ContourModelBase contour);
    public sealed record MoldShapeUpdatedMessage(MoldShape shape);
    public sealed record MoldSettingsUpdatedMessage(MoldStore.MoldSettings settings);
    public sealed record MoldFinalUpdatedMessage(MeshGeometry3D? mesh);

    //request messages
    public class MoldSettingsRequestMessage : RequestMessage<MoldStore.MoldSettings> { }
    public class MoldShapeRequestMessage : RequestMessage<MoldShape> { }
    public class MoldContourRequestMessage : RequestMessage<ContourModelBase> { }
    public class MoldFinalRequestMessage : RequestMessage<MeshGeometry3D?> { }
    public class MoldShapesRequestMessage : RequestMessage<List<string>> { }    

    #endregion

    public class MoldStore {
        public struct MoldSettings {
            public double OffsetXY;
            public double OffsetTop;
            public double OffsetBottom;
            
            public int Resolution;
        }

        private MoldSettings _settings;
        private BolusModel? _bolus;
        private MoldShape _shape;
        private MeshGeometry3D? _geometry; 
        private List<ContourModelBase> _contours;
        private int _activeContour;
        public MoldStore() {  
            _contours = new List<ContourModelBase> {
                new BoxContourModel()
            };
            _activeContour = 0;

            _settings = new MoldSettings {
                OffsetXY = 4.0f,
                OffsetTop = 4.0f,
                OffsetBottom = 4.0f,
                Resolution = 64,
            };

            _shape = new MoldBox(_settings);
            _geometry = null;

            //messages
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { NewBolus(m.bolus); });
            WeakReferenceMessenger.Default.Register<MoldSetShapeMessage>(this, (r, m) => { NewShape(m.shape); });
            WeakReferenceMessenger.Default.Register<MoldSetSettingsMessage>(this, (r, m) => { NewSettings(m.settings); });
            WeakReferenceMessenger.Default.Register<MoldSetFinalShapeMessage>(this, (r, m) => { NewFinalMold(m.mesh); });
            WeakReferenceMessenger.Default.Register<MoldSetContourIndexMessage>(this, (r, m) => {
                _activeContour = m.index;
                WeakReferenceMessenger.Default.Send(new MoldContourUpdatedMessage(_contours[_activeContour]));
            });
            WeakReferenceMessenger.Default.Register<MoldSetContourMessage>(this, (r, m) => {
                var index = _contours.FindIndex(c => c.ContourType == m.contour.GetType());
                _contours[index].Contour = m.contour;
                WeakReferenceMessenger.Default.Send(new MoldContourUpdatedMessage(_contours[_activeContour]));
            });

            //request messages
            WeakReferenceMessenger.Default.Register<MoldShapesRequestMessage>(this, (r, m) => {
                var names = new List<string>();
                foreach (var contour in _contours) names.Add(contour.Name);
                m.Reply(names);
            });
            WeakReferenceMessenger.Default.Register<MoldContourRequestMessage>(this, (r, m) => { m.Reply(_contours[_activeContour]); });
            WeakReferenceMessenger.Default.Register<MoldSettingsRequestMessage>(this, (r, m) => { m.Reply(_settings); });
            WeakReferenceMessenger.Default.Register<MoldShapeRequestMessage>(this, (r, m) => { m.Reply(_shape); });
            WeakReferenceMessenger.Default.Register<MoldFinalRequestMessage>(this, (r, m) => { m.Reply(_geometry); });
        }

        #region Messaging Responses
        private void NewSettings(MoldStore.MoldSettings settings) { 
            _settings = settings;
            _shape.SetSettings(_settings);
            WeakReferenceMessenger.Default.Send(new MoldSettingsUpdatedMessage(settings)); //do we actually need this?
            WeakReferenceMessenger.Default.Send(new MoldShapeUpdatedMessage(_shape));
        }

        private void NewBolus(BolusModel bolus) {
            _bolus = bolus;
            _shape.SetBolus(_bolus);
            WeakReferenceMessenger.Default.Send(new MoldShapeUpdatedMessage(_shape));
        }

        private void NewFinalMold(MeshGeometry3D? mesh) {
            _geometry = mesh;
            WeakReferenceMessenger.Default.Send(new MoldFinalUpdatedMessage(mesh));
        }

        private void NewShape(MoldShape? shape) {
            _geometry = null;

            _shape = shape;
            _shape.SetBolus(_bolus);

            WeakReferenceMessenger.Default.Send(new MoldShapeUpdatedMessage(_shape));
        }
        #endregion
    }
}
