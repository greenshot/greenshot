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
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.FileFormatHandlers;
using log4net;

namespace Greenshot.Plugin.Pdf;

/// <summary>
/// Handles the PDF file format for saving images as PDF documents.
/// </summary>
public sealed class PdfFileFormatHandler : AbstractFileFormatHandler
{
    public const string FormatId = "pdf";
    private static readonly ILog Log = LogManager.GetLogger(typeof(PdfFileFormatHandler));
    private static readonly IReadOnlyCollection<string> PdfExtensions = [".pdf"];
    private readonly IPdfConfiguration _configuration;

    public PdfFileFormatHandler(IPdfConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        SupportedExtensions[FileFormatHandlerActions.SaveToStream] = PdfExtensions;
        SupportedExtensions[FileFormatHandlerActions.SaveToFile] = PdfExtensions;
    }

    public override void RegisterFileFormats(IFileFormatRegistry registry)
    {
        RegisterFileFormat(registry, FormatId, [], PdfExtensions, "pdf", "application/pdf", null, "Portable Document Format");
    }

    public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
    {
        if (bitmap == null || destination == null || !string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            IPdfConfiguration configuration = (surfaceOutputSettings as PdfSurfaceOutputSettings)?.Configuration ?? _configuration;
            PdfDocumentWriter.Write([bitmap], destination, configuration, surface?.CaptureDetails);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error("Could not write PDF document.", exception);
            return false;
        }
    }

    public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
    {
        bitmap = null;
        return false;
    }
}
