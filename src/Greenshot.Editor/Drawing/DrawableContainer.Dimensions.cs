using System;
using System.Linq;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.Extensions;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    public abstract partial class DrawableContainer
    {
        [OptionalField] private bool[] _dimensionVisibility;
        internal bool[] DimensionVisibility => _dimensionVisibility == null ? Array.Empty<bool>() : (bool[])_dimensionVisibility.Clone();
        internal bool HasVisibleDimensions => _dimensionVisibility?.Any(value => value) == true;
        internal bool IsDimensionVisible(int index) => index >= 0 && index < (_dimensionVisibility?.Length ?? 0) && _dimensionVisibility[index];
        internal bool SetDimensionVisible(int index, bool visible)
        {
            int count = DimensionLayout.Edges(this).Length;
            if (index < 0 || index >= count || IsDimensionVisible(index) == visible) return false;
            _parent?.MakeUndoable(new DimensionVisibilityMemento(this), false);
            var flags = new bool[count];
            if (_dimensionVisibility != null) Array.Copy(_dimensionVisibility, flags, Math.Min(count, _dimensionVisibility.Length));
            flags[index] = visible; RestoreDimensionVisibility(flags);
            if (_parent != null) _parent.Modified = true;
            return true;
        }
        internal void RestoreDimensionVisibility(bool[] flags)
        {
            Invalidate(); _dimensionVisibility = flags == null ? null : (bool[])flags.Clone();
            NotifyGeometryChanged(); Invalidate();
        }
        internal NativeRect WithDimensionBounds(NativeRect geometry)
        {
            foreach (var label in DimensionLayout.Labels(this)) geometry = geometry.Union(label.Bounds);
            return geometry;
        }
    }
}
