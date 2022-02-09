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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This handles the Windows metafile files
    /// </summary>
    public class MetaFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly string [] OurExtensions = { ".wmf", ".emf" };

        /// <inheritdoc />
        public IEnumerable<string> SupportedExtensions(FileFormatHandlerActions fileFormatHandlerAction)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.SaveToStream)
            {
                return Enumerable.Empty<string>();
            }

            return OurExtensions;
        }

        /// <inheritdoc />
        public bool Supports(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.SaveToStream)
            {
                return false;
            }

            return OurExtensions.Contains(NormalizeExtension(extension));
        }

        /// <inheritdoc />
        public int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return int.MaxValue;
        }

        /// <inheritdoc />
        public bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            return false;
        }

        /// <inheritdoc />
        public bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            try
            {
                if (Image.FromStream(stream, true, true) is Metafile metaFile)
                {
                    bitmap = ImageHelper.Clone(metaFile, PixelFormat.Format32bppArgb);
                    return true;
                }
            }
            catch
            {
                // Ignore
            }
            bitmap = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface surface = null)
        {
            if (Image.FromStream(stream, true, true) is Metafile metaFile)
            {
                drawableContainer = new MetafileContainer(metaFile, surface);
                return true;
            }

            drawableContainer = null;
            return false;
        }
    }
}
