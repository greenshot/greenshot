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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToApplicationFileTests
{
    [Fact]
    public void ConvertDtoToDomain_ApplicationFileDto_Returns_ApplicationFile()
    {
        // Arrange
        var lineDto = new LineContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        var rectangleDto = new RectangleContainerDto
        {
            Left = 30,
            Top = 40,
            Width = 200,
            Height = 80
        };
        var dtoList = new DrawableContainerListDto
        {
            ContainerList = [lineDto, rectangleDto]
        };
        var image = new Bitmap(100, 100); // Create a sample image
        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            imageData = memoryStream.ToArray();
        }
        var dto = new GreenshotFileDto
        {
            ContainerList = dtoList,
            Image = imageData,
            SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion
        };

        // Act
        var domain = ConvertDtoToDomain.ToDomain(dto);

        // Assert
        Assert.NotNull(domain);
        Assert.Equal(dto.SchemaVersion, domain.SchemaVersion);
        Assert.NotNull(domain.Image);
        Assert.NotNull(domain.ContainerList);
        Assert.Equal(2, domain.ContainerList.Count);
        Assert.IsType<LineContainer>(domain.ContainerList[0]);
        Assert.IsType<RectangleContainer>(domain.ContainerList[1]);

        Assert.Equal(lineDto.Top, domain.ContainerList[0].Top);
        Assert.Equal(lineDto.Left, domain.ContainerList[0].Left);
        Assert.Equal(lineDto.Width, domain.ContainerList[0].Width);
        Assert.Equal(lineDto.Height, domain.ContainerList[0].Height);

        Assert.Equal(rectangleDto.Top, domain.ContainerList[1].Top);
        Assert.Equal(rectangleDto.Left, domain.ContainerList[1].Left);
        Assert.Equal(rectangleDto.Width, domain.ContainerList[1].Width);
        Assert.Equal(rectangleDto.Height, domain.ContainerList[1].Height);
    }
}
