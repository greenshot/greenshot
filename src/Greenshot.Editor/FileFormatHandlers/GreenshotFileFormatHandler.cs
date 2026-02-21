/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers;

public sealed class GreenshotFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileFormatHandler));
    private readonly IReadOnlyCollection<string> _saveExtensions = new[] { ".gsa" };
    private readonly IReadOnlyCollection<string> _loadExtensions = new[] { ".greenshot",".gsa" };
    public GreenshotFileFormatHandler()
    {
        SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _loadExtensions;
        SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _loadExtensions;
        SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _saveExtensions;
    }

    /// <summary>
    /// Save the surface to the specified stream in the current .greenshot file format.
    /// </summary>
    /// <remarks>Ignores the given bitmap, as the .greenshot file always uses the original surface image.</remarks>
    /// <returns><see langword="true"/> if the surface was successfully saved to the stream; otherwise, <see langword="false"/>.</returns>
    public override bool TrySaveToStream(Bitmap bitmap, Stream stream, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
    {
        if (surface == null)
        {
            return false;
        }

        try
        {
            //ignore the given bitmap, in .greenshot file we always use the original surface image
            return GreenshotFileVersionHandler.SaveToStreamInCurrentVersion(surface, stream);
        }
        catch (Exception ex)
        {
            Log.Error("Couldn't save surface as .greenshot: ", ex);
        }

        return false;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <remarks>This implementation loads the <see cref="GreenshotFile"/> from stream. Creates a <see cref="ISurface"/> and uses <see cref="Surface.GetImageForExport"/> wich renders all contained elements into the image.</remarks>
    /// <returns><see langword="true"/> if the bitmap was successfully loaded from the stream; otherwise, <see
    /// langword="false"/>.</returns>
    public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
    {
        try
        {
            var surface = GreenshotFileVersionHandler.CreateSurfaceFromStream(stream);

            bitmap = (Bitmap)surface.GetImageForExport();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Couldn't load .greenshot: ", ex);
        }
        bitmap = null;
        return false;
    }

    /// <summary>
    /// Load a <see cref="ISurface"/> from file path
    /// </summary>
    /// <remarks>This implementation loads the <see cref="GreenshotFile"/> from file. Creates a <see cref="ISurface"/>.</remarks>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    public ISurface LoadGreenshotSurface(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            Log.Warn("No file path provided for loading Greenshot surface.");
            return null;
        }
        Log.InfoFormat("Loading surface data from file {0}", fullPath);

        using Stream greenshotFileStream = File.OpenRead(fullPath);
        ISurface returnSurface = GreenshotFileVersionHandler.CreateSurfaceFromStream(greenshotFileStream);

        Log.InfoFormat("Information about file {0}: {1}x{2}-{3} Resolution {4}x{5}", fullPath, returnSurface.Image.Width, returnSurface.Image.Height,
                returnSurface.Image.PixelFormat, returnSurface.Image.HorizontalResolution, returnSurface.Image.VerticalResolution);

        return returnSurface;
    }
}

