using System;
using System.Drawing;

namespace Greenshot.Editor.Measurements
{
    public sealed class DimensionStyle : IEquatable<DimensionStyle>
    {
        public static DimensionStyle Default { get; } = new DimensionStyle(FontFamily.GenericSansSerif.Name, 11, FontStyle.Regular, Color.Black);
        public string Family { get; }
        public float Size { get; }
        public FontStyle FontStyle { get; }
        public Color Color { get; }
        public DimensionFormat Format { get; }
        public Color BackgroundColor { get; }
        public DimensionStyle(string family, float size, FontStyle style, Color color, DimensionFormat format = null, Color? backgroundColor = null)
        {
            if (string.IsNullOrWhiteSpace(family) || family.Length > 128 || float.IsNaN(size) || size < 1 || size > 144 || ((int)style & ~15) != 0)
                throw new ArgumentException("Invalid dimension font.");
            Family = family; Size = size; FontStyle = style; Color = color;
            Format = format ?? DimensionFormat.Automatic; BackgroundColor = backgroundColor ?? System.Drawing.Color.Transparent;
        }
        public DimensionStyle WithFormat(DimensionFormat format) => new DimensionStyle(Family, Size, FontStyle, Color, format, BackgroundColor);
        public DimensionStyle WithBackground(Color color) => new DimensionStyle(Family, Size, FontStyle, Color, Format,
            color == System.Drawing.Color.Transparent ? System.Drawing.Color.FromArgb(0, BackgroundColor) : color);
        public DimensionStyle WithBackgroundTransparency(bool transparent) => new DimensionStyle(Family, Size, FontStyle, Color, Format,
            System.Drawing.Color.FromArgb(transparent ? 0 : 255, BackgroundColor));
        public Font CreateFont()
        {
            try { return new Font(Family, Size * 96 / 72, FontStyle, GraphicsUnit.Pixel); }
            catch (ArgumentException) { return new Font(FontFamily.GenericSansSerif, Size * 96 / 72, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel); }
        }
        public bool Equals(DimensionStyle other) => other != null && Family == other.Family && Size == other.Size && FontStyle == other.FontStyle && Color.ToArgb() == other.Color.ToArgb() && Format.Equals(other.Format) && BackgroundColor.ToArgb() == other.BackgroundColor.ToArgb();
        public override bool Equals(object other) => Equals(other as DimensionStyle);
        public override int GetHashCode() => Family.GetHashCode() ^ Size.GetHashCode() ^ Color.ToArgb() ^ (int)FontStyle;
    }
}
