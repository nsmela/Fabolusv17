using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fabolus.Features.Bolus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold
{

    #region Messages
    //set messages
    public sealed record MoldSetShapeMessage(MoldShape shape);
    public sealed record MoldSetSettingsMessage(MoldStore.MoldSettings settings);
    //updates
    public sealed record MoldShapeUpdatedMessage(MoldShape shape);
    public sealed record MoldSettingsUpdatedMessage(MoldStore.MoldSettings settings);

    //request messages
    public class MoldSettingsRequestMessage : RequestMessage<MoldStore.MoldSettings> { }
    public class MoldShapeRequestMessage : RequestMessage<MoldShape> { }

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

        public MoldStore() {
            _settings = new MoldSettings {
                OffsetXY = 4.0f,
                OffsetTop = 4.0f,
                OffsetBottom = 4.0f,
                Resolution = 64,
            };

            _shape = new MoldBox(_settings);

            //messages
            WeakReferenceMessenger.Default.Register<BolusUpdatedMessage>(this, (r, m) => { NewBolus(m.bolus); });
            WeakReferenceMessenger.Default.Register<MoldSetSettingsMessage>(this, (r, m) => { NewSettings(m.settings); });

            //request messages
            WeakReferenceMessenger.Default.Register<MoldSettingsRequestMessage>(this, (r, m) => { m.Reply(_settings); });
            WeakReferenceMessenger.Default.Register<MoldShapeRequestMessage>(this, (r, m) => { m.Reply(_shape); });
        }

        #region Messaging Responses
        private void NewSettings(MoldStore.MoldSettings settings) { 
            _settings = settings;
            _shape.SetSettings(_settings);
            WeakReferenceMessenger.Default.Send(new MoldSettingsUpdatedMessage(settings));
        }

        private void NewBolus(BolusModel bolus) {
            _bolus = bolus;
            _shape.SetBolus(_bolus);
            WeakReferenceMessenger.Default.Send(new MoldShapeUpdatedMessage(_shape));
        }
        #endregion
    }
}
