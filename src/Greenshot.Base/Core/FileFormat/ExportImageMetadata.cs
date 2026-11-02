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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Dapplo.Ini;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Xmp;

namespace Greenshot.Base.Core.FileFormat;

/// <summary>
/// The details Greenshot writes into an exported image file, independent of the library which encodes it.
/// The file format handlers map it: WIC by metadata paths, ImageSharp with <see cref="ApplyTo"/>.
/// Nothing from a loaded file is carried over, only what is set here.
/// </summary>
public sealed class ExportImageMetadata
{
    private static readonly XNamespace X = "adobe:ns:meta/";
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace Xmp = "http://ns.adobe.com/xap/1.0/";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";

    /// <summary>
    /// The program which wrote the file, always set
    /// </summary>
    public string Software { get; set; }

    /// <summary>
    /// When the screen was captured, null when unknown or not wanted
    /// </summary>
    public DateTimeOffset? CaptureTime { get; set; }

    /// <summary>
    /// The author, null when not wanted
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    /// The copyright notice, null when not wanted
    /// </summary>
    public string Copyright { get; set; }

    /// <summary>
    /// True when there is more than the program name to write
    /// </summary>
    public bool HasDetails => CaptureTime.HasValue || !string.IsNullOrEmpty(Author) || !string.IsNullOrEmpty(Copyright);

    /// <summary>
    /// The capture time as EXIF DateTimeOriginal: local time "YYYY:MM:DD HH:MM:SS"
    /// </summary>
    public string ExifCaptureTime => CaptureTime?.ToString("yyyy':'MM':'dd HH':'mm':'ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// The offset of the capture time to UTC as EXIF OffsetTimeOriginal: "+HH:MM"
    /// </summary>
    public string ExifCaptureTimeOffset => CaptureTime?.ToString("zzz", CultureInfo.InvariantCulture);

    /// <summary>
    /// What to write for an export, following ExportMetadata, ExportMetadataAuthor and ExportMetadataCopyright
    /// </summary>
    /// <param name="captureDetails">the details of the capture, null for an image without (e.g. the clipboard)</param>
    public static ExportImageMetadata ForExport(ICaptureDetails captureDetails)
    {
        if (!IniConfigRegistry.TryGetSection<ICoreConfiguration>(out var coreConfiguration))
        {
            return ForExport(captureDetails, null, null, null);
        }
        return ForExport(captureDetails, coreConfiguration.ExportMetadata, coreConfiguration.ExportMetadataAuthor, coreConfiguration.ExportMetadataCopyright);
    }

    /// <summary>
    /// What to write for an export with the given fields
    /// </summary>
    /// <param name="captureDetails">the details of the capture, null for an image without (e.g. the clipboard)</param>
    /// <param name="fields">the optional details to write, null for none</param>
    /// <param name="author">the author, used with <see cref="ExportMetadataField.Author"/></param>
    /// <param name="copyright">the copyright notice, used with <see cref="ExportMetadataField.Author"/></param>
    public static ExportImageMetadata ForExport(ICaptureDetails captureDetails, ICollection<ExportMetadataField> fields, string author, string copyright)
    {
        var metadata = new ExportImageMetadata
        {
            Software = "Greenshot " + EnvironmentInfo.GetGreenshotVersion(true)
        };
        if (fields == null)
        {
            return metadata;
        }
        if (fields.Contains(ExportMetadataField.CaptureTime))
        {
            metadata.CaptureTime = CaptureTimeOf(captureDetails);
        }
        if (fields.Contains(ExportMetadataField.Author))
        {
            metadata.Author = NullIfEmpty(author);
            metadata.Copyright = NullIfEmpty(copyright);
        }
        return metadata;
    }

    /// <summary>
    /// The time the screen was captured: an image opened from a file or the clipboard wasn't captured by Greenshot,
    /// the time it was opened would be wrong as capture time
    /// </summary>
    public static DateTimeOffset? CaptureTimeOf(ICaptureDetails captureDetails)
    {
        if (captureDetails == null || captureDetails.DateTime == default)
        {
            return null;
        }
        if (captureDetails.CaptureMode is CaptureMode.None or CaptureMode.Clipboard or CaptureMode.File or CaptureMode.Import)
        {
            return null;
        }
        return new DateTimeOffset(captureDetails.DateTime);
    }

    private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Set the EXIF tags of the details on the profile
    /// </summary>
    public void ApplyTo(ExifProfile exifProfile)
    {
        exifProfile.SetValue(ExifTag.Software, Software);
        if (CaptureTime.HasValue)
        {
            exifProfile.SetValue(ExifTag.DateTimeOriginal, ExifCaptureTime);
            exifProfile.SetValue(ExifTag.OffsetTimeOriginal, ExifCaptureTimeOffset);
        }
        if (Author != null)
        {
            exifProfile.SetValue(ExifTag.Artist, Author);
        }
        if (Copyright != null)
        {
            exifProfile.SetValue(ExifTag.Copyright, Copyright);
        }
    }

    /// <summary>
    /// Write the details into the metadata of an ImageSharp image: EXIF, and XMP when there are details.
    /// Windows reads the details of a PNG only from XMP, other programs (e.g. Directory Opus) only from EXIF, so both are written.
    /// </summary>
    public void ApplyTo(SixLabors.ImageSharp.Metadata.ImageMetadata imageMetadata)
    {
        imageMetadata.ExifProfile ??= new ExifProfile();
        ApplyTo(imageMetadata.ExifProfile);
        if (HasDetails)
        {
            imageMetadata.XmpProfile = new XmpProfile(ToXmpPacket());
        }
    }

    /// <summary>
    /// The details as XMP packet (UTF-8)
    /// </summary>
    public byte[] ToXmpPacket()
    {
        var description = new XElement(Rdf + "Description",
            new XAttribute(Rdf + "about", string.Empty),
            new XAttribute(XNamespace.Xmlns + "xmp", Xmp.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "dc", Dc.NamespaceName),
            new XElement(Xmp + "CreatorTool", Software));
        if (CaptureTime.HasValue)
        {
            description.Add(new XElement(Xmp + "CreateDate", CaptureTime.Value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)));
        }
        if (Author != null)
        {
            description.Add(new XElement(Dc + "creator", new XElement(Rdf + "Seq", new XElement(Rdf + "li", Author))));
        }
        if (Copyright != null)
        {
            description.Add(new XElement(Dc + "rights", new XElement(Rdf + "Alt",
                new XElement(Rdf + "li", new XAttribute(XNamespace.Xml + "lang", "x-default"), Copyright))));
        }
        var xmpMeta = new XElement(X + "xmpmeta",
            new XAttribute(XNamespace.Xmlns + "x", X.NamespaceName),
            new XElement(Rdf + "RDF", new XAttribute(XNamespace.Xmlns + "rdf", Rdf.NamespaceName), description));
        // The packet wrapper lets other programs update the XMP in place, the begin attribute is the byte order mark
        string packet = "<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>"
                        + xmpMeta.ToString(SaveOptions.DisableFormatting)
                        + "<?xpacket end=\"w\"?>";
        return new UTF8Encoding(false).GetBytes(packet);
    }
}
