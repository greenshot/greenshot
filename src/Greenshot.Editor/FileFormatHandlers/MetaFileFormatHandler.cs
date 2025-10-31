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
using System.Drawing.Imaging;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This handles the Windows metafile files
    /// </summary>
    public class MetaFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MetaFileFormatHandler));
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".wmf", ".emf" };
        
        public MetaFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
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
        public override IEnumerable<IDrawableContainer> LoadDrawablesFromStream(Stream stream, string extension, ISurface surface = null)
        {
            MetafileContainer metafileContainer = null;
            try
            {
                metafileContainer = new MetafileContainer(stream, surface);
            }
            catch (Exception ex)
            {
                Log.Error("Can't load Metafile", ex);
            }
            if (metafileContainer != null)
            {
                yield return metafileContainer;
            }
        }
    }
}
