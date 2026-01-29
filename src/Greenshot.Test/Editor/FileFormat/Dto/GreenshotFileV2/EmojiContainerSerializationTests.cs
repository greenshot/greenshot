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
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

[Collection("DefaultCollection")]
public class EmojiContainerSerializationTests
{
    /// <summary>
    /// Tests the serialization and deserialization process of an <see cref="EmojiContainer"/> object.
    /// </summary>
    [Fact]
    public void SerializeDeserialize_EmojiContainer()
    {
        // Arrange
        var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var emoji = "??";
        var emojiContainer = new EmojiContainer(surface, emoji)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 100
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(emojiContainer);
        // var serialized = MessagePackSerializer.Serialize(dto);
        // var deserializedDto = MessagePackSerializer.Deserialize<EmojiContainerDto>(serialized);
        var deserializedDto = dto;
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as EmojiContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(emojiContainer.Left, result.Left);
        Assert.Equal(emojiContainer.Top, result.Top);
        Assert.Equal(emojiContainer.Width, result.Width);
        Assert.Equal(emojiContainer.Height, result.Height);
        Assert.Equal(emojiContainer.Emoji, result.Emoji);
    }

    /// <summary>
    /// Tests the serialization and deserialization of a rotated <see cref="EmojiContainer"/> object.
    /// </summary>
    [Fact]
    public void SerializeDeserialize_RotatedEmojiContainer()
    {
        // Arrange
        var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var emoji = "??";
        var emojiContainer = new EmojiContainer(surface, emoji)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 100
        };

        // imitate surface.ApplyBitmapEffect(new RotateEffect(90));
        var effect = new RotateEffect(90);
        var image = new Bitmap(300, 400);
        Matrix matrix = new Matrix();
        Image newImage = ImageHelper.ApplyEffect(image, effect, matrix);
        emojiContainer.Transform(matrix);

        // Act
        var dto = ConvertDomainToDto.ToDto(emojiContainer);
        // var serialized = MessagePackSerializer.Serialize(dto);
        // var deserializedDto = MessagePackSerializer.Deserialize<EmojiContainerDto>(serialized);
        var deserializedDto = dto;
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as EmojiContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(emojiContainer.Left, result.Left);
        Assert.Equal(emojiContainer.Top, result.Top);
        Assert.Equal(emojiContainer.Width, result.Width);
        Assert.Equal(emojiContainer.Height, result.Height);
        Assert.Equal(emojiContainer.Emoji, result.Emoji);
        Assert.Equal(90, result.RotationAngle);
    }
}
