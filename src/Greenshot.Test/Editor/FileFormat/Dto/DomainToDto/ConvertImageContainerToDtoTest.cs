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
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertImageContainerToDtoTest
{
    [Fact]
    public void ConvertDomainToDto_ImageContainer_Returns_ImageContainerDto()
    {
        // Arrange
        var image = new Bitmap(100, 100); // Create a sample image
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var imageContainer = new ImageContainer(surface);
        imageContainer.Image = image;

        // Act
        var result = ConvertDomainToDto.ToDto(imageContainer);

        // Assert
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);

        Assert.NotNull(result);
        Assert.Equal(imageContainer.Left, result.Left);
        Assert.Equal(imageContainer.Top, result.Top);
        Assert.Equal(imageContainer.Width, result.Width);
        Assert.Equal(imageContainer.Height, result.Height);
        Assert.NotNull(result.Image);
        Assert.True(result.Image.Length > 0); // Ensure the image was serialized

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.False((bool)resultShadow); // Ensure the shadow flag is false by default

    }
    
    [Fact]
    public void ConvertDomainToDto_ImageContainer_with_shadow_Returns_ImageContainerDto_with_Shadow()
    {
        // Arrange
        var image = new Bitmap(100, 100); // Create a sample image
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var imageContainer = new ImageContainer(surface);
        imageContainer.Image = image;
        imageContainer.SetFieldValue(FieldType.SHADOW, true);

        // Act
        var result = ConvertDomainToDto.ToDto(imageContainer);

        // Assert
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);

        Assert.NotNull(result);
        Assert.Equal(imageContainer.Left, result.Left);
        Assert.Equal(imageContainer.Top, result.Top);
        Assert.Equal(imageContainer.Width, result.Width);
        Assert.Equal(imageContainer.Height, result.Height);
        Assert.NotNull(result.Image);
        Assert.True(result.Image.Length > 0); // Ensure the image was serialized

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.True((bool)resultShadow);
    }

}