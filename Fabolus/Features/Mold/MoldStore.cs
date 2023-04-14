using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Bolus;
using Fabolus.Features.Mold.Contours;
using g3;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Mold
{

    #region Messages
    //set messages
    public sealed record MoldSetContourIndexMessage(int index);
    public sealed record MoldSetContourMessage(ContourBase contour);
    public sealed record MoldSetFinalShapeMessage(MeshGeometry3D? mesh = null);

    //updates
    public sealed record MoldContourUpdatedMessage(ContourModelBase contour);
    public sealed record MoldFinalUpdatedMessage(MeshGeometry3D? mesh);

    //request messages
    public class MoldContourRequestMessage : RequestMessage<ContourModelBase> { }
    public class MoldFinalRequestMessage : RequestMessage<MeshGeometry3D?> { }
    public class MoldListRequestMessage : RequestMessage<List<string>> { }    

    #endregion

    public class MoldStore {
        private MeshGeometry3D? _geometry; 
        private List<ContourModelBase> _contours;
        private int _activeContour;
        public MoldStore() {  
            _contours = new List<ContourModelBase> {
                new BoxContourModel()
            };
            _activeContour = 0;
            _geometry = null;

            //messages
            WeakReferenceMessenger.Default.Register<MoldSetFinalShapeMessage>(this, (r, m) => {
                _geometry = m.mesh;
                WeakReferenceMessenger.Default.Send(new MoldFinalUpdatedMessage(m.mesh));
            });
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
            WeakReferenceMessenger.Default.Register<MoldListRequestMessage>(this, (r, m) => {
                var names = new List<string>();
                foreach (var contour in _contours) names.Add(contour.Name);
                m.Reply(names);
            });

            WeakReferenceMessenger.Default.Register<MoldContourRequestMessage>(this, (r, m) => { m.Reply(_contours[_activeContour]); });
            WeakReferenceMessenger.Default.Register<MoldFinalRequestMessage>(this, (r, m) => { m.Reply(_geometry); });
        }
    }
}
