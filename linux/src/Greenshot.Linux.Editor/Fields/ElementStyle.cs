/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Fields;

/// <summary>
/// The field values of an element. Every element has all values, <see cref="Elements.DrawableElement.Fields"/>
/// tells which ones it uses; the others are ignored.
/// </summary>
public sealed class ElementStyle
{
    public int LineThickness { get; set; } = 2;

    public SKColor LineColor { get; set; } = SKColors.Red;

    public SKColor FillColor { get; set; } = SKColors.Transparent;

    public bool Shadow { get; set; } = true;

    public ArrowHeads ArrowHeads { get; set; } = ArrowHeads.End;

    public string FontFamily { get; set; } = "Sans";

    public float FontSize { get; set; } = 11f;

    public bool FontBold { get; set; }

    public bool FontItalic { get; set; }

    public TextHorizontalAlignment TextHorizontalAlignment { get; set; } = TextHorizontalAlignment.Center;

    public TextVerticalAlignment TextVerticalAlignment { get; set; } = TextVerticalAlignment.Center;

    public HighlightFilter HighlightFilter { get; set; } = HighlightFilter.TextHighlight;

    public ObfuscateFilter ObfuscateFilter { get; set; } = ObfuscateFilter.Pixelize;

    public int BlurRadius { get; set; } = 3;

    public int PixelSize { get; set; } = 5;

    public int MagnificationFactor { get; set; } = 2;

    public CutMarkStyle CutMarkStyle { get; set; } = CutMarkStyle.Torn;

    public int ToothHeight { get; set; } = 12;

    public int ToothRange { get; set; } = 20;

    public ElementStyle Clone() => (ElementStyle)MemberwiseClone();

    public object Get(FieldType field) => field switch
    {
        FieldType.LineThickness => LineThickness,
        FieldType.LineColor => LineColor,
        FieldType.FillColor => FillColor,
        FieldType.Shadow => Shadow,
        FieldType.ArrowHeads => ArrowHeads,
        FieldType.FontFamily => FontFamily,
        FieldType.FontSize => FontSize,
        FieldType.FontBold => FontBold,
        FieldType.FontItalic => FontItalic,
        FieldType.TextHorizontalAlignment => TextHorizontalAlignment,
        FieldType.TextVerticalAlignment => TextVerticalAlignment,
        FieldType.HighlightFilter => HighlightFilter,
        FieldType.ObfuscateFilter => ObfuscateFilter,
        FieldType.BlurRadius => BlurRadius,
        FieldType.PixelSize => PixelSize,
        FieldType.MagnificationFactor => MagnificationFactor,
        FieldType.CutMarkStyle => CutMarkStyle,
        FieldType.ToothHeight => ToothHeight,
        FieldType.ToothRange => ToothRange,
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    public void Set(FieldType field, object value)
    {
        switch (field)
        {
            case FieldType.LineThickness:
                LineThickness = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture), 0, 100);
                break;
            case FieldType.LineColor:
                LineColor = (SKColor)value;
                break;
            case FieldType.FillColor:
                FillColor = (SKColor)value;
                break;
            case FieldType.Shadow:
                Shadow = (bool)value;
                break;
            case FieldType.ArrowHeads:
                ArrowHeads = (ArrowHeads)value;
                break;
            case FieldType.FontFamily:
                FontFamily = (string)value;
                break;
            case FieldType.FontSize:
                FontSize = Math.Clamp(Convert.ToSingle(value, CultureInfo.InvariantCulture), 4f, 500f);
                break;
            case FieldType.FontBold:
                FontBold = (bool)value;
                break;
            case FieldType.FontItalic:
                FontItalic = (bool)value;
                break;
            case FieldType.TextHorizontalAlignment:
                TextHorizontalAlignment = (TextHorizontalAlignment)value;
                break;
            case FieldType.TextVerticalAlignment:
                TextVerticalAlignment = (TextVerticalAlignment)value;
                break;
            case FieldType.HighlightFilter:
                HighlightFilter = (HighlightFilter)value;
                break;
            case FieldType.ObfuscateFilter:
                ObfuscateFilter = (ObfuscateFilter)value;
                break;
            case FieldType.BlurRadius:
                BlurRadius = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture), 1, 100);
                break;
            case FieldType.PixelSize:
                PixelSize = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture), 2, 100);
                break;
            case FieldType.MagnificationFactor:
                MagnificationFactor = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture), 2, 8);
                break;
            case FieldType.CutMarkStyle:
                CutMarkStyle = (CutMarkStyle)value;
                break;
            case FieldType.ToothHeight:
                ToothHeight = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture), 1, 100);
                break;
            case FieldType.ToothRange:
                ToothRange = Math.Clamp(Convert.ToInt32(value, CultureInfo.InvariantCulture), 2, 100);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    /// <summary>
    /// A value as text for greenshot.ini: colors as #AARRGGBB, enums by name, numbers invariant
    /// </summary>
    public static string Format(object value) => value switch
    {
        SKColor color => $"#{color.Alpha:X2}{color.Red:X2}{color.Green:X2}{color.Blue:X2}",
        bool flag => flag ? "True" : "False",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    /// <summary>
    /// Read a value written by <see cref="Format"/>, null when it doesn't fit the field
    /// </summary>
    public static object? Parse(FieldType field, string text)
    {
        var sample = new ElementStyle().Get(field);
        try
        {
            return sample switch
            {
                SKColor => SKColor.TryParse(text, out var color) ? color : null,
                bool => bool.TryParse(text, out var flag) ? flag : null,
                int => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null,
                float => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var size) ? size : null,
                string => text,
                Enum => Enum.TryParse(sample.GetType(), text, ignoreCase: true, out var parsed) ? parsed : null,
                _ => null,
            };
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
