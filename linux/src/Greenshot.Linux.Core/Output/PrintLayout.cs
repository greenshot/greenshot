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

using SkiaSharp;

namespace Greenshot.Linux.Core.Output;

/// <summary>
/// The paper: size and margins in points (1/72 inch)
/// </summary>
public sealed record PageSetup(float Width, float Height, float MarginLeft, float MarginTop, float MarginRight, float MarginBottom)
{
    public const float PointsPerMillimeter = 72f / 25.4f;

    /// <summary>
    /// A4 with 10 mm margins, when the print dialog doesn't tell
    /// </summary>
    public static PageSetup A4 => FromMillimeters(210, 297, 10, 10, 10, 10);

    public static PageSetup FromMillimeters(double width, double height, double left, double top, double right, double bottom) => new(
        (float)(width * PointsPerMillimeter), (float)(height * PointsPerMillimeter),
        (float)(left * PointsPerMillimeter), (float)(top * PointsPerMillimeter),
        (float)(right * PointsPerMillimeter), (float)(bottom * PointsPerMillimeter));

    public SKRect PrintableArea => new(MarginLeft, MarginTop, Width - MarginRight, Height - MarginBottom);
}

/// <summary>
/// The print options of greenshot.ini (OutputPrint...)
/// </summary>
public sealed record PrintOptions(
    bool AllowShrink = true,
    bool AllowEnlarge = false,
    bool AllowRotate = false,
    bool Center = true,
    bool Inverted = false,
    bool Grayscale = false,
    bool Monochrome = false,
    string? Footer = null);

/// <summary>
/// Puts the capture on a page as PDF, with the layout rules of the PrintHelper of Windows:
/// one pixel is 1/100 inch, the image shrinks or grows to the printable area only when allowed,
/// it is turned when that fits better and allowed, and the footer is centered below it.
/// </summary>
public static class PrintLayout
{
    /// <summary>
    /// GDI+ prints in 1/100 inch, a pixel of the image is one of them
    /// </summary>
    private const float PointsPerPixel = 0.72f;
    private const float FooterFontSize = 10;

    public static byte[] CreatePdf(SKBitmap image, PageSetup page, PrintOptions options, string title)
    {
        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata { Title = title, Creator = "Greenshot", Producer = "Greenshot" }))
        {
            var canvas = document.BeginPage(page.Width, page.Height);
            Draw(canvas, image, page, options);
            document.EndPage();
            document.Close();
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Where the image goes on the page and whether it is turned; exposed for the tests
    /// </summary>
    internal static (SKRect Rect, bool Rotated) Arrange(int imageWidth, int imageHeight, SKRect area, PrintOptions options)
    {
        bool rotated = false;
        float width = imageWidth * PointsPerPixel;
        float height = imageHeight * PointsPerPixel;
        if (options.AllowRotate && (area.Width > area.Height && width < height || area.Width < area.Height && width > height))
        {
            (width, height) = (height, width);
            rotated = true;
        }

        if (options.AllowShrink || options.AllowEnlarge)
        {
            float scale = Math.Min(area.Width / width, area.Height / height);
            if (options.AllowShrink && scale < 1 || options.AllowEnlarge && scale > 1)
            {
                width *= scale;
                height *= scale;
            }
        }

        float left = options.Center ? area.Left + (area.Width - width) / 2 : area.Left;
        float top = options.Center ? area.Top + (area.Height - height) / 2 : area.Top;
        return (SKRect.Create(left, top, width, height), rotated);
    }

    private static void Draw(SKCanvas canvas, SKBitmap image, PageSetup page, PrintOptions options)
    {
        var area = page.PrintableArea;
        using var footerFont = new SKFont(SKTypeface.Default, FooterFontSize);
        float footerHeight = string.IsNullOrEmpty(options.Footer) ? 0 : footerFont.Spacing;
        area = new SKRect(area.Left, area.Top, area.Right, area.Bottom - footerHeight);

        var (rect, rotated) = Arrange(image.Width, image.Height, area, options);
        using var paint = new SKPaint { ColorFilter = ColorFilter(options) };
        using var skImage = SKImage.FromBitmap(image);
        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
        if (rotated)
        {
            // Turned counter clockwise like Rotate270FlipNone
            canvas.Save();
            canvas.Translate(rect.Left, rect.Bottom);
            canvas.RotateDegrees(-90);
            canvas.DrawImage(skImage, new SKRect(0, 0, rect.Height, rect.Width), sampling, paint);
            canvas.Restore();
        }
        else
        {
            canvas.DrawImage(skImage, rect, sampling, paint);
        }

        if (footerHeight > 0)
        {
            using var black = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            float y = area.Bottom - footerFont.Metrics.Ascent;
            canvas.DrawText(options.Footer!, page.Width / 2, y, SKTextAlign.Center, footerFont, black);
        }
    }

    private static SKColorFilter? ColorFilter(PrintOptions options)
    {
        SKColorFilter? filter = null;
        if (options.Grayscale || options.Monochrome)
        {
            filter = SKColorFilter.CreateColorMatrix(
            [
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0.299f, 0.587f, 0.114f, 0, 0,
                0, 0, 0, 1, 0,
            ]);
        }

        if (options.Monochrome)
        {
            // Black or white: a steep contrast curve around the middle gray
            var threshold = SKColorFilter.CreateColorMatrix(
            [
                255, 0, 0, 0, -127,
                0, 255, 0, 0, -127,
                0, 0, 255, 0, -127,
                0, 0, 0, 1, 0,
            ]);
            filter = filter == null ? threshold : SKColorFilter.CreateCompose(threshold, filter);
        }

        if (options.Inverted)
        {
            var invert = SKColorFilter.CreateColorMatrix(
            [
                -1, 0, 0, 0, 1,
                0, -1, 0, 0, 1,
                0, 0, -1, 0, 1,
                0, 0, 0, 1, 0,
            ]);
            filter = filter == null ? invert : SKColorFilter.CreateCompose(invert, filter);
        }

        return filter;
    }
}
