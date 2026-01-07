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
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertHighlightContainerToDtoTests
{
    [Fact]
    public void ConvertDomainToDto_HighlightContainer_Returns_HighlightContainerDto()
    {
        // Arrange
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var highlightContainer = new HighlightContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        // see HighlightContainer.InitializeFields() for defaults
        var defaultLineThickness = 0;
        var defaultLineColor = Color.Red;
        var defaultShadow = false;
        var defaultPreparedFilter = PreparedFilter.TEXT_HIGHTLIGHT;

        // Act
        var result = ConvertDomainToDto.ToDto(highlightContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultPreparedFilter = DtoHelper.GetFieldValue(result, FieldType.PREPARED_FILTER_HIGHLIGHT);

        Assert.NotNull(result);
        Assert.Equal(highlightContainer.Left, result.Left);
        Assert.Equal(highlightContainer.Top, result.Top);
        Assert.Equal(highlightContainer.Width, result.Width);
        Assert.Equal(highlightContainer.Height, result.Height);
        
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

        Assert.NotNull(resultPreparedFilter);
        Assert.IsType<PreparedFilter>(resultPreparedFilter);
        Assert.Equal(defaultPreparedFilter, resultPreparedFilter);
    }

    [Fact]
    public void ConvertDomainToDto_HighlightContainer_with_Field_Values_Returns_HighlightContainerDto_with_same_Values()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
        var highlightContainer = new HighlightContainer(surface)
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50
        };
        highlightContainer.SetFieldValue(FieldType.LINE_THICKNESS, 3);
        highlightContainer.SetFieldValue(FieldType.LINE_COLOR, colorBlue);
        highlightContainer.SetFieldValue(FieldType.SHADOW, true);
        highlightContainer.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, PreparedFilter.AREA_HIGHLIGHT);
        
        // Act
        var result = ConvertDomainToDto.ToDto(highlightContainer);

        // Assert
        var resultLineThickness = DtoHelper.GetFieldValue(result, FieldType.LINE_THICKNESS);
        var resultLineColor = DtoHelper.GetFieldValue(result, FieldType.LINE_COLOR);
        var resultShadow = DtoHelper.GetFieldValue(result, FieldType.SHADOW);
        var resultPreparedFilter = DtoHelper.GetFieldValue(result, FieldType.PREPARED_FILTER_HIGHLIGHT);

        Assert.NotNull(result);
        
        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3,resultLineThickness);
        
        Assert.NotNull(resultLineColor);
        Assert.IsType<Color>(resultLineColor);
        Assert.True(DtoHelper.CompareColorValue(colorBlue, (Color)resultLineColor),
            $"The color values are different. expected:{DtoHelper.ArgbString(colorBlue)} result:{DtoHelper.ArgbString((Color)resultLineColor)}");

        Assert.NotNull(resultShadow);
        Assert.IsType<bool>(resultShadow);
        Assert.True((bool)resultShadow);
        
        Assert.NotNull(resultPreparedFilter);
        Assert.IsType<PreparedFilter>(resultPreparedFilter);
        Assert.Equal(PreparedFilter.AREA_HIGHLIGHT, resultPreparedFilter);
    }
}
