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

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    public enum LayerKind
    {
        /// <summary>
        /// The captured or opened image.
        /// </summary>
        Capture,

        /// <summary>
        /// What the user (or a recipe) added.
        /// </summary>
        Annotations,

        /// <summary>
        /// Things on top, like the mouse cursor.
        /// </summary>
        Overlay,

        Custom
    }

    /// <summary>
    /// A layer owns its elements, the first element is drawn first (bottom).
    /// </summary>
    public sealed record Layer(Guid Id, string Name, LayerKind Kind, IReadOnlyList<Element> Elements)
    {
        public bool Visible { get; init; } = true;

        // For reading JSON: a value which is missing gets its default here. Generated JSON code (DocumentJsonContext) sets
        // every init property, a missing one to 0 or null, and so loses a default which is only written at the property.
        [JsonConstructor]
        public Layer(Guid id, string name, LayerKind kind, IReadOnlyList<Element> elements, bool visible = true) : this(id, name, kind, elements) => Visible = visible;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Locked { get; init; }

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }
    }
}
