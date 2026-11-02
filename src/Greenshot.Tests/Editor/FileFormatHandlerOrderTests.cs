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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.FileFormatHandlers;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// On Windows WIC loads the images, ImageSharp saves them and loads what WIC can't
    /// </summary>
    public class FileFormatHandlerOrderTests
    {
        public FileFormatHandlerOrderTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static IFileFormatHandler First(FileFormatHandlerActions action, string extension) =>
            SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>()
                .Where(ffh => ffh.Supports(action, extension))
                .OrderBy(ffh => ffh.PriorityFor(action, extension))
                .FirstOrDefault();

        [Theory]
        [InlineData(".png")]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".bmp")]
        [InlineData(".gif")]
        [InlineData(".tif")]
        [InlineData(".tiff")]
        public void Loading_GoesThroughWic(string extension)
        {
            foreach (var action in new[] { FileFormatHandlerActions.LoadFromStream, FileFormatHandlerActions.LoadFromFile, FileFormatHandlerActions.LoadDrawableFromStream })
            {
                Assert.IsType<WicFileFormatHandler>(First(action, extension));
            }
        }

        [Theory]
        [InlineData(".png")]
        [InlineData(".jpg")]
        [InlineData(".bmp")]
        [InlineData(".gif")]
        [InlineData(".tif")]
        [InlineData(".webp")]
        [InlineData(".tga")]
        public void Saving_StaysWithImageSharp(string extension)
        {
            Assert.IsType<ImageSharpFileFormatHandler>(First(FileFormatHandlerActions.SaveToStream, extension));
        }

        [Fact]
        public void SpecialFormats_KeepTheirHandlers()
        {
            Assert.IsType<IconFileFormatHandler>(First(FileFormatHandlerActions.LoadFromStream, ".ico"));
            Assert.IsType<DibFileFormatHandler>(First(FileFormatHandlerActions.LoadFromStream, ".dib"));
            Assert.IsType<WicFileFormatHandler>(First(FileFormatHandlerActions.SaveToStream, ".jxr"));
        }

        [Fact]
        public void Tga_IsLoadedByImageSharp()
        {
            Assert.IsType<ImageSharpFileFormatHandler>(First(FileFormatHandlerActions.LoadFromStream, ".tga"));

            using var stream = new MemoryStream();
            using (var image = new SixLabors.ImageSharp.Image<Rgba32>(7, 5, new Rgba32(10, 200, 30, 255)))
            {
                image.Save(stream, new TgaEncoder());
            }
            stream.Position = 0;

            var handlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            Assert.True(handlers.TryLoadFromStream(stream, ".tga", out var bitmap));
            using (bitmap)
            {
                Assert.Equal(7, bitmap.Width);
                Assert.Equal(Color.FromArgb(255, 10, 200, 30).ToArgb(), bitmap.GetPixel(3, 2).ToArgb());
            }
        }

        /// <summary>
        /// When the first handler can't read the image, the next one gets the stream from the start
        /// </summary>
        [Fact]
        public void Loading_FallsBackToTheNextHandler()
        {
            using var source = new Bitmap(9, 4, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.Teal);
            }
            using var stream = new MemoryStream();
            source.Save(stream, ImageFormat.Png);
            stream.Position = 0;

            var failing = new FailingHandler(".png");
            var handlers = new List<IFileFormatHandler> { new WicFileFormatHandler(), failing };

            Assert.True(handlers.TryLoadFromStream(stream, ".png", out var bitmap));
            using (bitmap)
            {
                Assert.Equal(9, bitmap.Width);
                Assert.Equal(Color.Teal.ToArgb(), bitmap.GetPixel(4, 2).ToArgb());
            }
            Assert.Equal(1, failing.Calls);

            stream.Position = 0;
            var drawables = handlers.LoadDrawablesFromStream(stream, ".png").ToList();
            Assert.Single(drawables);
            Assert.Equal(2, failing.Calls);
        }

        /// <summary>
        /// Comes first, reads a part of the stream and fails
        /// </summary>
        private sealed class FailingHandler : AbstractFileFormatHandler
        {
            public int Calls { get; private set; }

            public FailingHandler(string extension)
            {
                var extensions = new[] { extension };
                SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = extensions;
                SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = extensions;
            }

            public override int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension) => -1;

            public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null) => false;

            public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
            {
                Calls++;
                stream.Read(new byte[16], 0, 16);
                bitmap = null;
                return false;
            }
        }
    }
}
