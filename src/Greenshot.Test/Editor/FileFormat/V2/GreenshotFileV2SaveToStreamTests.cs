/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.V2;

[Collection("DefaultCollection")]
public class GreenshotFileV2SaveToStreamTests
{
    [Fact]
    public void SaveToStream_WritesZipWithContentJson_AndRectangleContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        surface.Image = new Bitmap(100, 100); // Create a sample image
        var rectangleContainer = new RectangleContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 150,
            Height = 80
        };

        surface.Elements.Add(rectangleContainer);
        using var stream = new MemoryStream();

        // Act
        // TODO : Call V2 - version directly
        GreenshotFileVersionHandler.SaveToStreamInCurrentVersion(surface, stream);

        // Assert
        var savedBytes = stream.ToArray();

        using var zipStream = new MemoryStream(savedBytes);
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read, true);
        var entry = zipArchive.GetEntry("content.json");
        Assert.NotNull(entry);

        using var loadStream = new MemoryStream(savedBytes);
        var loaded = GreenshotFileVersionHandler.LoadFromStream(loadStream);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.ContainerList);
        Assert.Single(loaded.ContainerList);
        var loadedRect = Assert.IsType<RectangleContainer>(loaded.ContainerList[0]);
        Assert.Equal(rectangleContainer.Left, loadedRect.Left);
        Assert.Equal(rectangleContainer.Top, loadedRect.Top);
        Assert.Equal(rectangleContainer.Width, loadedRect.Width);
        Assert.Equal(rectangleContainer.Height, loadedRect.Height);
    }
}
