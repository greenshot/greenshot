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
using System.Drawing.Imaging;
using System.IO;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class TiffLoadingTests
    {
        public TiffLoadingTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(".tif")]
        [InlineData(".tiff")]
        public void WpfFileFormatHandler_LoadsTiff_SavedByImageSharp(string extension)
        {
            // ImageSharp doesn't load TIFF (#1472), WIC has to, even when its built-in decoders aren't listed in the registry
            var wpfHandler = new WpfFileFormatHandler();
            Assert.True(wpfHandler.Supports(FileFormatHandlerActions.LoadFromStream, extension));

            using var bitmap = new Bitmap(8, 8, PixelFormat.Format24bppRgb);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(x * 30, y * 30, 77));
                }
            }

            using var stream = new MemoryStream();
            Assert.True(new ImageSharpFileFormatHandler().TrySaveToStream(bitmap, stream, extension, null, new SurfaceOutputSettings(WellKnownFileFormats.Png)));
            stream.Position = 0;

            Assert.True(wpfHandler.TryLoadFromStream(stream, extension, out var loaded));
            using (loaded)
            {
                Assert.Equal(bitmap.Size, loaded.Size);
                Assert.Equal(bitmap.GetPixel(5, 3).ToArgb(), loaded.GetPixel(5, 3).ToArgb());
            }
        }
    }
}
