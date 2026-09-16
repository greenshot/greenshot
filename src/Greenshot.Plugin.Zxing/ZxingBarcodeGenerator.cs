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
using ZXing;
using ZXing.Common;

namespace Greenshot.Plugin.Zxing
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

            return RenderMatrix(matrix, fore, back, roundedDots, format, requestedWidth, requestedHeight, margin);
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

            var bmp = new Bitmap(targetWidth, targetHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(back);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var brush = new SolidBrush(fore))
                {
                    for (int y = 0; y < matrixHeight; y++)
                    {
                        for (int x = 0; x < matrixWidth; x++)
                        {
                            if (matrix[x, y])
                            {
                                float px = x * scaleX;
                                float py = y * scaleY;

                                if (rounded && is2D)
                                {
                                    bool isFinder = false;
                                    if (format == BarcodeFormat.QR_CODE)
                                    {
                                        if (x >= margin && x < margin + 7 && y >= margin && y < margin + 7) isFinder = true;
                                        else if (x >= matrixWidth - margin - 7 && x < matrixWidth - margin && y >= margin && y < margin + 7) isFinder = true;
                                        else if (x >= margin && x < margin + 7 && y >= matrixHeight - margin - 7 && y < matrixHeight - margin) isFinder = true;
                                    }

                                    if (isFinder)
                                    {
                                        g.FillRectangle(brush, px, py, scaleX, scaleY);
                                    }
                                    else
                                    {
                                        float sizeX = scaleX - 0.5f;
                                        float sizeY = scaleY - 0.5f;
                                        g.FillEllipse(brush, px + 0.25f, py + 0.25f, Math.Max(1f, sizeX), Math.Max(1f, sizeY));
                                    }
                                }
                                else
                                {
                                    g.FillRectangle(brush, px, py, scaleX, scaleY);
                                }
                            }
                        }
                    }
                }
            }

            return bmp;
        }

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
