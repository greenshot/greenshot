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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertApplicationToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_ApplicationFile_Returns_ApplicationFileDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var lineContainer = new LineContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        var rectangleContainer = new RectangleContainer(surface)
        {
            Left = 30,
            Top = 40,
            Width = 200,
            Height = 80
        };
        var domainList = new DrawableContainerList { lineContainer, rectangleContainer };
        var image = new Bitmap(10, 10);
        var domain = new GreenshotFile
        {
            ContainerList = domainList,
            Image = image,
            MetaInformation = new GreenshotFileMetaInformation
            {
                SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion
            }
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(domain);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(domain.MetaInformation.SchemaVersion, dto.MetaInformation.SchemaVersion);
        Assert.NotNull(dto.Image);
        Assert.NotNull(dto.ContainerList);
        Assert.Equal(2, dto.ContainerList.ContainerList.Count);
        Assert.IsType<LineContainerDto>(dto.ContainerList.ContainerList[0]);
        Assert.IsType<RectangleContainerDto>(dto.ContainerList.ContainerList[1]);

        Assert.Equal(lineContainer.Top, dto.ContainerList.ContainerList[0].Top);
        Assert.Equal(lineContainer.Left, dto.ContainerList.ContainerList[0].Left);
        Assert.Equal(lineContainer.Width, dto.ContainerList.ContainerList[0].Width);
        Assert.Equal(lineContainer.Height, dto.ContainerList.ContainerList[0].Height);

        Assert.Equal(rectangleContainer.Top, dto.ContainerList.ContainerList[1].Top);
        Assert.Equal(rectangleContainer.Left, dto.ContainerList.ContainerList[1].Left);
        Assert.Equal(rectangleContainer.Width, dto.ContainerList.ContainerList[1].Width);
        Assert.Equal(rectangleContainer.Height, dto.ContainerList.ContainerList[1].Height);
    }

    /// <summary>
    /// Trivial test to ensure that null ApplicationFile returns null DTO.
    /// </summary>
    [Fact]
    public void ToDto_NullApplicationFile_ReturnsNull()
    {
        // Arrange
        GreenshotFile domain = null;

        // Act
        // ReSharper disable once ExpressionIsAlwaysNull
        var result = ConvertDomainToDto.ToDto(domain);

        // Assert
        Assert.Null(result);
    }

}
