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
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertLineContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_LineContainer_Returns_LineContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var lineContainer = new LineContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        // see LineContainer.InitializeFields() for defaults
        var defaultLineThickness = 2;
        var defaultLineColor = Color.Red;
        var defaultShadow = true;

        // Act
        var result = ConvertDomainToDto.ToDto(lineContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);

        Assert.NotNull(result);
        Assert.Equal(lineContainer.Left, result.Left);
        Assert.Equal(lineContainer.Top, result.Top);
        Assert.Equal(lineContainer.Width, result.Width);
        Assert.Equal(lineContainer.Height, result.Height);
        
        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(defaultShadow,(bool)resultShadow);
        
        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(defaultLineThickness, resultLineThickness);
        
        Assert.NotNull(resultColor);
        Assert.IsType<Color>(resultColor);
        Assert.True(DtoHelper.CompareColorValue(defaultLineColor, (Color)resultColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultLineColor)} result:{DtoHelper.ArgbString((Color)resultColor)}");
    }

    [Fact]
    public void ConvertDomainToDto_LineContainer_with_Field_Values_Returns_LineContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var lineContainer = new LineContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        lineContainer.SetFieldValue(FieldType.LINE_THICKNESS, 3);
        lineContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);
        lineContainer.SetFieldValue(FieldType.SHADOW, false);
        
        // Act
        var result = ConvertDomainToDto.ToDto(lineContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);

        Assert.NotNull(result);
        
        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3,resultLineThickness);
        
        Assert.NotNull(resultColor);
        Assert.IsType<Color>(resultColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.False((bool)resultShadow);
    }
}
