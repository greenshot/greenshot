// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Windows.Desktop;
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Core;
using Greenshot.Core;
using Greenshot.Core.Enums;
using Greenshot.Core.Extensions;
using Greenshot.Core.Sources;
using Greenshot.Core.Templates;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// Test the capture code
    /// </summary>
    public class CaptureTests
    {
        /// <summary>
        /// Test if a capture with the screen works
        /// </summary>
        [Fact]
        public async Task Test_CaptureFlow_ScreenSource()
        {
            var captureFlow = new CaptureFlow<BitmapSource>
            {
                Sources = {new ScreenSource()}
            };
            var capture = await captureFlow.Execute();
            Assert.NotNull(capture);
            Assert.NotNull(capture.CaptureElements);
            Assert.Equal(1, capture.CaptureElements.Count);
        }

        /// <summary>
        /// Test if a capture with the screen and mouse works
        /// </summary>
        [Fact]
        public async Task Test_CaptureFlow_ScreenSource_MouseSource()
        {
            var captureFlow = new CaptureFlow<BitmapSource>
            {
                Sources = { new ScreenSource() , new MouseSource()}
            };
            var capture = await captureFlow.Execute();
            Assert.NotNull(capture);
            Assert.NotNull(capture.CaptureElements);
            Assert.Equal(2, capture.CaptureElements.Count);
        }

        /// <summary>
        /// Test if multiple captures with GdiScreenCapture work
        /// </summary>
        [Fact]
        public void Test_GdiScreenCapture()
        {
            using (var gdiScreenCapture = new GdiScreenCapture())
            {
                gdiScreenCapture.CaptureFrame();
                using (var bitmap = gdiScreenCapture.CurrentFrameAsBitmap())
                {
                    Assert.True(bitmap.Width > 0);

/*
                    // Write the capture to a file, for analysis
                    using (var stream = new FileStream(Path.Combine(Path.GetTempPath(), "test.png"), FileMode.Create, FileAccess.Write))
                    {
                        ImageOutput.SaveToStream(bitmap, null, stream, new SurfaceOutputSettings(null, OutputFormats.png));
                    }
*/
                }

                var bitmapSource = gdiScreenCapture.CurrentFrameAsBitmapSource();
                Assert.True(bitmapSource.Width > 0);

                gdiScreenCapture.CaptureFrame();
            }
        }

        /// <summary>
        /// Test if a capture from a window works
        /// </summary>
        [WpfFact]
        public async Task Test_CaptureFlow_DwmWindowSource()
        {
            ICoreConfiguration config = new CoreConfigurationImpl();

            var windowToCapture = InteropWindowQuery.GetTopLevelWindows().First(window => window.GetCaption().Contains("Notepad"));
            var bounds = windowToCapture.GetInfo().Bounds;
            var captureFlow = new CaptureFlow<BitmapSource>
            {
                Sources = { new DwmWindowSource(config, () => windowToCapture) }
            };
            var capture = await captureFlow.Execute();
            Assert.NotNull(capture);
            Assert.NotNull(capture.CaptureElements);

            var template = new SimpleTemplate();
            var bitmapSource = template.Apply(capture).ToBitmapSource();
            Assert.Equal(bounds.Size, bitmapSource.Size());
            using (var outputStream = bitmapSource.ToStream(OutputFormats.png))
            using (var fileStream = File.Create("Test_CaptureFlow_DwmWindowSource.png"))
            {
                outputStream.Seek(0, SeekOrigin.Begin);
                await outputStream.CopyToAsync(fileStream);
            }
        }
        
        
        /// <summary>
        /// Test if capturing works
        /// </summary>
        [Fact]
        public void Test_BitmapCapture()
        {
            using (var screenBitmapCapture = new BitmapScreenCapture())
            {
                screenBitmapCapture.CaptureFrame();
                
                Assert.NotNull(screenBitmapCapture.CurrentFrameAsBitmap());
                
                var testFile1 = Path.Combine(Path.GetTempPath(), @"test-bitmap.png");
                screenBitmapCapture.CurrentFrameAsBitmap().NativeBitmap.Save(testFile1, ImageFormat.Png);
                
                var testFile2 = Path.Combine(Path.GetTempPath(), @"test-bitmapsource.png");
                using (var fileStream = new FileStream(testFile2, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder
                    {
                        Interlace = PngInterlaceOption.Off
                    };
                    encoder.Frames.Add(BitmapFrame.Create(screenBitmapCapture.CurrentFrameAsBitmap().NativeBitmapSource));
                    encoder.Save(fileStream);
                }
            }
        }
    }
}
