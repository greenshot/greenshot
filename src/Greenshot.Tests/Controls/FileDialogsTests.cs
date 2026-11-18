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

using Greenshot.Base.Controls;
using Greenshot.Base.Core.FileFormat;
using Xunit;

namespace Greenshot.Tests.Controls
{
    /// <summary>
    /// The default extension of the save dialog, from the filter texts of the language packs
    /// </summary>
    public class FileDialogsTests
    {
        [Theory]
        [InlineData("Greenshot templates (*.gst)|*.gst", "gst")]
        [InlineData("Greenshot Recipe (*.gsrecipe.json)|*.gsrecipe.json|JSON Recipe (*.json)|*.json|All files (*.*)|*.*", "gsrecipe.json")]
        [InlineData("Images| *.png ; *.jpg", "png")]
        [InlineData("All files (*.*)|*.*|ADMX (*.admx)|*.admx", "admx")]
        [InlineData("All files (*.*)|*.*", null)]
        [InlineData("Everything|*", null)]
        [InlineData("A report|report.txt", null)]
        [InlineData("JPEG|*.j?g", null)]
        [InlineData("Only a name", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void DefaultExtensionOf_IsTheFirstExtensionOfTheFileTypes(string filter, string expected)
        {
            Assert.Equal(expected, FileDialogs.DefaultExtensionOf(filter));
        }

        [Fact]
        public void OpenImageFilter_ListsAllFormatsTogetherThenEachFormatThenAllFiles()
        {
            var formats = new[]
            {
                new FileFormatDefinition("jpeg", new[] { "jpg", "jpeg" }, new[] { "jpg", "jpeg" }, "jpg", "image/jpeg", null, "JPEG image"),
                new FileFormatDefinition("png", new[] { "png" }, new[] { "png" }, "png", "image/png", null, "PNG image")
            };

            string filter = InteractiveUserInteraction.OpenImageFilter(formats);

            Assert.Equal("Image files (*.jpeg, *.jpg, *.png)|*.jpeg;*.jpg;*.png|JPEG image (*.jpg, *.jpeg)|*.jpg;*.jpeg|PNG image (*.png)|*.png|All files (*.*)|*.*", filter);
        }

        [Fact]
        public void OpenImageFilter_WithoutFormats_IsAllFiles()
        {
            Assert.Equal("All files (*.*)|*.*", InteractiveUserInteraction.OpenImageFilter(null));
        }
    }
}
