using System.Linq;
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
    internal sealed class DimensionFormatMemento : IMemento
    {
        private readonly DrawableContainer _owner;
        private readonly int[] _formats;
        internal DimensionFormatMemento(DrawableContainer owner) { _owner = owner; _formats = owner.DimensionFormats; }
        public IMemento Restore()
        {
            var inverse = new DimensionFormatMemento(_owner); _owner.RestoreDimensionFormats(_formats);
            if (_owner.Parent != null) _owner.Parent.Modified = true; return inverse;
        }
        public bool Merge(IMemento other) => false;
        public void Dispose() { }
    }
    internal sealed class AllDimensionFormatsMemento : IMemento
    {
        private readonly Surface _surface;
        private readonly DimensionStyle _style;
        private readonly DrawableContainer[] _owners;
        private readonly int[][] _formats;
        internal AllDimensionFormatsMemento(Surface surface, DrawableContainer[] owners)
        { _surface = surface; _style = surface.DimensionStyle; _owners = owners; _formats = owners.Select(owner => owner.DimensionFormats).ToArray(); }
        public IMemento Restore()
        {
            var inverse = new AllDimensionFormatsMemento(_surface, _owners);
            for (int i = 0; i < _owners.Length; i++) _owners[i].RestoreDimensionFormats(_formats[i]);
            _surface.RestoreDimensionStyle(_style); _surface.Modified = true; return inverse;
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
