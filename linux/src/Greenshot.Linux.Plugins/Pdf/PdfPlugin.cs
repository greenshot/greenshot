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

using System.Globalization;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Plugins;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;

namespace Greenshot.Linux.Plugins.Pdf;

/// <summary>
/// The [Pdf] section of greenshot.ini, with the keys of the Windows PDF plugin
/// </summary>
public sealed class PdfSettings(CoreSettings settings)
{
    public const string SectionName = "Pdf";

    public static readonly IReadOnlyList<string> PageSizes = ["Image", "Custom", "A3", "A4", "A5", "A6", "Letter"];

    /// <summary>
    /// Image (the page has the size of the capture), Custom, A3, A4, A5, A6 or Letter
    /// </summary>
    public string PageSize
    {
        get => PageSizes.FirstOrDefault(p => p.Equals(settings.GetValue(SectionName, nameof(PageSize)), StringComparison.OrdinalIgnoreCase)) ?? "Image";
        set => settings.SetValue(SectionName, nameof(PageSize), value);
    }

    public double PageWidthMm { get => Get(nameof(PageWidthMm), 210); set => Set(nameof(PageWidthMm), value); }

    public double PageHeightMm { get => Get(nameof(PageHeightMm), 297); set => Set(nameof(PageHeightMm), value); }

    public double MarginTopMm { get => Get(nameof(MarginTopMm), 0); set => Set(nameof(MarginTopMm), value); }

    public double MarginBottomMm { get => Get(nameof(MarginBottomMm), 0); set => Set(nameof(MarginBottomMm), value); }

    public double MarginLeftMm { get => Get(nameof(MarginLeftMm), 0); set => Set(nameof(MarginLeftMm), value); }

    public double MarginRightMm { get => Get(nameof(MarginRightMm), 0); set => Set(nameof(MarginRightMm), value); }

    /// <summary>
    /// OnlyShrinkToFit or FitToPage, for a fixed page size
    /// </summary>
    public bool FitToPage
    {
        get => string.Equals(settings.GetValue(SectionName, "ScalingMode"), "FitToPage", StringComparison.OrdinalIgnoreCase);
        set => settings.SetValue(SectionName, "ScalingMode", value ? "FitToPage" : "OnlyShrinkToFit");
    }

    /// <summary>
    /// The page in mm for a fixed size, null for Image
    /// </summary>
    public (double Width, double Height)? PageSizeMm => Options.PageSizeMm;

    /// <summary>
    /// The page as it is set now; a PDF step of a recipe changes some of it
    /// </summary>
    public PdfPageOptions Options => new(PageSize, PageWidthMm, PageHeightMm, MarginTopMm, MarginBottomMm, MarginLeftMm, MarginRightMm, FitToPage);

    private double Get(string key, double fallback) =>
        double.TryParse(settings.GetValue(SectionName, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : fallback;

    private void Set(string key, double value) => settings.SetValue(SectionName, key, value.ToString(CultureInfo.InvariantCulture));
}

/// <summary>
/// The page of a PDF: its size (Image, Custom, A3, A4, A5, A6, Letter), the margins in mm and whether a smaller capture is enlarged
/// </summary>
public sealed record PdfPageOptions(string PageSize, double PageWidthMm, double PageHeightMm, double MarginTopMm, double MarginBottomMm,
    double MarginLeftMm, double MarginRightMm, bool FitToPage)
{
    /// <summary>
    /// The page in mm for a fixed size, null for Image
    /// </summary>
    public (double Width, double Height)? PageSizeMm => PageSize switch
    {
        "A3" => (297, 420),
        "A4" => (210, 297),
        "A5" => (148, 210),
        "A6" => (105, 148),
        "Letter" => (215.9, 279.4),
        "Custom" => (PageWidthMm, PageHeightMm),
        _ => null,
    };
}

/// <summary>
/// Writes the capture as PDF like the PdfDocumentWriter of Windows: for the page size Image the page is the capture
/// at 96 dpi plus the margins, a fixed page gets the capture centered, shrunk (or also enlarged with FitToPage) to fit
/// </summary>
public static class PdfWriter
{
    private const float PointsPerMm = 72f / 25.4f;
    private const float PointsPerPixel = 72f / 96f;

    public static byte[] Create(SKBitmap image, PdfPageOptions settings, string title)
    {
        var (page, imageRect) = Layout(image.Width, image.Height, settings);
        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata { Title = title, Creator = "Greenshot", Producer = "Greenshot" }))
        {
            var canvas = document.BeginPage(page.Width, page.Height);
            using var skImage = SKImage.FromBitmap(image);
            canvas.DrawImage(skImage, imageRect, new SKSamplingOptions(SKCubicResampler.Mitchell));
            document.EndPage();
            document.Close();
        }

        return stream.ToArray();
    }

    internal static (SKSize Page, SKRect Image) Layout(int width, int height, PdfSettings settings) => Layout(width, height, settings.Options);

    internal static (SKSize Page, SKRect Image) Layout(int width, int height, PdfPageOptions settings)
    {
        float left = (float)settings.MarginLeftMm * PointsPerMm, right = (float)settings.MarginRightMm * PointsPerMm;
        float top = (float)settings.MarginTopMm * PointsPerMm, bottom = (float)settings.MarginBottomMm * PointsPerMm;
        float imageWidth = width * PointsPerPixel, imageHeight = height * PointsPerPixel;
        if (settings.PageSizeMm is not { } size)
        {
            return (new SKSize(imageWidth + left + right, imageHeight + top + bottom), SKRect.Create(left, top, imageWidth, imageHeight));
        }

        var page = new SKSize((float)size.Width * PointsPerMm, (float)size.Height * PointsPerMm);
        float areaWidth = Math.Max(1, page.Width - left - right), areaHeight = Math.Max(1, page.Height - top - bottom);
        float scale = Math.Min(areaWidth / imageWidth, areaHeight / imageHeight);
        if (scale < 1 || settings.FitToPage)
        {
            imageWidth *= scale;
            imageHeight *= scale;
        }

        return (page, SKRect.Create(left + (areaWidth - imageWidth) / 2, top + (areaHeight - imageHeight) / 2, imageWidth, imageHeight));
    }
}

/// <summary>
/// "Save as PDF": asks where, like the Windows destination does by default. A PDF step of a recipe can change the page
/// and save to the output folder without asking.
/// </summary>
public sealed class PdfDestination(CoreSettings settings, PdfSettings pdf, Func<string, Task<string?>> askPath, PdfPageOptions? page = null, bool ask = true) : IDestination
{
    public string Designation => "Pdf";

    public string DisplayName => Texts.Get("Pdf", "destination");

    public int Priority => 7;

    public bool IsAvailable => true;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var suggested = Path.ChangeExtension(FileDestination.GetDefaultPath(settings, capture.Details), ".pdf");
        var path = ask ? await askPath(suggested).ConfigureAwait(true) : suggested;
        if (path == null)
        {
            return ExportResult.Aborted;
        }

        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            path += ".pdf";
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, PdfWriter.Create(capture.Image, page ?? pdf.Options, capture.Details.Title ?? "Greenshot"), cancellationToken).ConfigureAwait(false);
            Log.Info($"Saved the capture as PDF to {path}");
            return ExportResult.Done(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Saving the PDF {path} failed", ex);
            return ExportResult.Failed(Texts.Core("error_nowriteaccess", path));
        }
    }
}

public sealed class PdfPlugin(CoreSettings settings, Func<string, Task<string?>> askPath) : IGreenshotPlugin, IRecipeStepProvider
{
    public void RegisterSteps(IStepRegistry registry) => registry.Register<Recipes.PdfStep>(config => new Recipes.PdfStep(config, this));

    /// <summary>
    /// The destination of a PDF step: the page of the step, asking where or not
    /// </summary>
    internal PdfDestination CreateDestination(PdfPageOptions page, bool ask) => new(settings, Settings, askPath, page, ask);

    public PdfSettings Settings { get; } = new(settings);

    public string Name => "Pdf";

    public string DisplayName => Texts.Linux("plugin_pdf");

    public IReadOnlyList<IDestination> Destinations => [new PdfDestination(settings, Settings, askPath)];
}
