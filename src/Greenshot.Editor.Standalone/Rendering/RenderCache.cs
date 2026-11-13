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
using System.Linq;
using Greenshot.Document;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Standalone.Rendering
{
    /// <summary>
    /// What the renderer keeps between two drawings of (versions of) a document: the decoded images of its resources and
    /// the outlines of the glyphs it drew. One per open document, disposed with it.
    /// </summary>
    public sealed class RenderCache : IDisposable
    {
        private readonly object _lock = new();
        private readonly Dictionary<Guid, Image<Rgba32>> _images = new();

        /// <summary>
        /// The glyph outlines of the texts which were drawn
        /// </summary>
        public DrawingTextCache Text { get; } = new();

        /// <summary>
        /// The decoded image of a resource, null when it isn't an image ImageSharp can read
        /// </summary>
        public Image<Rgba32> ImageOf(GreenshotDocument document, Guid id)
        {
            lock (_lock)
            {
                if (_images.TryGetValue(id, out var image))
                {
                    return image;
                }

                image = null;
                if (document.Resources.TryGetValue(id, out var resource) && resource.MediaType != null &&
                    resource.MediaType.StartsWith("image/", StringComparison.Ordinal) && resource.MediaType != "image/svg+xml")
                {
                    try
                    {
                        using var stream = resource.OpenRead();
                        image = Image.Load<Rgba32>(stream);
                    }
                    catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
                    {
                        // A format ImageSharp can't read (e.g. an icon) isn't drawn
                    }
                }

                _images[id] = image;
                return image;
            }
        }

        /// <summary>
        /// Forgets the images of resources the document no longer has
        /// </summary>
        public void Keep(GreenshotDocument document)
        {
            lock (_lock)
            {
                foreach (var id in _images.Keys.Where(id => !document.Resources.ContainsKey(id)).ToList())
                {
                    _images[id]?.Dispose();
                    _images.Remove(id);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var image in _images.Values)
                {
                    image?.Dispose();
                }

                _images.Clear();
            }
        }
    }
}
