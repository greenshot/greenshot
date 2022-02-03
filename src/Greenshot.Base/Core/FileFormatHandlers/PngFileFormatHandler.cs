/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Base.Core.FileFormatHandlers
{
    /// <summary>
    /// This can be an ImageSharp implementation
    /// </summary>
    public class PngFileFormatHandler : IFileFormatHandler
    {
        private static readonly string[] OurExtensions = { "png" };

        /// <inheritdoc />
        public IEnumerable<string> SupportedExtensions(FileFormatHandlerActions fileFormatHandlerAction)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.LoadDrawableFromStream)
            {
                return Enumerable.Empty<string>();
            }

            return OurExtensions;
        }

        /// <inheritdoc />
        public bool Supports(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.LoadDrawableFromStream)
            {
                return false;
            }

            return OurExtensions.Contains(extension?.ToLowerInvariant());
        }
        /// <inheritdoc />
        public int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return int.MaxValue;
        }

        public bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }

        public bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            // TODO: Implement this
            return false;
        }

        public bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface parent)
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }
    }
}
