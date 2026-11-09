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
using System.Drawing;
using ZXing;
using ZXing.Common;

namespace Greenshot.Plugin.Zxing.Processing
{
    /// <summary>
    /// Utility class for generating barcode and QR code bitmaps using ZXing.
    /// </summary>
    public static class ZxingBarcodeGenerator
    {
        /// <summary>
        /// Generates a Bitmap containing the encoded barcode or QR code.
        /// </summary>
        /// <param name="payload">Text or payload to encode.</param>
        /// <param name="format">Barcode format (default QR_CODE).</param>
        /// <param name="foreColor">Foreground (module) color (default Black).</param>
        /// <param name="backColor">Background color (default White).</param>
        /// <param name="roundedDots">Whether to render modern circular dots for 2D barcodes.</param>
        /// <param name="requestedWidth">Optional target width in pixels.</param>
        /// <param name="requestedHeight">Optional target height in pixels.</param>
        /// <param name="margin">Quiet zone margin (default 4).</param>
        /// <returns>Rendered Bitmap, or null if payload is empty or encoding fails.</returns>
        public static Bitmap Generate(
            string payload,
            BarcodeFormat format = BarcodeFormat.QR_CODE,
            Color? foreColor = null,
            Color? backColor = null,
            bool roundedDots = false,
            int? requestedWidth = null,
            int? requestedHeight = null,
            int margin = 4) =>
            Render(CreateLayout(payload, format, foreColor, backColor, roundedDots, requestedWidth, requestedHeight, margin));

        /// <summary>
        /// The layout of the encoded barcode or QR code, see <see cref="Generate"/>: what is drawn where, without drawing it
        /// </summary>
        /// <returns>The layout, or null if payload is empty</returns>
        public static BarcodeLayout CreateLayout(
            string payload,
            BarcodeFormat format = BarcodeFormat.QR_CODE,
            Color? foreColor = null,
            Color? backColor = null,
            bool roundedDots = false,
            int? requestedWidth = null,
            int? requestedHeight = null,
            int margin = 4)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }

            Color fore = foreColor ?? Color.Black;
            Color back = backColor ?? Color.White;

            var hints = new Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.CHARACTER_SET, "UTF-8" },
                { EncodeHintType.MARGIN, margin }
            };

            var writer = new MultiFormatWriter();
            var matrix = writer.encode(payload, format, 0, 0, hints);

            return Layout(matrix, fore, back, roundedDots, format, requestedWidth, requestedHeight, margin);
        }

        /// <summary>
        /// Renders a BitMatrix into a GDI+ Bitmap.
        /// </summary>
        public static Bitmap RenderMatrix(
            BitMatrix matrix,
            Color fore,
            Color back,
            bool rounded,
            BarcodeFormat format,
            int? requestedWidth = null,
            int? requestedHeight = null,
            int margin = 4) =>
            Render(Layout(matrix, fore, back, rounded, format, requestedWidth, requestedHeight, margin));

        /// <summary>
        /// The layout of a BitMatrix: the size of the barcode and each module (a square, or a dot for rounded 2D codes; the finder patterns of a
        /// QR code stay squares)
        /// </summary>
        public static BarcodeLayout Layout(
            BitMatrix matrix,
            Color fore,
            Color back,
            bool rounded,
            BarcodeFormat format,
            int? requestedWidth = null,
            int? requestedHeight = null,
            int margin = 4)
        {
            if (matrix == null) return null;

            int matrixWidth = matrix.Width;
            int matrixHeight = matrix.Height;

            bool is2D = (format == BarcodeFormat.QR_CODE ||
                         format == BarcodeFormat.AZTEC ||
                         format == BarcodeFormat.DATA_MATRIX ||
                         format == BarcodeFormat.PDF_417);

            int targetWidth = requestedWidth.HasValue && requestedWidth.Value > 0
                ? requestedWidth.Value
                : (is2D ? 200 : 350);

            int targetHeight = requestedHeight.HasValue && requestedHeight.Value > 0
                ? requestedHeight.Value
                : (is2D ? 200 : 100);

            float scaleX = (float)targetWidth / matrixWidth;
            float scaleY = (float)targetHeight / matrixHeight;

            if (is2D)
            {
                float minScale = Math.Min(scaleX, scaleY);
                scaleX = minScale;
                scaleY = minScale;
                targetWidth = Math.Max(1, (int)Math.Round(matrixWidth * scaleX));
                targetHeight = Math.Max(1, (int)Math.Round(matrixHeight * scaleY));
            }

            var modules = new List<BarcodeModule>();
            for (int y = 0; y < matrixHeight; y++)
            {
                for (int x = 0; x < matrixWidth; x++)
                {
                    if (!matrix[x, y])
                    {
                        continue;
                    }
                    float px = x * scaleX;
                    float py = y * scaleY;

                    bool isFinder = false;
                    if (rounded && is2D && format == BarcodeFormat.QR_CODE)
                    {
                        if (x >= margin && x < margin + 7 && y >= margin && y < margin + 7) isFinder = true;
                        else if (x >= matrixWidth - margin - 7 && x < matrixWidth - margin && y >= margin && y < margin + 7) isFinder = true;
                        else if (x >= margin && x < margin + 7 && y >= matrixHeight - margin - 7 && y < matrixHeight - margin) isFinder = true;
                    }

                    if (rounded && is2D && !isFinder)
                    {
                        float sizeX = scaleX - 0.5f;
                        float sizeY = scaleY - 0.5f;
                        modules.Add(new BarcodeModule(new RectangleF(px + 0.25f, py + 0.25f, Math.Max(1f, sizeX), Math.Max(1f, sizeY)), true));
                    }
                    else
                    {
                        modules.Add(new BarcodeModule(new RectangleF(px, py, scaleX, scaleY), false));
                    }
                }
            }

            return new BarcodeLayout(targetWidth, targetHeight, fore, back, modules);
        }

        /// <summary>
        /// Renders the layout into a GDI+ Bitmap of its size, e.g. for the editor
        /// </summary>
        /// <returns>The bitmap, null without a layout</returns>
        public static Bitmap Render(BarcodeLayout layout)
        {
            if (layout == null) return null;

            var bmp = new Bitmap(layout.Width, layout.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(layout.Back);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var brush = new SolidBrush(layout.Fore))
                {
                    foreach (var module in layout.Modules)
                    {
                        if (module.Round)
                        {
                            g.FillEllipse(brush, module.Area);
                        }
                        else
                        {
                            g.FillRectangle(brush, module.Area);
                        }
                    }
                }
            }

            return bmp;
        }

        /// <summary>
        /// Draws the layout with ImageSharp into a place of the image, e.g. on the HDR pixels of an HDR file (see IHdrDrawable): the background,
        /// then each module, like <see cref="Render"/> draws them with GDI+, scaled to the place
        /// </summary>
        /// <param name="image">The image to draw on</param>
        /// <param name="values">The values of the image (for an HDR image linear light)</param>
        /// <param name="layout">The layout</param>
        /// <param name="place">Where the barcode goes on the image</param>
        public static void Draw(SixLabors.ImageSharp.Image image, Greenshot.Base.ImageSharpDrawing.ImageValues values, BarcodeLayout layout, SixLabors.ImageSharp.Rectangle place)
        {
            if (image == null || values == null || layout == null || place.Width <= 0 || place.Height <= 0)
            {
                return;
            }
            float scaleX = (float)place.Width / layout.Width;
            float scaleY = (float)place.Height / layout.Height;
            var back = values.ColorOf(new SixLabors.ImageSharp.PixelFormats.Rgba32(layout.Back.R, layout.Back.G, layout.Back.B, layout.Back.A));
            var fore = values.ColorOf(new SixLabors.ImageSharp.PixelFormats.Rgba32(layout.Fore.R, layout.Fore.G, layout.Fore.B, layout.Fore.A));
            SixLabors.ImageSharp.Processing.ProcessingExtensions.Mutate(image, context =>
            {
                SixLabors.ImageSharp.Drawing.Processing.FillPathExtensions.Fill(context, back,
                    new SixLabors.ImageSharp.Drawing.RectangularPolygon(place.X, place.Y, place.Width, place.Height));
                foreach (var module in layout.Modules)
                {
                    float x = place.X + module.Area.X * scaleX;
                    float y = place.Y + module.Area.Y * scaleY;
                    float width = module.Area.Width * scaleX;
                    float height = module.Area.Height * scaleY;
                    SixLabors.ImageSharp.Drawing.IPath shape = module.Round
                        ? new SixLabors.ImageSharp.Drawing.EllipsePolygon(x + width / 2, y + height / 2, width, height)
                        : new SixLabors.ImageSharp.Drawing.RectangularPolygon(x, y, width, height);
                    // Like the editor (GDI+ PixelOffsetMode.None, pixel middles on whole numbers): the edges of the modules are anti-aliased the
                    // same way as in its bitmap
                    SixLabors.ImageSharp.Drawing.Processing.FillPathExtensions.Fill(context, fore, Greenshot.Base.ImageSharpDrawing.Painting.OnPixelCenters(shape));
                }
            });
        }

        /// <summary>
        /// The barcode format of the index in the format list of the barcode view (<see cref="Drawing.ZxingModel.FormatIndex"/>)
        /// </summary>
        public static BarcodeFormat MapFormatIndex(int index)
        {
            switch (index)
            {
                case 1: return BarcodeFormat.AZTEC;
                case 2: return BarcodeFormat.DATA_MATRIX;
                case 3: return BarcodeFormat.PDF_417;
                case 4: return BarcodeFormat.CODE_128;
                case 5: return BarcodeFormat.CODE_39;
                case 6: return BarcodeFormat.CODE_93;
                case 7: return BarcodeFormat.EAN_13;
                case 8: return BarcodeFormat.EAN_8;
                case 9: return BarcodeFormat.UPC_A;
                case 10: return BarcodeFormat.UPC_E;
                case 11: return BarcodeFormat.CODABAR;
                case 12: return BarcodeFormat.ITF;
                case 13: return BarcodeFormat.MSI;
                case 14: return BarcodeFormat.PLESSEY;
                default: return BarcodeFormat.QR_CODE;
            }
        }

        /// <summary>
        /// The index in the format list of the barcode view for the barcode format, see <see cref="MapFormatIndex"/>
        /// </summary>
        public static int MapFormatToIndex(BarcodeFormat format)
        {
            switch (format)
            {
                case BarcodeFormat.AZTEC: return 1;
                case BarcodeFormat.DATA_MATRIX: return 2;
                case BarcodeFormat.PDF_417: return 3;
                case BarcodeFormat.CODE_128: return 4;
                case BarcodeFormat.CODE_39: return 5;
                case BarcodeFormat.CODE_93: return 6;
                case BarcodeFormat.EAN_13: return 7;
                case BarcodeFormat.EAN_8: return 8;
                case BarcodeFormat.UPC_A: return 9;
                case BarcodeFormat.UPC_E: return 10;
                case BarcodeFormat.CODABAR: return 11;
                case BarcodeFormat.ITF: return 12;
                case BarcodeFormat.MSI: return 13;
                case BarcodeFormat.PLESSEY: return 14;
                default: return 0;
            }
        }

        /// <summary>
        /// A 2D code (square, may have rounded dots), else a 1D barcode
        /// </summary>
        public static bool Is2D(BarcodeFormat format)
            => format == BarcodeFormat.QR_CODE || format == BarcodeFormat.AZTEC || format == BarcodeFormat.DATA_MATRIX || format == BarcodeFormat.PDF_417;

        /// <summary>
        /// Maps a format name string to a BarcodeFormat enum value.
        /// </summary>
        public static BarcodeFormat MapBarcodeFormat(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BarcodeFormat.QR_CODE;

            string normalized = name.Trim().Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant();

            switch (normalized)
            {
                case "AZTEC":
                case "AZTEC2D":
                    return BarcodeFormat.AZTEC;
                case "DATAMATRIX":
                case "DATAMATRIX2D":
                    return BarcodeFormat.DATA_MATRIX;
                case "PDF417":
                case "PDF4172D":
                    return BarcodeFormat.PDF_417;
                case "CODE128":
                case "CODE1281D":
                    return BarcodeFormat.CODE_128;
                case "CODE39":
                case "CODE391D":
                    return BarcodeFormat.CODE_39;
                case "CODE93":
                case "CODE931D":
                    return BarcodeFormat.CODE_93;
                case "EAN13":
                case "EAN131D":
                    return BarcodeFormat.EAN_13;
                case "EAN8":
                case "EAN81D":
                    return BarcodeFormat.EAN_8;
                case "UPCA":
                case "UPCA1D":
                    return BarcodeFormat.UPC_A;
                case "UPCE":
                case "UPCE1D":
                    return BarcodeFormat.UPC_E;
                case "CODABAR":
                case "CODABAR1D":
                    return BarcodeFormat.CODABAR;
                case "ITF":
                case "ITF1D":
                    return BarcodeFormat.ITF;
                case "MSI":
                case "MSI1D":
                    return BarcodeFormat.MSI;
                case "PLESSEY":
                case "PLESSEY1D":
                    return BarcodeFormat.PLESSEY;
                default:
                    return BarcodeFormat.QR_CODE;
            }
        }
    }
}
