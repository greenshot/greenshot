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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Greenshot.Linux.Tests
{
    /// <summary>
    /// Reads values of JSON replies in the tests
    /// </summary>
    internal static class JsonNodeTestExtensions
    {
        /// <summary>
        /// The value of the property as T: a text, number or boolean, or the JsonArray / JsonObject; default when it is missing or null
        /// </summary>
        public static T Value<T>(this JsonNode node, string name) => node is JsonObject jsonObject ? jsonObject[name].Value<T>() : default;

        /// <summary>
        /// The items of a JsonArray as T
        /// </summary>
        public static IEnumerable<T> Values<T>(this JsonNode node) => node.AsArray().Select(item => item.Value<T>());

        /// <summary>
        /// The node as T: a text, number or boolean, or the JsonArray / JsonObject; default for null
        /// </summary>
        public static T Value<T>(this JsonNode node)
        {
            switch (node)
            {
                case null:
                    return default;
                case T typed:
                    return typed;
                case JsonValue value:
                    return value.GetValue<T>();
                default:
                    throw new InvalidCastException($"The JSON {node.ToJsonString()} isn't a {typeof(T).Name}");
            }
        }
    }
}
