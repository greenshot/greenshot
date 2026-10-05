/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Recipes;
using Greenshot.Base.Languages;

namespace Greenshot.Settings
{
    /// <summary>
    /// Converts the "#RRGGBB" / "#AARRGGBB" text of a color option
    /// </summary>
    public static class RecipeOptionColors
    {
        public static System.Drawing.Color Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return System.Drawing.Color.Black;
            string hex = text.Trim().TrimStart('#');
            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb)) return System.Drawing.Color.Black;
            if (hex.Length == 6) argb |= 0xFF000000;
            return System.Drawing.Color.FromArgb(unchecked((int)argb));
        }

        public static string Format(System.Drawing.Color color)
        {
            return color.A == 255
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
