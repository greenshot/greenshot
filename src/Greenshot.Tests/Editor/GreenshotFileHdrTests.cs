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

using System.Drawing;
using System.IO;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormatHandlers;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// A .greenshot file keeps the HDR content of the capture: it is HDR again when the file is opened. Older files (and files without HDR) open as before.
    /// </summary>
    public class GreenshotFileHdrTests
    {
        public GreenshotFileHdrTests()
        {
            TestEnvironment.EnsureInitialized();
            if (SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true) == null)
            {
                SimpleServiceProvider.Current.AddService<IHdrImageEncoder>(new WicHdrImageEncoder());
            }
            if (SimpleServiceProvider.Current.GetInstance<IHdrImageDecoder>(true) == null)
            {
                SimpleServiceProvider.Current.AddService<IHdrImageDecoder>(new WicHdrImageDecoder());
            }
        }

        /// <summary>
        /// A capture of 16x8 pixels: white in the 8-bit image, HDR highlights of 5.0 (400 nits at an SDR white of 240 nits)
        /// </summary>
        private static Surface CreateSurface(bool withHdr)
        {
            var bitmap = new Bitmap(16, 8, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
            }
            var capture = new Capture(bitmap);
            if (withHdr)
            {
                var hdrImage = new HdrImage(16, 8, 240, 400) { IsRequested = true };
                hdrImage.Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < 8; y++)
                    {
                        accessor.GetRowSpan(y).Fill(new HalfVector4(5, 5, 5, 1));
                    }
                });
                hdrImage.BelongTo(capture.Image);
                capture.HdrImage = hdrImage;
            }
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
        }

        private static ISurface SaveAndOpen(Surface surface)
        {
            var handler = new GreenshotFileFormatHandler();
            using var stream = new MemoryStream();
            Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, stream, ".greenshot", surface));
            stream.Position = 0;
            return WinFormsContextGuard.CreateWithoutContext(() => handler.LoadSurface(stream));
        }

        [Fact]
        public void HdrCapture_WithAnEditAndAnElement_IsHdrAgainWhenOpened()
        {
            using var surface = CreateSurface(true);
            Assert.True(surface.ApplyCrop(new NativeRect(2, 0, 12, 8)));
            surface.AddElement(new RectangleContainer(surface) { Left = 1, Top = 1, Width = 4, Height = 4 }, false, false);

            using var opened = SaveAndOpen(surface);

            Assert.Equal(HdrState.Available, opened.HdrState);
            Assert.True(opened.IsHdrRequested);
            Assert.Equal(12, opened.HdrImage.Width);
            Assert.Equal(5f, opened.HdrImage.Pixels[10, 6].ToVector4().X, 2);
            // The elements are in the file as they were, not in the HDR image
            Assert.Single(opened.Elements.OfType<RectangleContainer>());
            Assert.Equal(5f, opened.HdrImage.Pixels[2, 2].ToVector4().X, 2);
        }

        [Fact]
        public void CaptureWithoutHdr_OpensAsBefore()
        {
            using var surface = CreateSurface(false);
            surface.AddElement(new RectangleContainer(surface) { Left = 1, Top = 1, Width = 4, Height = 4 }, false, false);

            using var opened = SaveAndOpen(surface);

            Assert.Equal(HdrState.None, opened.HdrState);
            Assert.Single(opened.Elements.OfType<RectangleContainer>());
            Assert.Equal(new Size(16, 8), opened.Image.Size);
        }
    }
}
