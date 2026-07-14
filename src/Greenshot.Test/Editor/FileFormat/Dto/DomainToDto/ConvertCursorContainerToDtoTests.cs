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
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertCursorContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_CursorContainer_Returns_CursorContainerDto()
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
        var result = ConvertDomainToDto.ToDto(cursorContainer);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cursorContainer.Left, result.Left);
        Assert.Equal(cursorContainer.Top, result.Top);
        Assert.Equal(cursorContainer.Width, result.Width);
        Assert.Equal(cursorContainer.Height, result.Height);
        Assert.Equal(cursorContainer.Cursor.HotSpot.X, result.HotspotX);
        Assert.Equal(cursorContainer.Cursor.HotSpot.Y, result.HotspotY);
        Assert.Equal(cursorContainer.Cursor.Size.Width, result.CursorWidth);
        Assert.Equal(cursorContainer.Cursor.Size.Height, result.CursorHeight);
        Assert.NotNull(result.ColorLayer);
        Assert.NotNull(result.MaskLayer);
    }
}
