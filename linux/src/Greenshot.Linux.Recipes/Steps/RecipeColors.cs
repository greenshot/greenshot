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

#nullable disable

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Steps
{
    /// <summary>
    /// Colors of recipes: #RGB, #RRGGBB, #AARRGGBB (like ColorTranslator.FromHtml and the Color options), the names of .NET
    /// colors and rgb(...)/rgba(...)
    /// </summary>
    public static partial class RecipeColors
    {
        private static readonly Dictionary<string, SKColor> Named = typeof(SKColors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(SKColor))
            .ToDictionary(f => f.Name, f => (SKColor)f.GetValue(null), StringComparer.OrdinalIgnoreCase);

        [GeneratedRegex(@"^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+)\s*)?\)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RgbPattern();

        public static SKColor Parse(object value, SKColor fallback)
        {
            string text = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }

            if (text.StartsWith('#'))
            {
                string hex = text.Substring(1);
                if (hex.Length == 3)
                {
                    hex = string.Concat(hex.Select(c => new string(c, 2)));
                }

                if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
                {
                    return hex.Length switch
                    {
                        6 => new SKColor(argb | 0xFF000000),
                        8 => new SKColor(argb),
                        _ => fallback,
                    };
                }

                return fallback;
            }

            var rgb = RgbPattern().Match(text);
            if (rgb.Success)
            {
                byte Channel(int group) => (byte)Math.Clamp(int.Parse(rgb.Groups[group].Value, CultureInfo.InvariantCulture), 0, 255);
                byte alpha = 255;
                if (rgb.Groups[4].Success && double.TryParse(rgb.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double a))
                {
                    // rgba(0, 0, 0, 180) in the examples, CSS writes 0.7
                    alpha = (byte)Math.Clamp(a <= 1 && rgb.Groups[4].Value.Contains('.') ? a * 255 : a, 0, 255);
                }

                return new SKColor(Channel(1), Channel(2), Channel(3), alpha);
            }

            return Named.TryGetValue(text, out var named) ? named : fallback;
        }
    }
}
