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
using Greenshot.Base.Languages;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Texts of recipes and extensions shown to the user: built-in extensions use language keys in the form Section.key
    /// (e.g. Recipe.extension_border), which are translated when the key exists; other texts (from recipe files) are shown as they are.
    /// </summary>
    public static class RecipeText
    {
        public static string Translate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            string key = text.Trim();
            // Only Section.key: a plain word of a recipe file ("Title") must not turn into a translation
            int dot = key.IndexOf('.');
            if (dot <= 0 || dot == key.Length - 1 || key.IndexOf(' ') >= 0)
            {
                return text;
            }

            try
            {
                return Texts.Config.TryGetTranslation(key, out var translated) ? translated : text;
            }
            catch (Exception)
            {
                // No languages loaded (tests, design time)
                return text;
            }
        }
    }
}
