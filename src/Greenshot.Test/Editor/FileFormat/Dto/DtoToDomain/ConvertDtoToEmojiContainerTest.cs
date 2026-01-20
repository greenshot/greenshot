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
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToEmojiContainerTest
{
    [Fact]
    public void ConvertDtoToDomain_EmojiContainerDto_Returns_EmojiContainer()
    {
        // Arrange
        var emoji = "??";
        var dto = new EmojiContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 100,
            RotationAngle = 45,
            Emoji = emoji
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);
        Assert.Equal(dto.Emoji, result.Emoji);
        Assert.Equal(dto.RotationAngle, result.RotationAngle);
    }
}
