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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using log4net;
using Svg;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This handled the loading of SVG images to the editor
    /// </summary>
    public class SvgFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SvgFileFormatHandler));
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".svg" };

        public SvgFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            var svgDocument = SvgDocument.Open<SvgDocument>(stream);

            try
            {
                bitmap = svgDocument.Draw();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Can't load SVG", ex);
            }
            bitmap = null;
            return false;
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            // TODO: Implement this
            return false;
        }

        public override IEnumerable<IDrawableContainer> LoadDrawablesFromStream(Stream stream, string extension, ISurface parent = null)
        {
            SvgContainer svgContainer = null;
            try
            {
                svgContainer = new SvgContainer(stream, parent);
            }
            catch (Exception ex)
            {
                Log.Error("Can't load SVG", ex);
            }
            if (svgContainer != null)
            {
                yield return svgContainer;
            }
        }
    }
}
