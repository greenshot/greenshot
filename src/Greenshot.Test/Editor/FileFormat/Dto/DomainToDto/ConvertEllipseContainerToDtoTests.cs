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
public class ConvertEllipseContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_EllipseContainer_Returns_EllipseContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var ellipseContainer = new EllipseContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        // see EllipseContainer.InitializeFields() for defaults
        var defaultLineThickness = 2;
        var defaultLineColor = Color.Red;
        var defaultFillColor = Color.Transparent;
        var defaultShadow = true;

        // Act
        var result = ConvertDomainToDto.ToDto(ellipseContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);

        Assert.NotNull(result);
        Assert.Equal(ellipseContainer.Left, result.Left);
        Assert.Equal(ellipseContainer.Top, result.Top);
        Assert.Equal(ellipseContainer.Width, result.Width);
        Assert.Equal(ellipseContainer.Height, result.Height);
        
        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(defaultShadow,(bool)resultShadow);
        
        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(defaultLineThickness, resultLineThickness);
        
        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(defaultLineColor, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultLineColor)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");
        
        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(defaultFillColor, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(defaultFillColor)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");
    }

    [Fact]
    public void ConvertDomainToDto_EllipseContainer_with_Field_Values_Returns_EllipseContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorGreen = Color.Green;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var ellipseContainer = new EllipseContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        ellipseContainer.SetFieldValue(FieldType.LINE_THICKNESS, 3);
        ellipseContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);
        ellipseContainer.SetFieldValue(FieldType.FILL_COLOR, colorGreen);
        ellipseContainer.SetFieldValue(FieldType.SHADOW, false);
        
        // Act
        var result = ConvertDomainToDto.ToDto(ellipseContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);

        Assert.NotNull(result);
        
        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3,resultLineThickness);
        
        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");
        
        Assert.NotNull(resultFillColor);
        Assert.IsType<Color>(resultFillColor);
        Assert.True(DtoHelper.CompareColorValue(colorGreen, (Color)resultFillColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorGreen)} result:{DtoHelper.ArgbString((Color)resultFillColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.False((bool)resultShadow);
    }
}
