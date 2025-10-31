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
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using MessagePack;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

public class FreehandContainerSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="FreehandContainer"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="FreehandContainer"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="FreehandContainer"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_FreehandContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var freehandContainer = new FreehandContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            CapturePoints = [new Point(10, 20), new Point(30, 40)]
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(freehandContainer);
        var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = MessagePackSerializer.Deserialize<FreehandContainerDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as FreehandContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(freehandContainer.Left, result.Left);
        Assert.Equal(freehandContainer.Top, result.Top);
        Assert.Equal(freehandContainer.Width, result.Width);
        Assert.Equal(freehandContainer.Height, result.Height);
        Assert.NotNull(result.CapturePoints);
        Assert.Equal(freehandContainer.CapturePoints.Count, result.CapturePoints.Count);
        Assert.Equal(freehandContainer.CapturePoints[0].X, result.CapturePoints[0].X);
        Assert.Equal(freehandContainer.CapturePoints[0].Y, result.CapturePoints[0].Y);
        Assert.Equal(freehandContainer.CapturePoints[1].X, result.CapturePoints[1].X);
        Assert.Equal(freehandContainer.CapturePoints[1].Y, result.CapturePoints[1].Y);
    }
}
