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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToCursorContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_CursorContainerDto_Returns_CursorContainer()
    {
        // Arrange
        var dto = new CursorContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            HotspotX = 5,
            HotspotY = 10,
            CursorWidth = 32,
            CursorHeight = 32
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);
        
        Assert.NotNull(result.Cursor);
        Assert.Equal(dto.HotspotX, result.Cursor.HotSpot.X);
        Assert.Equal(dto.HotspotY, result.Cursor.HotSpot.Y);
        Assert.Equal(dto.Width, result.Cursor.Size.Width);
        Assert.Equal(dto.Height, result.Cursor.Size.Height);
    }

    [Fact]
    public void ConvertDtoToDomain_CursorContainerDto_WithLayers_Returns_CursorContainer()
    {
        // Arrange
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

        byte[] colorLayerData;
        byte[] maskLayerData;
        using (var ms = new MemoryStream())
        {
            colorBitmap.Save(ms, ImageFormat.Png);
            colorLayerData = ms.ToArray();
        }
        using (var ms = new MemoryStream())
        {
            maskBitmap.Save(ms, ImageFormat.Png);
            maskLayerData = ms.ToArray();
        }

        var dto = new CursorContainerDto
        {
            Left = 600,
            Top = 100,
            Width = 64,
            Height = 64,
            HotspotX = 10,
            HotspotY = 5,
            CursorWidth = 32,
            CursorHeight = 32,
            ColorLayer = colorLayerData,
            MaskLayer = maskLayerData
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);
        
        Assert.NotNull(result.Cursor);
        Assert.Equal(dto.HotspotX, result.Cursor.HotSpot.X);
        Assert.Equal(dto.HotspotY, result.Cursor.HotSpot.Y);
        Assert.Equal(dto.Width, result.Cursor.Size.Width);
        Assert.Equal(dto.Height, result.Cursor.Size.Height);
        
        Assert.NotNull(result.Cursor.ColorLayer);
        Assert.Equal(32, result.Cursor.ColorLayer.Width);
        Assert.Equal(32, result.Cursor.ColorLayer.Height);
        
        Assert.NotNull(result.Cursor.MaskLayer);
        Assert.Equal(32, result.Cursor.MaskLayer.Width);
        Assert.Equal(32, result.Cursor.MaskLayer.Height);
    }
}
