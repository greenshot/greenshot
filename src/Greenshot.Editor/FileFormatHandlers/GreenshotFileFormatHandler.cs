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
using System.Drawing;
using System.IO;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers;

/// <summary>
/// Handles the .gsa (Greenshot Archive) and .greenshot file format,
/// </summary>
public sealed class GreenshotFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileFormatHandler));
    private readonly IReadOnlyCollection<string> _saveExtensions = [".gsa", ".greenshot"];
    private readonly IReadOnlyCollection<string> _loadExtensions = [".greenshot", ".gsa"];
    public GreenshotFileFormatHandler()
    {
        SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _loadExtensions;
        SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _loadExtensions;
        SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _saveExtensions;
        SupportedExtensions[FileFormatHandlerActions.SaveToFile] = _saveExtensions;
        SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = _loadExtensions;
    }

    /// <summary>
    /// Save the surface to the specified stream in the current greenshot file format.<br/>
    /// <inheritdoc cref="GreenshotFileVersionHandler.SaveToStreamInCurrentVersion(ISurface, Stream)"/>
    /// </summary>
    /// <remarks>Ignores the given bitmap, as the greenshot file always uses the original surface image.</remarks>
    /// <returns><see langword="true"/> if the surface was successfully saved to the stream; otherwise, <see langword="false"/>.</returns>
    public override bool TrySaveToStream(Bitmap bitmap, Stream stream, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
    {
        if (surface == null)
        {
            return false;
        }

        try
        {
            // Ignore the given bitmap: editor files always contain the original surface image.
            return GreenshotFileVersionHandler.SaveToStreamInDedicatedFormat(surface, stream, extension);
        }
        catch (Exception ex)
        {
            Log.Error($"Couldn't save surface as greenshot file (extension=\"{extension}\"", ex);
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
            Log.Error($"Couldn't load greenshot file (extension=\"{extension}\"", ex);
        }
        bitmap = null;
        return false;
    }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <remarks>This implementation loads the <see cref="GreenshotFile"/> from stream and creates a <see cref="ISurface"/>.</remarks>
    public override ISurface LoadSurface(Stream surfaceFileStream) => GreenshotFileVersionHandler.CreateSurfaceFromStream(surfaceFileStream);

    /// <summary>
    /// Loads a surface from a Greenshot editor file.
    /// </summary>
    /// <param name="fullPath">The path to the Greenshot editor file.</param>
    /// <returns>The loaded surface, or <see langword="null"/> when no path was provided.</returns>
    public ISurface LoadGreenshotSurface(string fullPath) => GreenshotFileVersionHandler.LoadGreenshotSurface(fullPath);

}

