using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Measurements;

namespace Greenshot.Editor.Memento
{
    internal sealed class DimensionVisibilityMemento : IMemento
    {
        private readonly DrawableContainer _owner;
        private readonly bool[] _flags;
        internal DimensionVisibilityMemento(DrawableContainer owner) { _owner = owner; _flags = owner.DimensionVisibility; }
        public IMemento Restore()
        {
            var inverse = new DimensionVisibilityMemento(_owner); _owner.RestoreDimensionVisibility(_flags);
            if (_owner.Parent != null) _owner.Parent.Modified = true; return inverse;
        }
        public bool Merge(IMemento other) => false;
        public void Dispose() { }
    }
    internal sealed class DimensionStyleMemento : IMemento
    {
        private readonly Surface _surface;
        private readonly DimensionStyle _style;
        internal DimensionStyleMemento(Surface surface) { _surface = surface; _style = surface.DimensionStyle; }
        public IMemento Restore()
        { var inverse = new DimensionStyleMemento(_surface); _surface.RestoreDimensionStyle(_style); _surface.Modified = true; return inverse; }
        public bool Merge(IMemento other) => false;
        public void Dispose() { }
    }
}
