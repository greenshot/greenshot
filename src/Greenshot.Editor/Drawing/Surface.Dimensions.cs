using System;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface
    {
        public DimensionStyle DimensionStyle { get; private set; } = DimensionStyle.Default;
        public event EventHandler DimensionStyleChanged;
        public bool SetDimensionStyle(DimensionStyle style)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (DimensionStyle.Equals(style)) return false;
            MakeUndoable(new DimensionStyleMemento(this), false); RestoreDimensionStyle(style); Modified = true; return true;
        }
        internal void RestoreDimensionStyle(DimensionStyle style)
        {
            DimensionStyle = style ?? DimensionStyle.Default; DimensionStyleChanged?.Invoke(this, EventArgs.Empty); Invalidate();
        }
    }
}
