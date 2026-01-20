/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.ServiceModel.Security;
using Greenshot.Editor.FileFormat.V1.Legacy;
using log4net;

namespace Greenshot.Editor.FileFormat.V1;

/// <summary>
/// Provides methods for loading Greenshot template in file format version V1.
/// </summary>
/// <remarks>Greenshot template file format version V1 supports Greenshot template files from app version 1.2, 1.3 and 1.4.
/// Saving to this format is not supported anymore.</remarks>
internal static class GreenshotTemplateV1
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotTemplateV1));

    /// <summary>Determines whether the stream matches the Greenshot template format version V1.
    /// </summary>
    /// <remarks>In this file format is no specific marker or version information,
    /// so it is not possible to detect the file format version without deserializing the file.
    /// The stream's position will be modified during the operation but will remain open after the method completes.</remarks>
    internal static bool DoesFileFormatMatch(Stream greenshotTemplateFileStream)
    {
        // This file format has no specific marker, so we just check if we can deserialize it and could find at least one container.     
        var greenshotTemplate = LoadFromStream(greenshotTemplateFileStream);
        if (greenshotTemplate is { ContainerList.Count: > 0 })
        {
            Log.Info("Greenshot template file format: V1");
            return true;
        }

        return false;
    }

    /// <summary>
    /// This load function support Greenshot template from app version 1.2 , 1.3 and 1.4.
    /// </summary>
    /// <remarks>In this file format is no specific version information, so schema version is always 4.
    /// The stream's position will be modified during the operation but will remain open after the method completes.</remarks>
    /// <param name="greenshotTemplateFileStream">A <see cref="Stream"/> containing the Greenshot template data.</param>
    /// <returns>The loaded Greenshot template.</returns>
    /// <exception cref="ArgumentException"></exception>
    internal static GreenshotTemplate LoadFromStream(Stream greenshotTemplateFileStream)
    {
        // reset position
        greenshotTemplateFileStream.Seek(0, SeekOrigin.Begin);

        GreenshotTemplate greenshotTemplate = new GreenshotTemplate
        {
            FormatVersion = GreenshotFileVersionHandler.GreenshotFileFormatVersion.V1,
            SchemaVersion = -1
        };

        try
        {
            greenshotTemplate.ContainerList = LegacyFileHelper.GetContainerListFromLegacyContainerListStream(greenshotTemplateFileStream);
        }
        catch (SecurityAccessDeniedException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error("Error serializing elements from stream.", e);
        }

        greenshotTemplate.SchemaVersion = 4; // The schema version for V1 is always 4, as this is the last version that was used in the old Greenshot file format.

        return greenshotTemplate;
    }

}