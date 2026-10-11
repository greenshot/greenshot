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

namespace Greenshot.Document
{
    /// <summary>
    /// The size of the document and settings for all of it.
    /// </summary>
    public sealed record Canvas(int Width, int Height)
    {
        public Rgba Background { get; init; } = Rgba.Transparent;

        /// <summary>
        /// The number of the first step label.
        /// </summary>
        public int CounterStart { get; init; } = 1;
    }

    /// <summary>
    /// Bytes an element references by id, in their original format (e.g. image/png, image/svg+xml).
    /// </summary>
    public sealed record Resource(Guid Id, string MediaType, byte[] Data);

    /// <summary>
    /// A Greenshot document: a canvas with layers of elements and the resources they reference.
    /// The first layer is drawn first (bottom). Documents are immutable, a change creates a new document.
    /// </summary>
    public sealed record GreenshotDocument(Guid Id, Canvas Canvas, IReadOnlyList<Layer> Layers, IReadOnlyDictionary<Guid, Resource> Resources);
}
