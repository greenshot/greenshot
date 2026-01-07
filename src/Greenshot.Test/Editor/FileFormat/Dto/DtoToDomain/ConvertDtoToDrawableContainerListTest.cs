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
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToDrawableContainerListTest
{
    [Fact]
    public void ConvertDtoToDomain_DrawableContainerListDto_Returns_DrawableContainerList()
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

        // Act
        var domainList = ConvertDtoToDomain.ToDomain(dtoList);

        // Assert
        Assert.NotNull(domainList);
        Assert.Equal(2, domainList.Count);
        Assert.IsType<LineContainer>(domainList[0]);
        Assert.IsType<RectangleContainer>(domainList[1]);

        Assert.Equal(lineDto.Top, domainList[0].Top);
        Assert.Equal(lineDto.Left, domainList[0].Left);
        Assert.Equal(lineDto.Width, domainList[0].Width);
        Assert.Equal(lineDto.Height, domainList[0].Height);

        Assert.Equal(rectangleDto.Top, domainList[1].Top);
        Assert.Equal(rectangleDto.Left, domainList[1].Left);
        Assert.Equal(rectangleDto.Width, domainList[1].Width);
        Assert.Equal(rectangleDto.Height, domainList[1].Height);
    }
}