using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Shapes {
    public abstract class MoldShapeViewModelBase : ObservableObject {
        protected bool _isFrozen;
        public abstract void Initialize();
    }
}
