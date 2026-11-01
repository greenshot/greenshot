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

using System.Text.Json.Serialization;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// A person who wrote a recipe or an extension. Kept in the file, so the credit travels with it.
    /// </summary>
    public class RecipeAuthor
    {
        /// <summary>
        /// The name as the author wants to be credited
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The GitHub login of the author, optional
        /// </summary>
        [JsonPropertyName("github"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string GitHub { get; set; }

        public RecipeAuthor Clone() => new RecipeAuthor { Name = Name, GitHub = GitHub };

        public override string ToString() => Name;
    }
}
