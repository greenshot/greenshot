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
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

public class SpeechbubbleContainerSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="SpeechbubbleContainer"/> object.
    /// </summary>
    /// <remarks>This test verifies that an <see cref="SpeechbubbleContainer"/> object can be correctly converted to
    /// its DTO representation, serialized, deserialized and then converted back to <see cref="SpeechbubbleContainer"/>.</remarks>
    [Fact]
    public void SerializeDeserialize_SpeechbubbleContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var speechbubbleContainer = new SpeechbubbleContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Text = "Hello, greenshot!",
            StoredTargetGripperLocation = new Point(30, 40)
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(speechbubbleContainer);
        //var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = dto;// MessagePackSerializer.Deserialize<SpeechbubbleContainerDto>(serialized);
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as SpeechbubbleContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(speechbubbleContainer.Left, result.Left);
        Assert.Equal(speechbubbleContainer.Top, result.Top);
        Assert.Equal(speechbubbleContainer.Width, result.Width);
        Assert.Equal(speechbubbleContainer.Height, result.Height);
        Assert.Equal(speechbubbleContainer.Text, result.Text);
        Assert.Equal(speechbubbleContainer.StoredTargetGripperLocation, result.StoredTargetGripperLocation);
    }
}
