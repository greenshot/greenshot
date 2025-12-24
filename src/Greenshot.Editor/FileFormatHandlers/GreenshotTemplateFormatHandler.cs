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
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers;

/// <summary>
/// Handles the .gst (Greenshot Template) file format, similar to GreenshotFileFormatHandler but for templates.
/// </summary>
public sealed class GreenshotTemplateFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotTemplateFormatHandler));
    private readonly IReadOnlyCollection<string> _ourExtensions = [".gst"];
    public GreenshotTemplateFormatHandler()
    {
        SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
        SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = [];
        SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
    }

    /// <inheritdoc />
    public override bool TrySaveToStream(Bitmap bitmap, Stream stream, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
    {
        if (surface == null)
        {
            return false;
        }

        try
        {
            return GreenshotTemplateVersionHandler.SaveToStreamInCurrentVersion(new DrawableContainerList(surface.Elements), stream);
        }
        catch (Exception ex)
        {
            Log.Error("Couldn't save surface as .gst: ", ex);
        }

        return false;
    }

    /// <summary>
    /// Saves the surface data to a file in the Greenshot template format (.gst).
    /// </summary>
    /// <param name="fullPath">The full file path where the template will be saved. Cannot be null or empty.</param>
    /// <param name="surface">The surface to save as a template. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fullPath"/> is null or empty, or if <paramref name="surface"/> is null.</exception>
    /// <exception cref="Exception">Thrown if the template fails to save to the specified file.</exception>
    public void SaveTemplateToFile(string fullPath, ISurface surface)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            throw new ArgumentNullException(nameof(fullPath), "Cannot save Greenshot template to stream, fullPath is null or empty.");
        }
        if (surface == null)
        {
            throw new ArgumentNullException(nameof(surface), "Cannot save Greenshot template, surface is null.");
        }
        Log.InfoFormat("Saving template surface data to file {0}", fullPath);

        using Stream fileStreamWrite = File.OpenWrite(fullPath);

        if (!TrySaveToStream(null, fileStreamWrite, ".gst", surface, null))
        {
            throw new Exception("Failed to save Greenshot template to file.");
        }
    }

    /// <inheritdoc />
    /// <remarks>Currently not supported</remarks>
    public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
    {
        throw new NotImplementedException("Greenshot template (.gst) does not support save as image.");
    }

    /// <summary>
    /// Loads a Greenshot template from the specified file and applies its elements to the surface.
    /// </summary>
    /// <param name="fullPath">The full path to the file containing the Greenshot template. Cannot be null or empty.</param>
    /// <param name="surface">The surface to which the template elements will be applied. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fullPath"/> is null or empty, or if <paramref name="surface"/> is null.</exception>
    public void LoadTemplateFromFile(string fullPath, ISurface surface)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            throw new ArgumentNullException(nameof(fullPath), "Cannot load Greenshot template, fullPath is null or empty.");
        }
        if (surface == null)
        {
            throw new ArgumentNullException(nameof(surface), "Cannot load Greenshot template, surface is null.");
        }
        Log.InfoFormat("Loading template surface data from file {0}", fullPath);

        using Stream fileStreamRead = File.OpenRead(fullPath);
        var greenshotTemplate = GreenshotTemplateVersionHandler.LoadFromStream(fileStreamRead);
        surface.LoadElements(greenshotTemplate.ContainerList);
    }
}
