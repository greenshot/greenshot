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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Document.Rendering
{
    /// <summary>
    /// The decoded images of the resources, for an editor which draws the same document again and again. Resources can't
    /// change, so an image stays right as long as its resource id is used. Not thread-safe: one cache per drawing thread.
    /// </summary>
    public sealed class DocumentRenderCache : IDisposable
    {
        private readonly Dictionary<Guid, Image<Rgba32>> _images = new();

        /// <summary>
        /// The decoded image of the resource, null when it couldn't be decoded
        /// </summary>
        public bool TryGet(Guid resource, out Image<Rgba32> image) => _images.TryGetValue(resource, out image);

        public void Add(Guid resource, Image<Rgba32> image)
        {
            if (_images.TryGetValue(resource, out var old) && !ReferenceEquals(old, image))
            {
                old?.Dispose();
            }

            _images[resource] = image;
        }

        /// <summary>
        /// Frees the images of the resources which aren't in the list
        /// </summary>
        public void KeepOnly(ICollection<Guid> resources)
        {
            foreach (var id in new List<Guid>(_images.Keys))
            {
                if (!resources.Contains(id))
                {
                    _images[id]?.Dispose();
                    _images.Remove(id);
                }
            }
        }

        public void Dispose()
        {
            foreach (var image in _images.Values)
            {
                image?.Dispose();
            }

            _images.Clear();
        }
    }
}
