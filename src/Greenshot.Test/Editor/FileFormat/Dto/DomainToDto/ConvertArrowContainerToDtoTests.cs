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
using static Greenshot.Editor.Drawing.ArrowContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertArrowContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_ArrowContainer_Returns_ArrowContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var arrowContainer = new ArrowContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        // see ArrowContainer.InitializeFields() for defaults
        var defaultLineThickness = 2;
        var defaultLineColor = Color.Red;
        var defaultFillColor = Color.Transparent;
        var defaultShadow = true;
        var defaultArrowHeads = ArrowHeadCombination.END_POINT;

        // Act
        var result = ConvertDomainToDto.ToDto(arrowContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultArrowHeads = DtoHelper.GetFieldValue(result, FieldType.ARROWHEADS);

        Assert.NotNull(result);
        Assert.Equal(arrowContainer.Left, result.Left);
        Assert.Equal(arrowContainer.Top, result.Top);
        Assert.Equal(arrowContainer.Width, result.Width);
        Assert.Equal(arrowContainer.Height, result.Height);

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.Equal(defaultShadow, (bool)resultShadow);

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
        
        Assert.NotNull(resultArrowHeads);
        Assert.IsType<ArrowHeadCombination>(resultArrowHeads);
        Assert.Equal(defaultArrowHeads, resultArrowHeads);
    }

    [Fact]
    public void ConvertDomainToDto_ArrowContainer_with_Field_Values_Returns_ArrowContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var colorGreen = Color.Green;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var arrowContainer = new ArrowContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        arrowContainer.SetFieldValue(FieldType.LINE_THICKNESS, 3);
        arrowContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);
        arrowContainer.SetFieldValue(FieldType.FILL_COLOR, colorGreen);
        arrowContainer.SetFieldValue(FieldType.SHADOW, false);
        arrowContainer.SetFieldValue(FieldType.ARROWHEADS, ArrowHeadCombination.BOTH);

        // Act
        var result = ConvertDomainToDto.ToDto(arrowContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultFillColor = DtoHelper.GetFieldValue(result, FieldType.FILL_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultArrowHeads = DtoHelper.GetFieldValue(result, FieldType.ARROWHEADS);

        Assert.NotNull(result);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3, resultLineThickness);

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
        
        Assert.NotNull(resultArrowHeads);
        Assert.IsType<ArrowHeadCombination>(resultArrowHeads);
        Assert.Equal(ArrowHeadCombination.BOTH, resultArrowHeads);
    }
}
