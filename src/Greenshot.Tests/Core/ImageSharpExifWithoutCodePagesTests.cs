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
using System.IO;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Core.FileFormat;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Greenshot doesn't ship System.Text.Encoding.CodePages (see src/Directory.Build.props): ImageSharp only needs it for EXIF texts in the JIS encoding.
    /// These tests run without it, like Greenshot.
    /// </summary>
    public class ImageSharpExifWithoutCodePagesTests
    {
        private static readonly DateTimeOffset Captured = new DateTimeOffset(2026, 11, 2, 16, 30, 5, TimeSpan.FromHours(1));
        private static readonly byte[] AsciiCode = Encoding.ASCII.GetBytes("ASCII\0\0\0");
        private static readonly byte[] JisCode = Encoding.ASCII.GetBytes("JIS\0\0\0\0\0");

        public ImageSharpExifWithoutCodePagesTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// The build leaves the assembly out, also for the tests: else the other tests here prove nothing
        /// </summary>
        [Fact]
        public void CodePages_IsNotInTheOutput()
        {
            Assert.False(File.Exists(Path.Combine(AppContext.BaseDirectory, "System.Text.Encoding.CodePages.dll")));
        }

        /// <summary>
        /// A UserComment in ASCII (the usual one) doesn't need the code pages: the details are read
        /// </summary>
        [Fact]
        public void Jpeg_AsciiUserComment_DetailsAreRead()
        {
            var opened = new CaptureDetails { CaptureMode = CaptureMode.File };
            using (var stream = new MemoryStream(CreateJpeg()))
            {
                ImageMetadataReader.ReadInto(opened, stream);
            }

            Assert.Equal(Captured, ExportImageMetadata.CaptureTimeOf(opened));
        }

        /// <summary>
        /// A UserComment in JIS needs the code pages: reading the details of the file fails inside ImageSharp, which the reader catches
        /// (the image itself is loaded by WIC, the details are left out)
        /// </summary>
        [Fact]
        public void Jpeg_JisUserComment_ReadingTheDetailsDoesNotThrow()
        {
            byte[] jpeg = CreateJpeg();
            int index = IndexOf(jpeg, AsciiCode);
            Assert.True(index >= 0, "The UserComment wasn't found in the JPEG");
            Buffer.BlockCopy(JisCode, 0, jpeg, index, JisCode.Length);

            string fileName = Path.Combine(Path.GetTempPath(), $"greenshot-jis-exif-{Guid.NewGuid():N}.jpg");
            try
            {
                File.WriteAllBytes(fileName, jpeg);
                var opened = new CaptureDetails { CaptureMode = CaptureMode.File };
                var exception = Record.Exception(() => ImageMetadataReader.ReadInto(opened, fileName));
                Assert.Null(exception);
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        private static byte[] CreateJpeg()
        {
            using var image = new Image<Rgb24>(8, 8);
            image.Metadata.ExifProfile = new ExifProfile();
            image.Metadata.ExifProfile.SetValue(ExifTag.DateTimeOriginal, "2026:11:02 16:30:05");
            image.Metadata.ExifProfile.SetValue(ExifTag.OffsetTimeOriginal, "+01:00");
            image.Metadata.ExifProfile.SetValue(ExifTag.UserComment, new EncodedString(EncodedString.CharacterCode.ASCII, "Greenshot"));
            using var stream = new MemoryStream();
            image.SaveAsJpeg(stream);
            return stream.ToArray();
        }

        private static int IndexOf(byte[] data, byte[] pattern)
        {
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                int j = 0;
                while (j < pattern.Length && data[i + j] == pattern[j])
                {
                    j++;
                }
                if (j == pattern.Length)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
