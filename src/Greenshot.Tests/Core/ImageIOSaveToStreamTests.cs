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
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces.Plugin;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class ImageIOSaveToStreamTests
    {
        [Fact]
        public void SaveToStream_WhenNoHandlerSupportsFormat_ReportsFailure()
        {
            TestEnvironment.EnsureInitialized();

            string formatId = "test-unsupported-" + Guid.NewGuid().ToString("N");
            string extension = formatId;
            var saveableFormatDefinition = new FileFormatDefinition(
                            formatId,
                            Array.Empty<string>(),
                            new[] { extension },
                            extension,
                            "application/x-" + formatId,
                            Array.Empty<string>(),
                            formatId);
            SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>().Register(saveableFormatDefinition);

            using (var image = new Bitmap(1, 1))
            using (var stream = new MemoryStream())
            {
                Assert.Throws<InvalidOperationException>(() => ImageIO.SaveToStream(
                    image,
                    null,
                    stream,
                    new SurfaceOutputSettings(formatId)));
            }
        }

        [Fact]
        public void SaveToStream_WhenFormatIsLoadOnly_ReportsFailure()
        {
            TestEnvironment.EnsureInitialized();

            string formatId = "test-load-only-" + Guid.NewGuid().ToString("N");
            string extension = formatId;
            var onlyLoadableFormatDefinition = new FileFormatDefinition(
                formatId,
                new[] { extension },
                Array.Empty<string>(),
                extension,
                "application/x-" + formatId,
                Array.Empty<string>(),
                formatId);

            SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>().Register(onlyLoadableFormatDefinition);

            using (var image = new Bitmap(1, 1))
            using (var stream = new MemoryStream())
            {
                Assert.Throws<InvalidOperationException>(() => ImageIO.SaveToStream(
                    image,
                    null,
                    stream,
                    new SurfaceOutputSettings(formatId)));
            }
        }
    }
}