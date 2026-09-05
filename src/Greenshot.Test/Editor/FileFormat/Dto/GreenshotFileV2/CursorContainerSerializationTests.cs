/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.V2;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

[Collection("DefaultCollection")]
public class CursorContainerSerializationTests
{
    [Fact]
    public void SerializeDeserialize_CursorContainer()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var colorBitmap = new Bitmap(32, 32);
        var maskBitmap = new Bitmap(32, 32);
        
        using (var g = Graphics.FromImage(colorBitmap))
        {
            g.Clear(Color.Red);
        }
        using (var g = Graphics.FromImage(maskBitmap))
        {
            g.Clear(Color.Black);
        }

        var cursorContainer = new CursorContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        
        cursorContainer.Cursor = new CapturedCursor
        {
            HotSpot = new NativePoint(5, 10),
            Size = new NativeSize(32, 32),
            ColorLayer = colorBitmap,
            MaskLayer = maskBitmap
        };

        // Act
        var dto = ConvertDomainToDto.ToDto(cursorContainer);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<CursorContainerDto>(serialized);
        var result = ConvertDtoToDomain.ToDomain(deserializedDto, null) as CursorContainer;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cursorContainer.Left, result.Left);
        Assert.Equal(cursorContainer.Top, result.Top);
        Assert.Equal(cursorContainer.Width, result.Width);
        Assert.Equal(cursorContainer.Height, result.Height);
        
        Assert.NotNull(result.Cursor);
        Assert.Equal(cursorContainer.Cursor.HotSpot.X, result.Cursor.HotSpot.X);
        Assert.Equal(cursorContainer.Cursor.HotSpot.Y, result.Cursor.HotSpot.Y);
        Assert.Equal(cursorContainer.Cursor.Size.Width, result.Cursor.Size.Width);
        Assert.Equal(cursorContainer.Cursor.Size.Height, result.Cursor.Size.Height);
        
        Assert.NotNull(result.Cursor.ColorLayer);
        Assert.Equal(cursorContainer.Cursor.ColorLayer.Width, result.Cursor.ColorLayer.Width);
        Assert.Equal(cursorContainer.Cursor.ColorLayer.Height, result.Cursor.ColorLayer.Height);
        
        Assert.NotNull(result.Cursor.MaskLayer);
        Assert.Equal(cursorContainer.Cursor.MaskLayer.Width, result.Cursor.MaskLayer.Width);
        Assert.Equal(cursorContainer.Cursor.MaskLayer.Height, result.Cursor.MaskLayer.Height);
    }
}
