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
using System.Drawing;
using System.IO;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToImageContainerTest
{
    [Fact]
    public void ConvertDtoToDomain_ImageContainerDto_Returns_ImageContainer()
    {
        // Arrange
        var image = new Bitmap(100, 100); // Create a sample image
        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            imageData = memoryStream.ToArray();
        }

        var dto = new ImageContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 222, // different width for testing, because the image is 100x100, so it is scaled  
            Height = 222, // different height for testing, because the image is 100x100, so it is scaled  
            Image = imageData,
            Fields = [ new FieldDto
            {
                FieldTypeName = nameof(FieldType.SHADOW),
                Scope = nameof(ImageContainer),
                Value = new BoolFieldValueDto
                {
                    Value = false
                }
            }]
        };


        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(222, result.Width);
        Assert.Equal(222, result.Height);
        Assert.NotNull(result.Image);
        Assert.Equal(image.Width, result.Image.Width);
        Assert.Equal(image.Height, result.Image.Height);
    }

    /// <summary>
    /// Test with shadow, the <see cref="DropShadowEffect"/> recalculates the size and position
    /// </summary>
    [Fact]
    public void ConvertDtoToDomain_ImageContainerDto_with_shadow_Returns_ImageContainer_with_shadow()
    {
        // Arrange
        var dropShadowImpact = new
        {
            AdditionalWidth = 14,
            AdditionalHeight = 14,
            OffsetTop = 1,
            OffsetLeft = 1
        };

        var image = new Bitmap(100, 100); // Create a sample image
        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            imageData = memoryStream.ToArray();
        }

        var dto = new ImageContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 222, // different width for testing, because the image is 100x100  
            Height = 222, // different height for testing, because the image is 100x100  
            Image = imageData,
            Fields = [ new FieldDto
            {
                FieldTypeName = nameof(FieldType.SHADOW),
                Scope = nameof(ImageContainer),
                Value = new BoolFieldValueDto
                {
                    Value = true
                }
            }]

        };


        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        Assert.NotNull(result);

        IField shadowField = result.GetField(FieldType.SHADOW);
        Assert.NotNull(shadowField);
        Assert.IsType<bool>(shadowField.Value);
        Assert.True((bool)shadowField.Value); // Ensure the shadow flag is true

        Assert.Equal(dto.Left + dropShadowImpact.OffsetLeft, result.Left); 
        Assert.Equal(dto.Top + dropShadowImpact.OffsetTop, result.Top); 
        Assert.Equal(100 + dropShadowImpact.AdditionalWidth, result.Width); 
        Assert.Equal(100 + dropShadowImpact.AdditionalHeight, result.Height); 
        Assert.NotNull(result.Image);
        Assert.Equal(image.Width, result.Image.Width);
        Assert.Equal(image.Height, result.Image.Height);
    }
}