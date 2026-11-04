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
        // Primitive optional data keeps legacy project/clipboard type allowlists unchanged. -1 inherits the document.
        [OptionalField] private int[] _dimensionFormats;
        private const int MaximumDimensionFormats = 4096;
        internal int[] DimensionFormats => _dimensionFormats == null ? null : (int[])_dimensionFormats.Clone();
        internal DimensionFormat GetDimensionFormat(int index) => index >= 0 && index < (_dimensionFormats?.Length ?? 0)
            ? DimensionFormat.Unpack(_dimensionFormats[index]) : null;
        internal bool SetDimensionFormat(int index, DimensionFormat format)
        {
            int count = Math.Min(MaximumDimensionFormats, DimensionLayout.Edges(this).Length);
            if (index < 0 || index >= count || Equals(GetDimensionFormat(index), format)) return false;
            _parent?.MakeUndoable(new DimensionFormatMemento(this), false);
            var values = Enumerable.Repeat(-1, count).ToArray();
            if (_dimensionFormats != null) Array.Copy(_dimensionFormats, values, Math.Min(count, _dimensionFormats.Length));
            values[index] = format?.Packed ?? -1; RestoreDimensionFormats(values);
            if (_parent != null) _parent.Modified = true;
            return true;
        }
        internal void RestoreDimensionFormats(int[] values)
        {
            Invalidate();
            _dimensionFormats = values == null ? null : values.Take(MaximumDimensionFormats)
                .Select(value => DimensionFormat.Unpack(value)?.Packed ?? -1).ToArray();
            NotifyGeometryChanged(); Invalidate();
        }
        private void ValidateDimensionFormats()
        {
            if (_dimensionFormats != null && (_dimensionFormats.Length > MaximumDimensionFormats ||
                _dimensionFormats.Any(value => value != -1 && DimensionFormat.Unpack(value) == null))) _dimensionFormats = null;
        }
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
