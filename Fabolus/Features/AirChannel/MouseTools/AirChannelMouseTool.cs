using Fabolus.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fabolus.Features.AirChannel.MouseTools {
    public abstract class AirChannelMouseTool : MouseTool {
        public virtual int? SelectedChannel { get; protected set; }

        public override abstract void MouseDown(MouseEventArgs mouse);

        public override abstract void MouseMove(MouseEventArgs mouse);

        public override abstract void MouseUp(MouseEventArgs mouse);

    }
}
