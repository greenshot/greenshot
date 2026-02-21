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
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media.Effects;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.V2;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

[Collection("DefaultCollection")]
public class SvgContainerSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="SvgContainer"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="SvgContainer"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="SvgContainer"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_SvgContainer()
    {
        // Arrange
        var svgFilePath = Path.Combine("TestData", "Images", "Logo_G_with_Border.svg");
        using var svgStream = File.OpenRead(svgFilePath);

        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var svgContainer = new SvgContainer(svgStream, surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(svgContainer);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<SvgContainerDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as SvgContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(svgContainer.Left, result.Left);
        Assert.Equal(svgContainer.Top, result.Top);
        Assert.Equal(svgContainer.Width, result.Width);
        Assert.Equal(svgContainer.Height, result.Height);

        // only simple check because the SVG content is here a memory stream
        Assert.NotNull(result.SvgContent);
    }

    /// <summary>
    /// Tests the serialization and deserialization of a rotated <see cref="SvgContainer"/> object.
    /// </summary>
    [Fact]
    public void SerializeDeserialize_RotatedSvgContainer()
    {
        // Arrange
        var svgFilePath = Path.Combine("TestData", "Images", "Logo_G_with_Border.svg");
        using var svgStream = File.OpenRead(svgFilePath);

        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var svgContainer = new SvgContainer(svgStream, surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        // imitate surface.ApplyBitmapEffect(new RotateEffect(90));
        var effect = new RotateEffect(90);
        var image = new Bitmap(300, 400);
        Matrix matrix = new Matrix();
        Image newImage = ImageHelper.ApplyEffect(image, effect, matrix);
        svgContainer.Transform(matrix);

        // Act
        var dto = ConvertDomainToDto.ToDto(svgContainer);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<SvgContainerDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as SvgContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(svgContainer.Left, result.Left);
        Assert.Equal(svgContainer.Top, result.Top);
        Assert.Equal(svgContainer.Width, result.Width);
        Assert.Equal(svgContainer.Height, result.Height);

        // only simple check because the SVG content is here a memory stream
        Assert.NotNull(result.SvgContent);

        // Check if the rotation angle is preserved
        Assert.Equal(90, result.RotationAngle);

    }
}
