/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using log4net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;

namespace Greenshot.Base.Core.FileFormat;

/// <summary>
/// Reads the details Greenshot writes (<see cref="ExportImageMetadata"/>) back from an opened file into its capture details:
/// the capture time, the window title and the web address. So filename patterns and recipes work for an opened screenshot like for the capture.
/// Nothing else is read, camera details (location, serial numbers) are ignored.
/// </summary>
public static class ImageMetadataReader
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ImageMetadataReader));
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace Xmp = "http://ns.adobe.com/xap/1.0/";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";

    /// <summary>
    /// The key in <see cref="ICaptureDetails.MetaData"/> of a capture time read from the file (round trip format), see <see cref="ExportImageMetadata.CaptureTimeOf"/>
    /// </summary>
    public const string CaptureTimeKey = "capturetime";

    /// <summary>
    /// Read the details of the file into the capture details, when ImportMetadata is on
    /// </summary>
    public static void ReadInto(ICaptureDetails captureDetails, string filename)
    {
        if (captureDetails == null || string.IsNullOrEmpty(filename))
        {
            return;
        }
        if (IniConfigRegistry.TryGetSection<ICoreConfiguration>(out var coreConfiguration) && !coreConfiguration.ImportMetadata)
        {
            return;
        }
        try
        {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            ReadInto(captureDetails, stream);
        }
        catch (Exception ex)
        {
            // The image itself was loaded, missing details are no reason to fail
            Log.Warn($"Couldn't read the details of {filename}: {ex.Message}");
        }
    }

    /// <summary>
    /// Read the details of the image in the stream into the capture details
    /// </summary>
    public static void ReadInto(ICaptureDetails captureDetails, Stream stream)
    {
        // Identify reads the headers and metadata, not the pixels
        var info = Image.Identify(stream);
        if (info?.Metadata == null)
        {
            return;
        }
        var exif = info.Metadata.ExifProfile;
        var xmp = info.Metadata.XmpProfile?.GetDocument();

        var captureTime = XmpCaptureTime(xmp) ?? ExifCaptureTime(exif);
        if (captureTime.HasValue)
        {
            captureDetails.DateTime = captureTime.Value.LocalDateTime;
            captureDetails.AddMetaData(CaptureTimeKey, captureTime.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        string title = XmpAlternative(xmp, Dc + "title") ?? exif?.GetValue(ExifTag.XPTitle)?.Value?.TrimEnd('\0');
        if (!string.IsNullOrWhiteSpace(title))
        {
            captureDetails.Title = title;
            captureDetails.AddMetaData("windowtitle", title);
        }

        string url = xmp?.Descendants(Dc + "source").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(url))
        {
            captureDetails.AddMetaData("url", url);
        }
    }

    private static DateTimeOffset? XmpCaptureTime(XDocument xmp)
    {
        string value = xmp?.Descendants(Xmp + "CreateDate").FirstOrDefault()?.Value;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var time) ? time : null;
    }

    /// <summary>
    /// EXIF DateTimeOriginal "YYYY:MM:DD HH:MM:SS", with OffsetTimeOriginal "+HH:MM" when present, else local time
    /// </summary>
    private static DateTimeOffset? ExifCaptureTime(ExifProfile exif)
    {
        string value = exif?.GetValue(ExifTag.DateTimeOriginal)?.Value?.Trim('\0', ' ');
        if (!DateTime.TryParseExact(value, "yyyy':'MM':'dd HH':'mm':'ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return null;
        }
        string offset = exif.GetValue(ExifTag.OffsetTimeOriginal)?.Value?.Trim('\0', ' ');
        if (!string.IsNullOrEmpty(offset) && TimeSpan.TryParseExact(offset.TrimStart('+', '-'), "hh':'mm", CultureInfo.InvariantCulture, out var span))
        {
            return new DateTimeOffset(local, offset.StartsWith("-", StringComparison.Ordinal) ? span.Negate() : span);
        }
        return new DateTimeOffset(local);
    }

    private static string XmpAlternative(XDocument xmp, XName name)
    {
        var element = xmp?.Descendants(name).FirstOrDefault();
        if (element == null)
        {
            return null;
        }
        // rdf:Alt with one rdf:li per language, or a plain value
        var items = element.Descendants(Rdf + "li").ToList();
        var item = items.FirstOrDefault(li => (string)li.Attribute(XNamespace.Xml + "lang") == "x-default") ?? items.FirstOrDefault();
        return item?.Value ?? element.Value;
    }
}
