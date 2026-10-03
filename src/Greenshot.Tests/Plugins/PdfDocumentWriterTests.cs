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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Greenshot.Plugin.Pdf;
using Xunit;

namespace Greenshot.Tests.Plugins;

public class PdfDocumentWriterTests
{
    [Fact]
    public void Configuration_SaveDialogDefaultsToEnabledAndCanBeDisabled()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        var viewModel = new PdfConfigurationViewModel(configuration);

        Assert.True(viewModel.ShowSaveDialog);
        viewModel.ShowSaveDialog = false;
        Assert.False(configuration.ShowSaveDialog);
    }

    [Fact]
    public void Write_CreatesSinglePagePdfWithImageAndCaptureInformation()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        var captureDetails = new Greenshot.Base.Core.CaptureDetails
        {
            Title = "Screenshot title",
            Filename = "source-image.png"
        };

        using var bitmap = new Bitmap(96, 48);
        bitmap.SetResolution(96, 96);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, captureDetails, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.StartsWith("%PDF-1.4", pdf, StringComparison.Ordinal);
        Assert.Contains("/Count 1", pdf);
        Assert.Contains("/Subtype/Image/Width 96/Height 48", pdf);
        Assert.Contains("/Filter/FlateDecode", pdf);
        Assert.Contains("/Title <FEFF", pdf);
        Assert.Contains("/Filename <FEFF", pdf);
        Assert.Contains("/Creator <FEFF", pdf);
        Assert.Contains("/CreationDate (D:", pdf);
        Assert.EndsWith("%%EOF\n", pdf, StringComparison.Ordinal);

        int xrefOffset = int.Parse(ReadAfter(pdf, "startxref\n"), CultureInfo.InvariantCulture);
        Assert.StartsWith("xref\n", pdf.Substring(xrefOffset), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_UsesCaptureDateTimeAsPdfCreationDate()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        var captureDetails = new Greenshot.Base.Core.CaptureDetails
        {
            DateTime = new DateTime(2024, 3, 5, 14, 9, 7, DateTimeKind.Utc)
        };
        using var bitmap = new Bitmap(1, 1);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, captureDetails, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.Contains("/CreationDate (D:20240305140907Z)", pdf);
    }

    [Fact]
    public void Write_OmitsCreationDateWithoutCaptureDetails()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        using var bitmap = new Bitmap(1, 1);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.DoesNotContain("/CreationDate", pdf);
    }

    [Fact]
    public void Write_UsesImageDpiForPageDimensions()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        using var bitmap = new Bitmap(96, 48);
        bitmap.SetResolution(192, 192);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.Contains("/MediaBox[0 0 36 18]", pdf);
        Assert.Contains("q 36 0 0 18 0 0 cm /Img1 Do Q", pdf);
    }

    [Fact]
    public void Write_AutomaticPageAddsMarginsWithoutScalingImage()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        configuration.MarginLeftMm = 25.4;
        configuration.MarginRightMm = 25.4;
        configuration.MarginTopMm = 25.4;
        configuration.MarginBottomMm = 25.4;
        configuration.ScalingMode = "FitToPage";
        using var bitmap = new Bitmap(96, 48);
        bitmap.SetResolution(96, 96);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.Contains("/MediaBox[0 0 216 180]", pdf);
        Assert.Contains("q 72 0 0 36 72 72 cm /Img1 Do Q", pdf);
    }

    [Theory]
    [InlineData(PdfScalingModes.OnlyShrinkToFit, 72, 36, 324, 342)]
    [InlineData(PdfScalingModes.FitToPage, 576, 288, 72, 216)]
    public void Write_FixedPageCentersSmallImageAndOnlyEnlargesWhenAllowed(string scalingMode, double width, double height, double left, double bottom)
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        configuration.PageSize = "Custom";
        configuration.PageWidthMm = 254;
        configuration.PageHeightMm = 254;
        configuration.MarginLeftMm = 25.4;
        configuration.MarginRightMm = 25.4;
        configuration.MarginTopMm = 25.4;
        configuration.MarginBottomMm = 25.4;
        configuration.ScalingMode = scalingMode;
        using var bitmap = new Bitmap(96, 48);
        bitmap.SetResolution(96, 96);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.Contains($"q {PdfPageSizes.FormatNumber(width)} 0 0 {PdfPageSizes.FormatNumber(height)} {PdfPageSizes.FormatNumber(left)} {PdfPageSizes.FormatNumber(bottom)} cm /Img1 Do Q", pdf);
    }

    [Fact]
    public void Write_FixedPageShrinksLargeImageProportionallyWithinMargins()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        configuration.PageSize = "Custom";
        configuration.PageWidthMm = 25.4;
        configuration.PageHeightMm = 25.4;
        configuration.MarginLeftMm = 2.54;
        configuration.MarginRightMm = 2.54;
        configuration.MarginTopMm = 2.54;
        configuration.MarginBottomMm = 2.54;
        using var bitmap = new Bitmap(192, 96);
        bitmap.SetResolution(96, 96);
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0");

        string pdf = Encoding.GetEncoding(28591).GetString(output.ToArray());
        Assert.Contains("/MediaBox[0 0 72 72]", pdf);
        Assert.Contains("q 57.6 0 0 28.8 7.2 21.6 cm /Img1 Do Q", pdf);
    }

    [Fact]
    public void Write_EmbedsRgbPixelsWithoutLoss()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl();
        configuration.ResetToDefaults();
        using var bitmap = new Bitmap(2, 1);
        bitmap.SetPixel(0, 0, Color.FromArgb(255, 12, 34, 56));
        bitmap.SetPixel(1, 0, Color.FromArgb(255, 78, 90, 123));
        using var output = new MemoryStream();

        PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0");

        byte[] pixels = ReadImagePixels(output.ToArray());
        Assert.Equal(new byte[] { 12, 34, 56, 78, 90, 123 }, pixels);
    }

    [Fact]
    public void Write_RejectsMarginsThatLeaveNoPrintableArea()
    {
        TestEnvironment.EnsureInitialized();
        var configuration = new PdfConfigurationImpl
        {
            PageSize = "Custom",
            PageWidthMm = 10,
            PageHeightMm = 10,
            MarginLeftMm = 5,
            MarginRightMm = 5
        };
        using var bitmap = new Bitmap(10, 10);
        using var output = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => PdfDocumentWriter.Write(bitmap, output, configuration, null, "Created with Greenshot v1.4.0"));
    }

    private static string ReadAfter(string value, string marker)
    {
        int start = value.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = value.IndexOf('\n', start);
        return value.Substring(start, end - start);
    }

    private static byte[] ReadImagePixels(byte[] pdfBytes)
    {
        string pdf = Encoding.GetEncoding(28591).GetString(pdfBytes);
        int filterIndex = pdf.IndexOf("/Filter/FlateDecode", StringComparison.Ordinal);
        int lengthStart = pdf.IndexOf("/Length ", filterIndex, StringComparison.Ordinal) + 8;
        int lengthEnd = pdf.IndexOf(">>", lengthStart, StringComparison.Ordinal);
        int streamLength = int.Parse(pdf.Substring(lengthStart, lengthEnd - lengthStart), CultureInfo.InvariantCulture);
        int streamStart = pdf.IndexOf("stream\n", filterIndex, StringComparison.Ordinal) + 7;

        using (var compressed = new MemoryStream(pdfBytes, streamStart + 2, streamLength - 6))
        using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress))
        using (var pixels = new MemoryStream())
        {
            deflate.CopyTo(pixels);
            return pixels.ToArray();
        }
    }
}
