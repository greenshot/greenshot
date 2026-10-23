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
using System.Text;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The format of an image file by its first bytes, and the file name of an image url
    /// </summary>
    public sealed class ImageFileContentTests
    {
        [Theory]
        [InlineData(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }, ".png")]
        [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F' }, ".jpg")]
        [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 1, 0 }, ".gif")]
        [InlineData(new byte[] { (byte)'B', (byte)'M', 0x36, 0, 0, 0, 0, 0 }, ".bmp")]
        [InlineData(new byte[] { 0, 0, 1, 0, 1, 0, 16, 16 }, ".ico")]
        [InlineData(new byte[] { (byte)'I', (byte)'I', 0x2A, 0, 8, 0, 0, 0 }, ".tif")]
        [InlineData(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' }, ".webp")]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, null)]
        [InlineData(new byte[] { 0x89, (byte)'P' }, null)]
        public void GuessExtension_ByTheFirstBytes(byte[] content, string expected)
        {
            Assert.Equal(expected, ImageFileContent.GuessExtension(content));
        }

        [Theory]
        [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"/>", ".svg")]
        [InlineData("﻿<?xml version=\"1.0\"?>\n<!-- logo -->\n<svg width=\"1\"></svg>", ".svg")]
        [InlineData("<!DOCTYPE html><html><body><svg></svg></body></html>", null)]
        [InlineData("just some text, not an image", null)]
        public void GuessExtension_OfSvg_NotOfAPage(string text, string expected)
        {
            Assert.Equal(expected, ImageFileContent.GuessExtension(Encoding.UTF8.GetBytes(text)));
        }

        [Theory]
        [InlineData("https://example.com/images/red%20star.svg?size=2", ".svg", "red star.svg")]
        [InlineData("https://example.com/image.php?id=1", ".png", "image.png")]
        [InlineData("https://example.com/icons/check", ".png", "check.png")]
        [InlineData("https://example.com/", ".gif", "image.gif")]
        public void FileNameOf_TheLastSegment_WithTheExtensionOfTheImage(string url, string extension, string expected)
        {
            Assert.Equal(expected, ImageFileContent.FileNameOf(new Uri(url), extension));
        }
    }
}
