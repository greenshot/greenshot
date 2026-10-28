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

namespace Greenshot.Linux.Editor.Effects;

/// <summary>
/// The settings of the drop shadow effect, the DropShadowEffect of Windows. In greenshot.ini as
/// "Darkness:0.60|ShadowSize:7|ShadowOffset:-1,-1", like the EffectConverter writes it.
/// </summary>
public class DropShadowSettings
{
    public float Darkness { get; set; } = 0.6f;

    public int ShadowSize { get; set; } = 7;

    public SKPointI ShadowOffset { get; set; } = new(-1, -1);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"Darkness:{Darkness:F2}|ShadowSize:{ShadowSize}|ShadowOffset:{ShadowOffset.X},{ShadowOffset.Y}");

    public static DropShadowSettings Parse(string? text)
    {
        var settings = new DropShadowSettings();
        settings.Read(text);
        return settings;
    }

    /// <summary>
    /// The name:value pairs separated by |, unknown or broken values keep their default
    /// </summary>
    protected static IEnumerable<(string Name, string Value)> Pairs(string? text) =>
        (text ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(':', 2))
            .Where(pair => pair.Length == 2)
            .Select(pair => (pair[0].Trim(), pair[1].Trim()));

    protected virtual void Read(string? text)
    {
        foreach (var (name, value) in Pairs(text))
        {
            switch (name)
            {
                case "Darkness" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var darkness) && darkness is >= 0 and <= 1:
                    Darkness = darkness;
                    break;
                case "ShadowSize" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size):
                    ShadowSize = size;
                    break;
                case "ShadowOffset":
                    var coordinates = value.Split(',');
                    if (coordinates.Length == 2 && int.TryParse(coordinates[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                        && int.TryParse(coordinates[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                    {
                        ShadowOffset = new SKPointI(x, y);
                    }

                    break;
            }
        }
    }
}

/// <summary>
/// The settings of the torn edge effect, the TornEdgeEffect of Windows: the drop shadow settings, then
/// "GenerateShadow:True|ToothHeight:12|HorizontalToothRange:20|VerticalToothRange:20|Edges:True,True,True,True|BackgroundColor:0"
/// </summary>
public sealed class TornEdgeSettings : DropShadowSettings
{
    public int ToothHeight { get; set; } = 12;

    public int HorizontalToothRange { get; set; } = 20;

    public int VerticalToothRange { get; set; } = 20;

    /// <summary>
    /// Top, right, bottom, left
    /// </summary>
    public bool[] Edges { get; set; } = [true, true, true, true];

    public bool GenerateShadow { get; set; } = true;

    /// <summary>
    /// The color of the torn off parts and behind the shadow, transparent by default
    /// </summary>
    public SKColor BackgroundColor { get; set; } = SKColors.Transparent;

    /// <summary>
    /// The random edges of an element, not stored in greenshot.ini
    /// </summary>
    public int Seed { get; set; }

    public TornEdgeSettings Clone()
    {
        var clone = (TornEdgeSettings)MemberwiseClone();
        clone.Edges = (bool[])Edges.Clone();
        return clone;
    }

    public override string ToString() =>
        base.ToString() + string.Create(CultureInfo.InvariantCulture,
            $"|GenerateShadow:{GenerateShadow}|ToothHeight:{ToothHeight}|HorizontalToothRange:{HorizontalToothRange}|VerticalToothRange:{VerticalToothRange}" +
            $"|Edges:{Edges[0]},{Edges[1]},{Edges[2]},{Edges[3]}|BackgroundColor:{(int)(uint)BackgroundColor}");

    public static new TornEdgeSettings Parse(string? text)
    {
        var settings = new TornEdgeSettings();
        settings.Read(text);
        return settings;
    }

    protected override void Read(string? text)
    {
        base.Read(text);
        foreach (var (name, value) in Pairs(text))
        {
            switch (name)
            {
                case "GenerateShadow" when bool.TryParse(value, out var shadow):
                    GenerateShadow = shadow;
                    break;
                case "ToothHeight" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height):
                    ToothHeight = height;
                    break;
                case "HorizontalToothRange" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var horizontal):
                    HorizontalToothRange = horizontal;
                    break;
                case "VerticalToothRange" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vertical):
                    VerticalToothRange = vertical;
                    break;
                case "Edges":
                    var edges = value.Split(',');
                    for (int i = 0; i < Math.Min(4, edges.Length); i++)
                    {
                        if (bool.TryParse(edges[i], out var edge))
                        {
                            Edges[i] = edge;
                        }
                    }

                    break;
                case "BackgroundColor" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var argb):
                    BackgroundColor = new SKColor((uint)argb);
                    break;
            }
        }
    }
}
