using System;
using System.Drawing;
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
        [OptionalField] private int[] _dimensionBackgrounds;
        [OptionalField] private bool[] _dimensionBackgroundOverrides;
        internal int[] DimensionBackgrounds => _dimensionBackgrounds == null ? null : (int[])_dimensionBackgrounds.Clone();
        internal bool[] DimensionBackgroundOverrides => _dimensionBackgroundOverrides == null ? null : (bool[])_dimensionBackgroundOverrides.Clone();
        internal Color? GetDimensionBackground(int index) => index >= 0 && index < (_dimensionBackgroundOverrides?.Length ?? 0)
            && index < (_dimensionBackgrounds?.Length ?? 0) && _dimensionBackgroundOverrides[index] ? Color.FromArgb(_dimensionBackgrounds[index]) : (Color?)null;
        internal bool SetDimensionBackground(int index, Color? color)
        {
            int count = Math.Min(MaximumDimensionFormats, DimensionLayout.Edges(this).Length);
            if (index < 0 || index >= count || GetDimensionBackground(index)?.ToArgb() == color?.ToArgb()) return false;
            _parent?.MakeUndoable(new DimensionBackgroundMemento(this), false);
            var colors = new int[count]; var overrides = new bool[count];
            if (_dimensionBackgrounds != null) Array.Copy(_dimensionBackgrounds, colors, Math.Min(count, _dimensionBackgrounds.Length));
            if (_dimensionBackgroundOverrides != null) Array.Copy(_dimensionBackgroundOverrides, overrides, Math.Min(count, _dimensionBackgroundOverrides.Length));
            colors[index] = color?.ToArgb() ?? 0; overrides[index] = color.HasValue;
            RestoreDimensionBackgrounds(colors, overrides);
            if (_parent != null) _parent.Modified = true;
            return true;
        }
        internal void RestoreDimensionBackgrounds(int[] colors, bool[] overrides)
        {
            Invalidate();
            _dimensionBackgrounds = colors?.Take(MaximumDimensionFormats).ToArray();
            _dimensionBackgroundOverrides = overrides?.Take(MaximumDimensionFormats).ToArray();
            NotifyGeometryChanged(); Invalidate();
        }
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
            if ((_dimensionBackgrounds?.Length ?? 0) > MaximumDimensionFormats ||
                (_dimensionBackgroundOverrides?.Length ?? 0) != (_dimensionBackgrounds?.Length ?? 0))
            { _dimensionBackgrounds = null; _dimensionBackgroundOverrides = null; }
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
