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
using Greenshot.Base.Core;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Texts of recipes and extensions shown to the user: built-in extensions use language keys, which are translated
    /// when the key exists; other texts (from recipe files) are shown as they are.
    /// </summary>
    public static class RecipeText
    {
        public static string Translate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            try
            {
                return Language.TryGetString(text.Trim(), out var translated) ? translated : text;
            }
            catch (Exception)
            {
                // No languages loaded (tests, design time)
                return text;
            }
        }
    }
}
