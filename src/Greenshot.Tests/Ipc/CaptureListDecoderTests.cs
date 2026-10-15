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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Greenshot.Ipc.BrowserExtension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    public class CaptureListDecoderTests
    {
        [Fact]
        public void TryDecode_TwoTiles_StitchesAtDevicePixelRatio()
        {
            JObject captureList = CreateCaptureList(4, 2, 2, CreateTile(0, Color.Red), CreateTile(2, Color.Blue));

            Assert.True(CaptureListDecoder.TryDecode(Encode(captureList), out Bitmap bitmap, out string error), error);
            using (bitmap)
            {
                Assert.Equal(8, bitmap.Width);
                Assert.Equal(4, bitmap.Height);
                Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
                Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(6, 1).ToArgb());
            }
        }

        [Fact]
        public void TryDecode_Overlap_DoesNotDrawRepeatedStickyHeader()
        {
            JObject captureList = CreateCaptureList(2, 3, 1,
                new JObject
                {
                    ["x"] = 0,
                    ["y"] = 0,
                    ["viewportWidth"] = 2,
                    ["viewportHeight"] = 2,
                    ["imageBase64"] = CreateSolidImage(2, 2, Color.Red)
                },
                new JObject
                {
                    ["x"] = 0,
                    ["y"] = 1,
                    ["viewportWidth"] = 2,
                    ["viewportHeight"] = 2,
                    ["imageBase64"] = CreateSolidImage(2, 2, Color.Blue),
                    ["overlays"] = new JArray
                    {
                        new JObject { ["edge"] = "top", ["x"] = 0, ["y"] = 0, ["width"] = 2, ["height"] = 1 }
                    }
                });
            captureList["overlapEnabled"] = true;
            captureList["overlapX"] = 0;
            captureList["overlapY"] = 1;

            Assert.True(CaptureListDecoder.TryDecode(Encode(captureList), out Bitmap bitmap, out string error), error);
            using (bitmap)
            {
                Assert.Equal(2, bitmap.Width);
                Assert.Equal(3, bitmap.Height);
                Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(0, 1).ToArgb());
                Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(0, 2).ToArgb());
            }
        }

        [Fact]
        public void TryDecode_RejectsPayloadOver20MiB()
        {
            string payload = new string('A', CaptureListDecoder.MaxPayloadCharacters + 1);

            Assert.False(CaptureListDecoder.TryDecode(payload, out Bitmap bitmap, out string error));
            Assert.Null(bitmap);
            Assert.Contains("20 MiB", error);
        }

        [Fact]
        public void TryDecode_RejectsUnsupportedCoordinateUnit()
        {
            JObject captureList = CreateCaptureList(2, 2, 1, CreateTile(0, Color.Red));
            captureList["coordinateUnit"] = "device-pixels";

            Assert.False(CaptureListDecoder.TryDecode(Encode(captureList), out Bitmap bitmap, out string error));
            Assert.Null(bitmap);
            Assert.Contains("css-pixels", error);
        }

        private static JObject CreateCaptureList(double documentWidth, double documentHeight, double devicePixelRatio, params JObject[] tiles)
        {
            return new JObject
            {
                ["version"] = 1,
                ["coordinateUnit"] = "css-pixels",
                ["devicePixelRatio"] = devicePixelRatio,
                ["documentWidth"] = documentWidth,
                ["documentHeight"] = documentHeight,
                ["viewportWidth"] = 2,
                ["viewportHeight"] = 2,
                ["screenshots"] = new JArray(tiles)
            };
        }

        private static JObject CreateTile(double x, Color color)
        {
            string imageBase64 = CreateSolidImage(4, 4, color);
            return new JObject
            {
                ["x"] = x,
                ["y"] = 0,
                ["viewportWidth"] = 2,
                ["viewportHeight"] = 2,
                ["imageFormat"] = "png",
                ["imageBase64"] = imageBase64
            };
        }

        private static string CreateSolidImage(int width, int height, Color color)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var stream = new MemoryStream())
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(color);
                }
                bitmap.Save(stream, ImageFormat.Png);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private static string Encode(JObject captureList)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(captureList.ToString(Formatting.None)));
        }
    }
}
