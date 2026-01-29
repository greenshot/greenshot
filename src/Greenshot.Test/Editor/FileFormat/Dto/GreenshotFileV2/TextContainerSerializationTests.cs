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
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

public class TextContainerSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="TextContainer"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="TextContainer"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="TextContainer"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_TextContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var textContainer = new TextContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!"
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(textContainer);
        //var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = dto;//  MessagePackSerializer.Deserialize<TextContainerDto>(serialized);
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as TextContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(textContainer.Left, result.Left);
        Assert.Equal(textContainer.Top, result.Top);
        Assert.Equal(textContainer.Width, result.Width);
        Assert.Equal(textContainer.Height, result.Height);
        Assert.Equal(textContainer.Text, result.Text);
    }
}
