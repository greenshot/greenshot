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
using System.ServiceModel.Security;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Editor.FileFormat.V1.Legacy;
using log4net;

namespace Greenshot.Editor.FileFormat.V1;

/// <summary>
/// Provides methods for loading Greenshot file format version V1.
/// </summary>
/// <remarks>Greenshot file format version V1 supports Greenshot file versions 01.02, 01.03 and 01.04.
/// Saving to this format is not supported anymore.</remarks>
internal static class GreenshotFileV1
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileV1));

    /// <summary>
    /// Determines whether the stream matches the Greenshot file format version V1.
    /// </summary>
    /// <remarks>It checks for specific markers ("Greenshot01.02", "Greenshot01.03" or
    /// "Greenshot01.04") and validates the size of surface / container list data. The stream's position will be
    /// modified during the operation but will remain open after the method completes.</remarks>
    /// <param name="greenshotFileStream">The stream containing the file to check. The stream must support seeking.</param>
    /// <returns><see langword="true"/> if the file format matches the Greenshot version V1 and the file contains surface
    /// data; otherwise, <see langword="false"/>.</returns>
    internal static bool DoesFileFormatMatch(Stream greenshotFileStream)
    {
        // Start at -14 read "GreenshotXX.YY" (XX=Major, YY=Minor)
        const int markerSize = 14;
        greenshotFileStream.Seek(-markerSize, SeekOrigin.End);
        // set leaveOpen to prevent the automatic closing of the file stream
        using var streamReader = new StreamReader(greenshotFileStream, Encoding.ASCII, false, 1024, true);
        var greenshotMarker = streamReader.ReadToEnd();

        // only these two known marker are allowed 
        var foundMarkerV0102 = greenshotMarker.Equals("Greenshot01.02");
        var foundMarkerV0103 = greenshotMarker.Equals("Greenshot01.03");
        var foundMarkerV0104 = greenshotMarker.Equals("Greenshot01.04");

        // Load 8 byte in front of marker for file size 
        const int fileSizeLocation = 8 + markerSize;
        greenshotFileStream.Seek(-fileSizeLocation, SeekOrigin.End);
        // set leaveOpen to prevent the automatic closing of the file stream
        using BinaryReader reader = new BinaryReader(greenshotFileStream, Encoding.ASCII, true);
        var bytesWritten = reader.ReadInt64();

        if (foundMarkerV0102 && bytesWritten > 0)
        {
            Log.InfoFormat("Greenshot file format: {0}", greenshotMarker);
            return true;
        }

        if (foundMarkerV0103 && bytesWritten > 0)
        {
            Log.InfoFormat("Greenshot file format: {0}", greenshotMarker);
            return true;
        }

        if (foundMarkerV0104 && bytesWritten > 0)
        {
            Log.InfoFormat("Greenshot file format: {0}", greenshotMarker);
            return true;
        }

        return false;
    }

    /// <summary>
    /// This load function support Greenshot file version 01.02, 01.03 and 01.04.
    /// </summary>
    /// <param name="greenshotFileStream">A <see cref="Stream"/> containing the Greenshot file data.</param>
    /// <returns>The loaded Greenshot file.</returns>
    /// <exception cref="ArgumentException">If schema version is not found in the stream.</exception>
    /// <exception cref="SecurityAccessDeniedException">If legacy container list cannot be read due to security restrictions.</exception>
    internal static GreenshotFile LoadFromStream(Stream greenshotFileStream)
    {
        GreenshotFile returnGreenshotFile = new GreenshotFile
        {
            FormatVersion = GreenshotFileVersionHandler.GreenshotFileFormatVersion.V1,
        };

        const int markerSize = 14;
        greenshotFileStream.Seek(-markerSize, SeekOrigin.End);
        // set leaveOpen to prevent the automatic closing of the file stream
        using var streamReader = new StreamReader(greenshotFileStream, Encoding.ASCII, false, 1024, true);
        var greenshotMarker = streamReader.ReadToEnd();

        // only these two known marker are allowed 
        var foundMarkerV0102 = greenshotMarker.Equals("Greenshot01.02");
        var foundMarkerV0103 = greenshotMarker.Equals("Greenshot01.03");
        var foundMarkerV0104 = greenshotMarker.Equals("Greenshot01.04");

        if (foundMarkerV0102)
        {
            returnGreenshotFile.SchemaVersion = 2;
        }
        else if (foundMarkerV0103)
        {
            returnGreenshotFile.SchemaVersion = 3;
        }else if (foundMarkerV0104)
        {
            returnGreenshotFile.SchemaVersion = 4;
        }
        else
        {
            throw new ArgumentException("No schema version found in Greenshot file stream!");
        }


        // 14 byte for marker and 8 byte for filesize
        const int fileSizeLocation = 8 + markerSize;
        greenshotFileStream.Seek(-fileSizeLocation, SeekOrigin.End);
        // set leaveOpen to prevent the automatic closing of the file stream
        using BinaryReader reader = new BinaryReader(greenshotFileStream, Encoding.ASCII, true);
        long bytesWritten = reader.ReadInt64();
        greenshotFileStream.Seek(-(bytesWritten + fileSizeLocation), SeekOrigin.End);

        try
        {
            returnGreenshotFile.ContainerList = LegacyFileHelper.GetContainerListFromLegacyContainerListStream(greenshotFileStream);
        }
        catch (SecurityAccessDeniedException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error("Error serializing elements from stream.", e);
        }

        Bitmap captureBitmap;

        // reset stream to load image 
        greenshotFileStream.Seek(0, SeekOrigin.Begin);
        // Fixed problem that the bitmap stream is disposed... by Cloning the image
        // This also ensures the bitmap is correctly created
        using (Image tmpImage = Image.FromStream(greenshotFileStream, true, true))
        {
            Log.DebugFormat("Loaded .greenshot file with Size {0}x{1} and PixelFormat {2}", tmpImage.Width, tmpImage.Height, tmpImage.PixelFormat);
            captureBitmap = ImageHelper.Clone(tmpImage) as Bitmap;
        }

        if (captureBitmap != null)
        {
            returnGreenshotFile.Image = captureBitmap;
            Log.InfoFormat("Information about .greenshot file: {0}x{1}-{2} Resolution {3}x{4}", captureBitmap.Width, captureBitmap.Height, captureBitmap.PixelFormat, captureBitmap.HorizontalResolution, captureBitmap.VerticalResolution);
        }

        return returnGreenshotFile;
    }
}