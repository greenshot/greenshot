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
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.FileFormat.Dto;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertEmojiContainerToDtoTest
{
    [Fact]
    public void ConvertDomainToDto_EmojiContainer_Returns_EmojiContainerDto()
    {
        // Arrange
        var surface = (Surface)SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var emoji = "??";
        var emojiContainer = new EmojiContainer(surface, emoji)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 100,
            RotationAngle = 45
        };

        // Act
        var result = ConvertDomainToDto.ToDto(emojiContainer);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(emojiContainer.Left, result.Left);
        Assert.Equal(emojiContainer.Top, result.Top);
        Assert.Equal(emojiContainer.Width, result.Width);
        Assert.Equal(emojiContainer.Height, result.Height);
        Assert.Equal(emojiContainer.Emoji, result.Emoji);
        Assert.Equal(emojiContainer.RotationAngle, result.RotationAngle);
    }
}
