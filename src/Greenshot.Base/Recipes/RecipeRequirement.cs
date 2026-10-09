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

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Explicit declaration of an extension or plugin required by a capture recipe.
    /// </summary>
    public class RecipeRequirement
    {
        /// <summary>
        /// The unique ID of the required extension (e.g. "Greenshot.Plugin.Zxing").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User-friendly display name of the extension (e.g. "Barcode &amp; QR Code Extension").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Minimum required version of the extension (e.g. "1.0"), or null if any version is acceptable.
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// Optional URL where the extension can be found or downloaded.
        /// </summary>
        public string Url { get; set; }

        public RecipeRequirement()
        {
        }

        public RecipeRequirement(string id, string name = null, string minVersion = null, string url = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            MinVersion = minVersion;
            Url = url;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(MinVersion))
            {
                return $"{Name ?? Id} (v{MinVersion}+)";
            }
            return Name ?? Id;
        }
    }
}
