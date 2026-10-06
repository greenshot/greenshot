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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.Pdf.Configuration;
using Greenshot.Plugin.Pdf.Destinations;
using log4net;

namespace Greenshot.Plugin.Pdf.Recipes;

/// <summary>
/// Capture recipe step that saves the capture as a PDF.
/// </summary>
[StepInfo("Pdf", "Save as PDF", "Saves the capture as a PDF file.", "Export")]
[StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
[StepParameter("PageSize", ContractDataType.Enum, Description = "PDF page size (default: PDF settings)", AllowedValues = new[] { PdfPageSizes.Image, PdfPageSizes.Custom, PdfPageSizes.A3, PdfPageSizes.A4, PdfPageSizes.A5, PdfPageSizes.A6, PdfPageSizes.Letter })]
[StepParameter("MeasurementUnit", ContractDataType.Enum, Description = "Measurement unit displayed in PDF settings (dimensions and margins are in mm)", AllowedValues = new[] { PdfMeasurementUnits.Mm, PdfMeasurementUnits.Cm, PdfMeasurementUnits.Inch })]
[StepParameter("PageWidthMm", ContractDataType.Decimal, Description = "Custom page width in millimeters (default: PDF settings)")]
[StepParameter("PageHeightMm", ContractDataType.Decimal, Description = "Custom page height in millimeters (default: PDF settings)")]
[StepParameter("MarginTopMm", ContractDataType.Decimal, Description = "Top margin in millimeters (default: PDF settings)")]
[StepParameter("MarginBottomMm", ContractDataType.Decimal, Description = "Bottom margin in millimeters (default: PDF settings)")]
[StepParameter("MarginLeftMm", ContractDataType.Decimal, Description = "Left margin in millimeters (default: PDF settings)")]
[StepParameter("MarginRightMm", ContractDataType.Decimal, Description = "Right margin in millimeters (default: PDF settings)")]
[StepParameter("ScalingMode", ContractDataType.Enum, Description = "How to scale the image on a fixed page (default: PDF settings)", AllowedValues = new[] { PdfScalingModes.OnlyShrinkToFit, PdfScalingModes.FitToPage })]
[StepParameter("ShowSaveDialog", ContractDataType.Boolean, DefaultValue = true, Description = "Show the PDF save dialog")]
[StepOutputVariable("Pdf.FilePath", ContractDataType.FilePath, "Path of the saved PDF file", Conditional = true)]
public sealed class PdfStep : ICaptureStep
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(PdfStep));
    private readonly IPdfConfiguration _configuration;

    public string Name { get; }
    public RecipeNodeConfig NodeConfig { get; }

    public PdfStep(RecipeNodeConfig config, IPdfConfiguration configuration)
    {
        NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Name = config.Name ?? "PdfExportStep";
    }

    public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        if (context.Payload?.EnsureSurface() == null)
        {
            context.LogStep("PdfStep: No surface available to export.");
            Log.Warn("PdfStep: Surface is null in context payload.");
            return;
        }

        var captureDetails = context.Payload.RawCapture?.CaptureDetails ?? new CaptureDetails();
        var configuration = new PdfConfigurationImpl
        {
            PageSize = NodeConfig.GetParameter("PageSize", _configuration.PageSize),
            MeasurementUnit = NodeConfig.GetParameter("MeasurementUnit", _configuration.MeasurementUnit),
            PageWidthMm = NodeConfig.GetParameter("PageWidthMm", _configuration.PageWidthMm),
            PageHeightMm = NodeConfig.GetParameter("PageHeightMm", _configuration.PageHeightMm),
            MarginTopMm = NodeConfig.GetParameter("MarginTopMm", _configuration.MarginTopMm),
            MarginBottomMm = NodeConfig.GetParameter("MarginBottomMm", _configuration.MarginBottomMm),
            MarginLeftMm = NodeConfig.GetParameter("MarginLeftMm", _configuration.MarginLeftMm),
            MarginRightMm = NodeConfig.GetParameter("MarginRightMm", _configuration.MarginRightMm),
            ScalingMode = NodeConfig.GetParameter("ScalingMode", _configuration.ScalingMode)
        };
        var destination = new PdfDestination(configuration, NodeConfig.GetParameter("ShowSaveDialog", true));
        var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
        var result = await DestinationExporter.ExportAsync(destination, source, captureDetails, false, context.UserInteraction, cancellationToken).ConfigureAwait(false);
        await ExportResultHandler.ApplyAsync(destination, result, source, cancellationToken).ConfigureAwait(false);

        switch (result.Status)
        {
            case ExportStatus.Succeeded:
                context.Properties["Pdf.FilePath"] = result.FilePath;
                context.LogStep($"Saved capture as PDF: {result.FilePath}");
                break;
            case ExportStatus.Failed:
                context.LogStep($"PdfStep: Could not save capture as PDF: {result.Error}");
                Log.ErrorFormat("PdfStep: Could not save capture as PDF: {0}", result.Error);
                break;
            default:
                context.LogStep("PdfStep: PDF export was declined.");
                break;
        }
    }
}
