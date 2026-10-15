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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Greenshot.Ipc.BrowserExtension
{
    /// <summary>
    /// Decodes a base64 capture-list document and stitches its PNG/JPEG tiles into one bitmap.
    /// Coordinates and document dimensions are in CSS pixels; DevicePixelRatio determines output pixels per CSS pixel.
    /// </summary>
    public static class CaptureListDecoder
    {
        public const string MimeType = "application/vnd.greenshot.capturelist";
        public const int MaxPayloadCharacters = 20 * 1024 * 1024;
        private const int MaxTiles = 4096;
        private const double MaxDevicePixelRatio = 8.0;

        private sealed class CaptureTile
        {
            public Bitmap Image { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double ViewportWidth { get; set; }
            public double ViewportHeight { get; set; }
            public List<CaptureOverlay> Overlays { get; set; }
        }

        private sealed class CaptureOverlay
        {
            public string Edge { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

        /// <summary>
        /// Validates, decodes and stitches a capture-list payload. The returned bitmap is owned by the caller.
        /// </summary>
        public static bool TryDecode(string base64Payload, out Bitmap bitmap, out string error)
        {
            bitmap = null;
            if (string.IsNullOrWhiteSpace(base64Payload))
            {
                error = "the capture-list payload is missing";
                return false;
            }
            if (base64Payload.Length > MaxPayloadCharacters)
            {
                error = "the capture-list payload exceeds the 20 MiB limit";
                return false;
            }

            byte[] jsonBytes;
            try
            {
                jsonBytes = Convert.FromBase64String(base64Payload);
            }
            catch (FormatException)
            {
                error = "the capture-list payload is not valid base64";
                return false;
            }

            JObject captureList;
            try
            {
                string json = new UTF8Encoding(false, true).GetString(jsonBytes);
                using (var stringReader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(stringReader) { MaxDepth = 32 })
                {
                    captureList = JObject.Load(jsonReader);
                    if (jsonReader.Read())
                    {
                        error = "the capture-list contains data after its JSON document";
                        return false;
                    }
                }
            }
            catch (Exception ex) when (ex is DecoderFallbackException || ex is JsonException || ex is ArgumentException || ex is InvalidOperationException)
            {
                error = "the capture-list payload is not valid UTF-8 JSON";
                return false;
            }

            JToken versionToken = captureList["version"];
            if (versionToken?.Type != JTokenType.Integer || !string.Equals(versionToken.ToString(), "1", StringComparison.Ordinal))
            {
                error = "the capture-list version is missing or unsupported";
                return false;
            }
            JToken coordinateUnitToken = captureList["coordinateUnit"];
            if (coordinateUnitToken?.Type != JTokenType.String || !string.Equals(coordinateUnitToken.Value<string>(), "css-pixels", StringComparison.Ordinal))
            {
                error = "the capture-list coordinate unit must be css-pixels";
                return false;
            }

            if (!TryGetPositiveNumber(captureList, "devicePixelRatio", out double devicePixelRatio) || devicePixelRatio > MaxDevicePixelRatio ||
                !TryGetPositiveNumber(captureList, "documentWidth", out double documentWidth) ||
                !TryGetPositiveNumber(captureList, "documentHeight", out double documentHeight))
            {
                error = "the capture-list has invalid document dimensions or device pixel ratio";
                return false;
            }

            bool overlapEnabled = false;
            JToken overlapEnabledToken = captureList["overlapEnabled"];
            if (overlapEnabledToken != null)
            {
                if (overlapEnabledToken.Type != JTokenType.Boolean)
                {
                    error = "the capture-list overlapEnabled field must be a boolean";
                    return false;
                }
                overlapEnabled = overlapEnabledToken.Value<bool>();
            }

            double overlapX = 0;
            double overlapY = 0;
            if (overlapEnabled &&
                (!TryGetNonNegativeNumber(captureList, "overlapX", out overlapX) ||
                 !TryGetNonNegativeNumber(captureList, "overlapY", out overlapY)))
            {
                error = "the capture-list overlap dimensions are invalid";
                return false;
            }

            int outputWidth;
            int outputHeight;
            if (!TryGetOutputSize(documentWidth, documentHeight, devicePixelRatio, out outputWidth, out outputHeight, out error))
            {
                return false;
            }

            if (!(captureList["screenshots"] is JArray screenshotArray) || screenshotArray.Count == 0 || screenshotArray.Count > MaxTiles)
            {
                error = $"the capture-list must contain between 1 and {MaxTiles} screenshots";
                return false;
            }

            var tiles = new List<CaptureTile>(screenshotArray.Count);
            Bitmap composedBitmap = null;
            try
            {
                for (int index = 0; index < screenshotArray.Count; index++)
                {
                    if (!(screenshotArray[index] is JObject screenshot) ||
                        !TryGetNonNegativeNumber(screenshot, "x", out double x) ||
                        !TryGetNonNegativeNumber(screenshot, "y", out double y) ||
                        !TryGetPositiveNumber(screenshot, "viewportWidth", out double viewportWidth) ||
                        !TryGetPositiveNumber(screenshot, "viewportHeight", out double viewportHeight) ||
                        x >= documentWidth || y >= documentHeight ||
                        (overlapEnabled && (overlapX > viewportWidth || overlapY > viewportHeight)))
                    {
                        error = $"screenshot {index + 1} has invalid coordinates or viewport dimensions";
                        return false;
                    }

                    JToken imagePayloadToken = screenshot["imageBase64"];
                    string imagePayload = imagePayloadToken?.Type == JTokenType.String ? imagePayloadToken.Value<string>() : null;
                    if (!ImportCaptureDecoder.TryDecode(imagePayload, out Bitmap image, out string imageError))
                    {
                        error = $"screenshot {index + 1} is invalid: {imageError}";
                        return false;
                    }

                    tiles.Add(new CaptureTile
                    {
                        Image = image,
                        X = x,
                        Y = y,
                        ViewportWidth = viewportWidth,
                        ViewportHeight = viewportHeight,
                        Overlays = ParseOverlays(screenshot, viewportWidth, viewportHeight, out error)
                    });
                    if (tiles[tiles.Count - 1].Overlays == null)
                    {
                        image.Dispose();
                        tiles.RemoveAt(tiles.Count - 1);
                        return false;
                    }
                }

                composedBitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(composedBitmap))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;

                    for (int index = 0; index < tiles.Count; index++)
                    {
                        CaptureTile tile = tiles[index];
                        int left = (int)Math.Round(tile.X * devicePixelRatio);
                        int top = (int)Math.Round(tile.Y * devicePixelRatio);
                        int right = (int)Math.Round((tile.X + tile.ViewportWidth) * devicePixelRatio);
                        int bottom = (int)Math.Round((tile.Y + tile.ViewportHeight) * devicePixelRatio);
                        var destination = Rectangle.FromLTRB(left, top, Math.Max(left + 1, right), Math.Max(top + 1, bottom));

                        using (var drawRegion = new Region(destination))
                        {
                            if (overlapEnabled && index > 0)
                            {
                                foreach (CaptureOverlay overlay in tile.Overlays)
                                {
                                    bool repeatedTopOverlay = string.Equals(overlay.Edge, "top", StringComparison.Ordinal) &&
                                        tile.Y > 0 && overlay.Y >= 0 && overlay.Y + overlay.Height <= overlapY;
                                    bool repeatedLeftOverlay = string.Equals(overlay.Edge, "left", StringComparison.Ordinal) &&
                                        tile.X > 0 && overlay.X >= 0 && overlay.X + overlay.Width <= overlapX;
                                    if (!repeatedTopOverlay && !repeatedLeftOverlay)
                                    {
                                        continue;
                                    }

                                    int overlayLeft = (int)Math.Round((tile.X + overlay.X) * devicePixelRatio);
                                    int overlayTop = (int)Math.Round((tile.Y + overlay.Y) * devicePixelRatio);
                                    int overlayRight = (int)Math.Round((tile.X + overlay.X + overlay.Width) * devicePixelRatio);
                                    int overlayBottom = (int)Math.Round((tile.Y + overlay.Y + overlay.Height) * devicePixelRatio);
                                    drawRegion.Exclude(Rectangle.FromLTRB(overlayLeft, overlayTop, overlayRight, overlayBottom));
                                }
                            }

                            graphics.SetClip(drawRegion, CombineMode.Replace);
                            graphics.DrawImage(tile.Image, destination);
                            graphics.ResetClip();
                        }
                    }
                }

                bitmap = composedBitmap;
                composedBitmap = null;
                error = null;
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ExternalException || ex is OutOfMemoryException)
            {
                bitmap?.Dispose();
                bitmap = null;
                error = "the capture-list could not be stitched into an image";
                return false;
            }
            finally
            {
                composedBitmap?.Dispose();
                foreach (CaptureTile tile in tiles)
                {
                    tile.Image.Dispose();
                }
            }
        }

        private static bool TryGetOutputSize(double documentWidth, double documentHeight, double devicePixelRatio, out int width, out int height, out string error)
        {
            width = 0;
            height = 0;
            double pixelWidth = Math.Ceiling(documentWidth * devicePixelRatio);
            double pixelHeight = Math.Ceiling(documentHeight * devicePixelRatio);
            if (double.IsNaN(pixelWidth) || double.IsInfinity(pixelWidth) || double.IsNaN(pixelHeight) || double.IsInfinity(pixelHeight) ||
                pixelWidth > int.MaxValue || pixelHeight > int.MaxValue)
            {
                error = "the stitched image dimensions are too large";
                return false;
            }

            width = (int)pixelWidth;
            height = (int)pixelHeight;
            if (!ImportCaptureDecoder.IsAcceptableSize(width, height, out error))
            {
                return false;
            }
            return true;
        }

        private static bool TryGetPositiveNumber(JObject obj, string propertyName, out double value)
        {
            return TryGetNumber(obj, propertyName, out value) && value > 0;
        }

        private static bool TryGetNonNegativeNumber(JObject obj, string propertyName, out double value)
        {
            return TryGetNumber(obj, propertyName, out value) && value >= 0;
        }

        private static bool TryGetNumber(JObject obj, string propertyName, out double value)
        {
            value = 0;
            JToken token = obj[propertyName];
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
            {
                return false;
            }

            try
            {
                value = token.Value<double>();
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                return false;
            }
        }

        private static List<CaptureOverlay> ParseOverlays(JObject screenshot, double viewportWidth, double viewportHeight, out string error)
        {
            var overlays = new List<CaptureOverlay>();
            JToken overlayToken = screenshot["overlays"];
            if (overlayToken == null)
            {
                error = null;
                return overlays;
            }
            if (!(overlayToken is JArray overlayArray) || overlayArray.Count > 1024)
            {
                error = "the screenshot overlay list is invalid";
                return null;
            }

            foreach (JToken item in overlayArray)
            {
                if (!(item is JObject overlay))
                {
                    error = "the screenshot contains an invalid overlay rectangle";
                    return null;
                }

                JToken edgeToken = overlay["edge"];
                string edge = edgeToken?.Type == JTokenType.String ? edgeToken.Value<string>() : null;
                if ((edge != "top" && edge != "left") ||
                    !TryGetNumber(overlay, "x", out double x) ||
                    !TryGetNumber(overlay, "y", out double y) ||
                    !TryGetPositiveNumber(overlay, "width", out double width) ||
                    !TryGetPositiveNumber(overlay, "height", out double height) ||
                    x < -viewportWidth || y < -viewportHeight || x > viewportWidth || y > viewportHeight)
                {
                    error = "the screenshot contains an invalid overlay rectangle";
                    return null;
                }

                overlays.Add(new CaptureOverlay
                {
                    Edge = overlay.Value<string>("edge"),
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                });
            }

            error = null;
            return overlays;
        }
    }
}
