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
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertSvgContainerToDtoTest
{
    [Fact]
    public void ConvertDomainToDto_SvgContainer_Returns_SvgContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var svgFilePath = Path.Combine("TestData", "Images", "Logo_G_with_Border.svg");
        using var svgStream = File.OpenRead(svgFilePath);

        var svgContainer = new SvgContainer(svgStream, surface);

        // Act
        var result = ConvertDomainToDto.ToDto(svgContainer);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(svgContainer.Left, result.Left);
        Assert.Equal(svgContainer.Top, result.Top);
        Assert.Equal(svgContainer.Width, result.Width);
        Assert.Equal(svgContainer.Height, result.Height);

        // Ensure the SVG was serialized
        Assert.NotNull(result.SvgData);
        Assert.True(result.SvgData.Length > 0);
    }
}
