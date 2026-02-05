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
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.V1;
using Greenshot.Editor.FileFormat.V2;
using log4net;
using static Greenshot.Editor.FileFormat.GreenshotFileVersionHandler;

namespace Greenshot.Editor.FileFormat;

/// <summary>
/// Provides functionality for handling different Greenshot template file format versions.
/// </summary>
public class GreenshotTemplateVersionHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileVersionHandler));

    /// <summary>
    /// Loads a <see cref="GreenshotTemplate"/> from the stream.
    /// </summary>
    /// <remarks>This method detects the file format version of the Greenshot template and loads the GreenshotTemplate accordingly.</remarks>
    /// <param name="greenshotTemplateStream">A <see cref="Stream"/> containing the Greenshot template data.</param>
    /// <returns>The loaded <see cref="GreenshotTemplate"/> .</returns>
    /// <exception cref="ArgumentException">Thrown if the stream does not contain a valid Greenshot template.</exception>
    public static GreenshotTemplate LoadFromStream(Stream greenshotTemplateStream)
    {
        GreenshotFileFormatVersion fileFormatVersion = DetectFileFormatVersion(greenshotTemplateStream);

        return fileFormatVersion switch
        {
            GreenshotFileFormatVersion.V1 => GreenshotTemplateV1.LoadFromStream(greenshotTemplateStream),
            GreenshotFileFormatVersion.V2 => GreenshotTemplateV2.LoadFromStream(greenshotTemplateStream),
            _ => throw new ArgumentException("Stream is not a Greenshot template file!")
        };
    }

    /// <summary>
    /// <inheritdoc cref="CreateGreenshotFileInCurrentVersion"/>
    /// <inheritdoc cref="SaveToStream"/>
    /// </summary>
    public static bool SaveToStreamInCurrentVersion(DrawableContainerList elements, Stream stream) =>
        SaveToStream(CreateGreenshotTemplateInCurrentVersion(elements), stream);

    /// <summary>
    /// <inheritdoc cref="GreenshotTemplateV2.SaveToStream"/>
    /// </summary>
    private static bool SaveToStream(GreenshotTemplate greenshotTemplate, Stream stream) => GreenshotTemplateV2.SaveToStream(greenshotTemplate, stream);


    private static GreenshotFileFormatVersion DetectFileFormatVersion(Stream drawableContainerListFileStream)
    {
        // check for newest version first

        if (GreenshotTemplateV2.DoesFileFormatMatch(drawableContainerListFileStream))
        {
            return GreenshotFileFormatVersion.V2;
        }

        if (GreenshotTemplateV1.DoesFileFormatMatch(drawableContainerListFileStream))
        {
            return GreenshotFileFormatVersion.V1;
        }

        Log.Error("Stream does not contain a known Greenshot template file format!");
        throw new ArgumentException("Stream does not contain a known Greenshot template file format!");
    }

    /// <summary>
    /// Creates a new <see cref="GreenshotTemplate"/> with the specified elements and also
    /// configured with the current format and schema versions.
    /// </summary>
    /// <param name="elements"></param>
    /// <returns></returns>
    private static GreenshotTemplate CreateGreenshotTemplateInCurrentVersion(DrawableContainerList elements)
    {
        return new GreenshotTemplate
        {
            ContainerList = elements,
            MetaInformation = new GreenshotTemplateMetaInformation
            {
                FormatVersion = GreenshotFileFormatVersion.V2,
                SchemaVersion = CurrentSchemaVersion,
            }
        };
    }

}