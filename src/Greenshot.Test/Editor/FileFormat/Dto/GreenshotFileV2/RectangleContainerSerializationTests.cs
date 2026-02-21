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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.V2;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

[Collection("DefaultCollection")]
public class RectangleContainerSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="RectangleContainer"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="RectangleContainer"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="RectangleContainer"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_RectangleContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var rectangleContainer = new RectangleContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(rectangleContainer);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<RectangleContainerDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as RectangleContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(rectangleContainer.Left, result.Left);
        Assert.Equal(rectangleContainer.Top, result.Top);
        Assert.Equal(rectangleContainer.Width, result.Width);
        Assert.Equal(rectangleContainer.Height, result.Height);
    }
}
