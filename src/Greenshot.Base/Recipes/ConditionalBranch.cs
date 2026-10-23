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
    /// One branch of a Conditional node: the first branch whose expression is true (or "else") decides which transitions with
    /// this key the flow follows.
    /// </summary>
    public class ConditionalBranch
    {
        /// <summary>The key of the branch, the "branch" of the transitions which leave the node by it.</summary>
        [JsonPropertyName("Key"), JsonRequired]
        public string Key { get; set; }

        /// <summary>An expression like "${payload.width > 800}", or "else".</summary>
        [JsonPropertyName("Expression"), JsonRequired]
        public string Expression { get; set; }

        public ConditionalBranch()
        {
        }

        public ConditionalBranch(string key, string expression)
        {
            Key = key;
            Expression = expression;
        }

        public override string ToString() => $"{Key}: {Expression}";
    }
}
