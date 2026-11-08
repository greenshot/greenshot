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

using System.Drawing;
using Greenshot.Base.Languages;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// The color of a color button in words, for its accessible description: a screen reader can't see the bar under the icon
    /// </summary>
    internal static class ColorDescription
    {
        /// <summary>
        /// "Color: #FF8000", "Color: #FF8000, opacity 50 %" or "Color: Transparent"
        /// </summary>
        public static string Describe(Color color)
        {
            if (color.A == 0)
            {
                return string.Format(Texts.Editor.ColorValue, Texts.Editor.ColorTransparent);
            }

            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            if (color.A == 255)
            {
                return string.Format(Texts.Editor.ColorValue, hex);
            }

            return string.Format(Texts.Editor.ColorValueOpacity, hex, (color.A * 100 + 127) / 255);
        }
    }
}
