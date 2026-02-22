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
using System.Drawing;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.V1;
using Greenshot.Editor.FileFormat.V2;
using log4net;

namespace Greenshot.Editor.FileFormat;

/// <summary>
/// Provides functionality for handling all supported Greenshot file format versions.
/// <remarks>It also provides methods to create a <see cref="GreenshotFile"/> from a surface and to create a <see cref="ISurface"/> from a Greenshot file.</remarks>
/// </summary>
public sealed class GreenshotFileVersionHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileVersionHandler));

    /// <summary>
    /// Specifies the internal file types supported by Greenshot
    /// </summary>
    public enum GreenshotFileType
    {
        GreenshotFile = 0,
        GreenshotTemplate = 1
    }

    /// <summary>
    /// Represents the file format version for greenshot files. This includes greenshot templates as well.
    /// </summary>
    /// <remarks> The file versions are now independent of the app version.<br/>
    /// The version numbers of greenshot 1.2 matched old greenshot file version 01.02 and greenshot 1.3 matched old file version 01.03.
    /// Now the definition changed a bit and it is composed of two parts.
    /// <code> {serializer version}.{schema version} </code>
    /// The first part is <see cref="GreenshotFileFormatVersion"/>. This decides wich serializer/ binary data structure is used.<br/>
    /// The second part is <see cref="GreenshotFileDto.SchemaVersion"/>. This schema version only needs to be changed if certain actions are necessary for backward compatibility.<br/>
    /// 
    /// The old versions still fit this pattern.
    /// </remarks>
    public enum GreenshotFileFormatVersion
    {
        Unknown = 0,
        /// <summary>
        /// This format uses BinaryFormat serialization, supporting Greenshot file versions 01.02 and 01.03
        /// </summary>
        V1 = 1,
        /// <summary>
        /// This format uses System.Text.Json serialization 
        /// </summary>
        V2 = 2
    }

    /// <summary>
    /// Version of the current file schema. More precisely, the version of the Dto structure. This includes greenshot templates as well.
    /// </summary>
    /// <remarks>
    /// Increase this version if you change the Dto structure in a way that breaks backward compatibility.
    /// After incrementing this version, you need to extend <see cref="GreenshotFileV2.MigrateToCurrentVersion"/>.
    /// <para>
    /// ./FileFormat/readme.md for more information about the file format and versioning.
    /// </para></remarks>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Loads a <see cref="GreenshotFile"/> from the stream.
    /// </summary>
    /// <remarks>This method detects the file format version of the Greenshot file and loads the GreenshotFile accordingly.</remarks>
    /// <param name="greenshotFileStream">A <see cref="Stream"/> containing the Greenshot file data.</param>
    /// <returns>The loaded <see cref="GreenshotFile"/> .</returns>
    /// <exception cref="ArgumentException">Thrown if the stream does not contain a valid Greenshot file.</exception>
    public static GreenshotFile LoadFromStream(Stream greenshotFileStream)
    {
        GreenshotFileFormatVersion fileFormatVersion = DetectFileFormatVersion(greenshotFileStream);

        return fileFormatVersion switch
        {
            GreenshotFileFormatVersion.V1 => GreenshotFileV1.LoadFromStream(greenshotFileStream),
            GreenshotFileFormatVersion.V2 => GreenshotFileV2.LoadFromStream(greenshotFileStream),
            _ => throw new ArgumentException("Stream is not a Greenshot file!")
        };

    }

    /// <summary>
    /// <inheritdoc cref="CreateGreenshotFile"/>
    /// <inheritdoc cref="SaveToStream"/>
    /// </summary>
    public static bool SaveToStreamInCurrentVersion( ISurface surface, Stream stream) =>
        SaveToStream(CreateGreenshotFile(surface), stream);

    /// <summary>
    /// <inheritdoc cref="GreenshotFileV2.SaveToStream"/>
    /// </summary>
    private static bool SaveToStream(GreenshotFile greenshotFile, Stream stream) => GreenshotFileV2.SaveToStream(greenshotFile, stream);

    /// <summary>
    /// Detects the Greenshot file version from the provided stream.
    /// </summary>
    /// <param name="greenshotFileStream">A <see cref="Stream"/> containing the Greenshot file data.</param>
    /// <returns><see cref="GreenshotFileFormatVersion"/></returns>
    /// <exception cref="ArgumentException">Thrown if the file format version cannot be determined.</exception>
    private static GreenshotFileFormatVersion DetectFileFormatVersion(Stream greenshotFileStream)
    {
        // check for newest version first

        if (GreenshotFileV2.DoesFileFormatMatch(greenshotFileStream))
        {
            return GreenshotFileFormatVersion.V2;
        }

        if (GreenshotFileV1.DoesFileFormatMatch(greenshotFileStream))
        {
            return GreenshotFileFormatVersion.V1;
        }

        Log.Error("Stream does not contain a known Greenshot file format!");
        throw new ArgumentException("Stream does not contain a known Greenshot file format!");
    }

    /// <summary>
    /// Creates a <see cref="GreenshotFile"/> from surface.
    /// <inheritdoc cref="CreateGreenshotFileInCurrentVersion"/>
    /// </summary>
    /// <param name="surface">Cannot be <see langword="null"/>.</param>
    /// <returns>A <see cref="GreenshotFile"/> with surface's image, rendered image, and elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="surface"/> is <see langword="null"/> or if its <see cref="ISurface.Image"/> is <see langword="null"/>.</exception>
    public static GreenshotFile CreateGreenshotFile(ISurface surface)
    {
        if (surface == null)
        {
            Log.Error("Surface cannot be null");
            throw new ArgumentNullException(nameof(surface), "Surface cannot be null");
        }

        if (surface.Image == null)
        {
            Log.Error("Surface image cannot be null");
            throw new ArgumentNullException(nameof(surface.Image), "Surface image cannot be null");
        }

        return CreateGreenshotFileInCurrentVersion(
            (Bitmap)surface.Image,
            (Bitmap)surface.GetImageForExport(),
            new DrawableContainerList(surface.Elements));
    }

    /// <summary>
    /// Creates a new <see cref="GreenshotFile"/> with the specified image and elements and also
    /// configured with the current format and schema versions.
    /// </summary>
    /// <param name="image">Background image of the surface. </param>
    /// <param name="renderedImage">rendered image of the surface, which includes all drawable containers. </param>
    /// <param name="elements">Elements that are positioned on the surface, like text, shapes, and images. </param>
    /// <returns></returns>
    private static GreenshotFile CreateGreenshotFileInCurrentVersion(Bitmap image, Bitmap renderedImage, DrawableContainerList elements)
    {
        return new GreenshotFile
        {
            Image = image,
            RenderedImage = renderedImage,
            ContainerList = elements,
            MetaInformation = new GreenshotFileMetaInformation
            {
                FormatVersion = GreenshotFileFormatVersion.V2,
                SchemaVersion = CurrentSchemaVersion,
            },
        };
    }

    /// <summary>
    /// Creates a new <see cref="ISurface"/> instance and initializes it with the data from the specified <see
    /// cref="GreenshotFile"/>. It validates the data and creates always a new surface, or throw an exception.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="greenshotFile"/> is <see langword="null"/>
    /// or if its <see cref="GreenshotFile.Image"/> is <see langword="null"/>.</exception>
    public static ISurface CreateSurface(GreenshotFile greenshotFile)
    {
        if (greenshotFile == null)
        {
            Log.Error("Greenshot file cannot be null");
            throw new ArgumentNullException(nameof(greenshotFile), "Greenshot file cannot be null");
        }

        if (greenshotFile.Image == null)
        {
            // a new surface should always have an image, so this is a critical error
            Log.Error("Greenshot file image cannot be null");
            throw new ArgumentNullException(nameof(greenshotFile.Image), "Greenshot file image cannot be null");
        }

        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();

        surface.Image = greenshotFile.Image;
        
        if (greenshotFile.ContainerList != null)
        {
            // An empty container list is allowed, it's a normal case when no elements are present.
            surface.LoadElements(greenshotFile.ContainerList);
        }

        return surface;
    }

    /// <summary>
    /// <inheritdoc cref="LoadFromStream"/>
    /// <inheritdoc cref="CreateSurface"/>
    /// </summary>
    public static ISurface CreateSurfaceFromStream(Stream stream) =>
        CreateSurface(LoadFromStream(stream));
}