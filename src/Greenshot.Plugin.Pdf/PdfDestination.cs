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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Plugin.Pdf;

/// <summary>
/// Represents a destination for exporting captures as PDF documents.
/// </summary>
public sealed class PdfDestination : DestinationBase
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(PdfDestination));
    private readonly IPdfConfiguration _configuration;

    public PdfDestination(IPdfConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public override string Designation => "Pdf";

    public override DestinationDescriptor Descriptor => new DestinationDescriptor(
        Language.GetString("pdf", "destination"), 0, DestinationIcons.Resource("Save.Image"));

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        string suggestedPath;
        try
        {
            suggestedPath = CreateFilename(request.Metadata);
        }
        catch (Exception exception)
        {
            Log.Error("Could not create PDF filename.", exception);
            return ExportResult.Failed(exception.Message, exception);
        }

        bool showSaveDialog = _configuration.ShowSaveDialog;
        string selectedPath = suggestedPath;
        if (showSaveDialog)
        {
            try
            {
                selectedPath = await request.Ui.PickSaveFileAsync(new SaveFileRequest(request.Metadata, suggestedPath, PdfFileFormatHandler.FormatId), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Log.Error("Could not show the PDF save dialog.", exception);
                return ExportResult.Failed(exception.Message, exception);
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return ExportResult.Declined;
            }
        }

        string pdfPath = Path.ChangeExtension(selectedPath, ".pdf");
        var outputSettings = new PdfSurfaceOutputSettings(_configuration);

        try
        {
            string savedPath = await ExportFiles.SaveAsync(request.Source, pdfPath, showSaveDialog || CoreConfiguration.OutputFileAllowOverwrite, outputSettings, cancellationToken).ConfigureAwait(false);
            if (request.Metadata != null)
            {
                request.Metadata.Filename = savedPath;
            }

            return ExportResult.Succeeded(filePath: savedPath);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Log.Error("Could not save capture as PDF.", exception);
            return ExportResult.Failed(exception.Message, exception);
        }
    }

    private static string CreateFilename(ICaptureDetails captureDetails)
    {
        string pattern = CoreConfiguration.OutputFileFilenamePattern;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            pattern = "greenshot ${capturetime}";
        }

        string filename = FilenameHelper.GetFilenameFromPattern(pattern, PdfFileFormatHandler.FormatId, captureDetails);
        CoreConfiguration.ValidateAndCorrectOutputFilePath();
        string directory = FilenameHelper.FillVariables(CoreConfiguration.OutputFilePath, false);
        return Path.Combine(directory, Path.ChangeExtension(filename, ".pdf"));
    }
}

/// <summary>
/// Represents the output settings for saving a surface as a PDF document.
/// Holds pdf specific settings to write the PDF document.
/// Setting the encoding cache key ensures that the PDF is always re-encoded, even if the surface has been encoded before.
/// </summary>
internal sealed class PdfSurfaceOutputSettings : SurfaceOutputSettings
{
    public PdfSurfaceOutputSettings(IPdfConfiguration configuration)
        : base(PdfFileFormatHandler.FormatId)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        EncodingCacheKey = Guid.NewGuid().ToString("N");
    }

    public IPdfConfiguration Configuration { get; }
}
