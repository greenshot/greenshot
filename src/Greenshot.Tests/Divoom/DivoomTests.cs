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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text.Json;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.Divoom;
using Greenshot.Plugin.Divoom.Destinations;
using Greenshot.Plugin.Divoom.Device;
using Xunit;

namespace Greenshot.Tests.Divoom
{
    /// <summary>
    /// The protocol of the Divoom devices without a device: addresses, the answers of the server and of the device, the pictures
    /// </summary>
    public class DivoomTests
    {
        [Theory]
        [InlineData("192.0.2.10", "http://192.0.2.10/post")]
        [InlineData(" 192.0.2.10 ", "http://192.0.2.10/post")]
        [InlineData("http://192.0.2.10", "http://192.0.2.10/post")]
        [InlineData("pixoo.local:8080", "http://pixoo.local:8080/post")]
        public void GetCommandUri_OfAnAddress(string address, string expected)
        {
            Assert.Equal(expected, DivoomClient.GetCommandUri(address)?.AbsoluteUri);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://192.0.2.10")]
        [InlineData("ftp://192.0.2.10")]
        [InlineData("192.0.2.10/other")]
        [InlineData("user@192.0.2.10")]
        [InlineData("192.0.2.10?x=1")]
        public void GetCommandUri_NotTheAddressOfADevice_IsNull(string address)
        {
            Assert.Null(DivoomClient.GetCommandUri(address));
        }

        [Fact]
        public void ParseDevices_TheDevicesWithAnAddress()
        {
            const string answer = "{\"ReturnCode\":0,\"ReturnMessage\":\"\",\"DeviceList\":[" +
                                  "{\"DeviceName\":\"Desk\",\"DeviceId\":300000001,\"DevicePrivateIP\":\"192.0.2.10\",\"DeviceMac\":\"0200000000aa\",\"Hardware\":400}," +
                                  "{\"DeviceName\":\"\",\"DevicePrivateIP\":\"192.0.2.11\"}," +
                                  "{\"DeviceName\":\"No address\"}]}";

            var devices = DivoomClient.ParseDevices(answer);

            Assert.Equal(2, devices.Count);
            Assert.Equal("Desk", devices[0].Name);
            Assert.Equal("192.0.2.10", devices[0].Address);
            Assert.Equal("Desk (192.0.2.10)", devices[0].DisplayText);
            // Without a name the address is the name
            Assert.Equal("192.0.2.11", devices[1].DisplayText);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("{\"ReturnCode\":0,\"ReturnMessage\":\"\"}")]
        [InlineData("[]")]
        public void ParseDevices_NoList_NoDevices(string answer)
        {
            Assert.Empty(DivoomClient.ParseDevices(answer));
        }

        [Theory]
        [InlineData("{\"error_code\":0}")]
        [InlineData("{\"error_code\":\"0\",\"PicId\":5}")]
        public void ParseAnswer_ErrorCodeZero_IsTaken(string answer)
        {
            using var document = DivoomClient.ParseAnswer(answer);
            Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        }

        [Theory]
        [InlineData("{\"error_code\":1}")]
        [InlineData("{\"other\":0}")]
        [InlineData("<html></html>")]
        [InlineData("")]
        public void ParseAnswer_NotTaken_Throws(string answer)
        {
            Assert.Throws<DivoomException>(() => DivoomClient.ParseAnswer(answer));
        }

        [Fact]
        public void PictureCommand_OneFrameWithTheRgbAsBase64()
        {
            // 0xFB 0xEF 0xFF is "++//" in base64: written as it is, not as +
            var rgb = Enumerable.Repeat(new byte[] { 0xFB, 0xEF, 0xFF }, 16 * 16).SelectMany(bytes => bytes).ToArray();

            string json = DivoomClient.ToJson(DivoomClient.CreatePictureCommand(7, 16, rgb));

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            Assert.Equal("Draw/SendHttpGif", root.GetProperty("Command").GetString());
            Assert.Equal(1, root.GetProperty("PicNum").GetInt32());
            Assert.Equal(16, root.GetProperty("PicWidth").GetInt32());
            Assert.Equal(0, root.GetProperty("PicOffset").GetInt32());
            Assert.Equal(7, root.GetProperty("PicID").GetInt32());
            Assert.Equal(rgb, Convert.FromBase64String(root.GetProperty("PicData").GetString()));
            Assert.Contains("++//", json);
        }

        [Theory]
        [InlineData(64, 64, 64, 0, 0, 64, 64)]
        [InlineData(128, 64, 64, 0, 16, 64, 32)]
        [InlineData(10, 20, 16, 4, 0, 8, 16)]
        [InlineData(8, 8, 16, 0, 0, 16, 16)]
        public void FitInto_AsLargeAsFits_InTheMiddle(int width, int height, int size, int x, int y, int targetWidth, int targetHeight)
        {
            Assert.Equal(new Rectangle(x, y, targetWidth, targetHeight), DivoomImage.FitInto(width, height, size));
        }

        [Fact]
        public void ToRgb_OfTheSize_PixelForPixel_TransparentIsBlack()
        {
            using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(0, 255, 255, 255));
            }
            bitmap.SetPixel(0, 0, Color.Red);
            bitmap.SetPixel(15, 0, Color.Lime);
            bitmap.SetPixel(15, 15, Color.Blue);

            var rgb = DivoomImage.ToRgb(bitmap, 16);

            Assert.Equal(16 * 16 * 3, rgb.Length);
            Assert.Equal(new byte[] { 255, 0, 0 }, rgb.Take(3).ToArray());
            Assert.Equal(new byte[] { 0, 255, 0 }, rgb.Skip(15 * 3).Take(3).ToArray());
            Assert.Equal(new byte[] { 0, 0, 255 }, rgb.Skip((16 * 16 - 1) * 3).ToArray());
            // A transparent pixel is dark on the device
            Assert.Equal(new byte[] { 0, 0, 0 }, rgb.Skip(3).Take(3).ToArray());
        }

        [Fact]
        public void ToRgb_LargerImage_FitsIntoTheScreen()
        {
            using var bitmap = new Bitmap(64, 32, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
            }

            var rgb = DivoomImage.ToRgb(bitmap, 16);

            // 16 x 8 in the middle: rows 4 to 11 white, above and below black
            Assert.Equal(new byte[] { 0, 0, 0 }, rgb.Take(3).ToArray());
            Assert.Equal(new byte[] { 255, 255, 255 }, rgb.Skip((8 * 16 + 8) * 3).Take(3).ToArray());
            Assert.Equal(new byte[] { 0, 0, 0 }, rgb.Skip((15 * 16) * 3).Take(3).ToArray());
        }

        [Theory]
        [InlineData(16, 16)]
        [InlineData(32, 32)]
        [InlineData(64, 64)]
        [InlineData(0, 64)]
        [InlineData(128, 64)]
        public void NormalizeSize_OneOfTheScreens(int size, int expected)
        {
            Assert.Equal(expected, DivoomImage.NormalizeSize(size));
        }

        [Fact]
        public void Recipe_FixedSizeCaptureOfTheScreen_ToTheDevice()
        {
            var recipe = new DivoomPlugin().CreateRecipes().Single();

            var select = recipe.FindFirstNodeByType(WellKnownStepTypes.InteractiveSelection);
            Assert.Equal("${config.Divoom.PanelSize}x${config.Divoom.PanelSize}", select.GetParameter<string>("FixedCaptureArea"));
            var export = recipe.FindFirstNodeByType(WellKnownStepTypes.Destinations);
            Assert.Contains(DivoomDestination.DesignationName, export.GetParameter<System.Collections.Generic.List<string>>("DestinationDesignations"));
            Assert.Equal(CaptureSourceType.Region, recipe.FindFirstNodeByType(WellKnownStepTypes.Source).GetParameter("SourceType", CaptureSourceType.Clipboard));
        }
    }
}
