﻿/*
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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormatHandlers
{
    public abstract class AbstractFileFormatHandler : IFileFormatHandler
    {
        /// <inheritdoc />
        public IDictionary<FileFormatHandlerActions, IList<string>> SupportedExtensions { get; } = new Dictionary<FileFormatHandlerActions, IList<string>>();

        /// <inheritdoc />
        public virtual int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return 0;
        }

        public abstract bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null);

        public abstract bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap);

        /// <summary>
        /// Default implementation taking the TryLoadFromStream image and placing it in an ImageContainer 
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="drawableContainer">IDrawableContainer out</param>
        /// <param name="parentSurface">ISurface</param>
        /// <returns>bool</returns>
        public virtual bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface parentSurface = null)
        {
            if (TryLoadFromStream(stream, extension, out var bitmap))
            {
                var imageContainer = new ImageContainer(parentSurface)
                {
                    Image = bitmap
                };
                drawableContainer = imageContainer;
                return true;
            }

            drawableContainer = null;
            return true;
        }
    }
}
