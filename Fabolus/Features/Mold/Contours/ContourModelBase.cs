﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Mold.Contours {
    /// <summary>
    /// Used to collect all objects related to a specific type of contouring abolus for a mold
    /// </summary>
    public abstract class ContourModelBase {
        public virtual string Name { get; }
        public virtual float Offset { get; }
        public virtual Type ContourType { get; protected set; }
        public virtual ContourBase Contour { get; set; }
        public virtual ContourViewModelBase ViewModel { get; protected set; }
    }
}
