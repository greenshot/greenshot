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
using System.Drawing;
using System.Linq;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

[Collection("DefaultCollection")]
public class ConvertDtoToHighlightContainerTests
{
    [Fact]
    public void ConvertDtoToDomain_HighlightContainerDto_Returns_HighlightContainer()
    {
        // Arrange
        var colorBlue = Color.Blue;
        var dto = new HighlightContainerDto
        {
            Left = 10,
            Top = 20,
            Width = 100,
            Height = 50,
            Fields = [ new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_THICKNESS),
                    Scope = nameof(HighlightContainer),
                    Value = new IntFieldValueDto
                    {
                        Value = 3 // default in HighlightContainer is 0
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.LINE_COLOR),
                    Scope = nameof(HighlightContainer),
                    Value = new ColorFieldValueDto
                    {
                        Value = colorBlue // default in HighlightContainer is Red
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.SHADOW),
                    Scope = nameof(HighlightContainer),
                    Value = new BoolFieldValueDto
                    {
                        Value = true // default in HighlightContainer is false
                    }
                },
                new FieldDto
                {
                    FieldTypeName = nameof(FieldType.PREPARED_FILTER_HIGHLIGHT),
                    Scope = nameof(HighlightContainer),
                    Value = new PreparedFilterFieldValueDto
                    {
                        Value = PreparedFilter.AREA_HIGHLIGHT // default in HighlightContainer is TEXT_HIGHTLIGHT
                    }
                }
            ]
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(dto, null);

        // Assert
        var resultLineThickness = result.GetFieldValue(FieldType.LINE_THICKNESS);
        var resultLineColor = result.GetFieldValue(FieldType.LINE_COLOR);
        var resultShadow = result.GetFieldValue(FieldType.SHADOW);
        var resultPreparedFilter = result.GetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT);
        var allFilter = result.Filters;
        var brigthnessFilter = allFilter.OfType<BrightnessFilter>().FirstOrDefault();
        var blurFilter = allFilter.OfType<BlurFilter>().FirstOrDefault();

        Assert.NotNull(result);
        Assert.Equal(dto.Left, result.Left);
        Assert.Equal(dto.Top, result.Top);
        Assert.Equal(dto.Width, result.Width);
        Assert.Equal(dto.Height, result.Height);

        Assert.NotNull(resultLineThickness);
        Assert.IsType<int>(resultLineThickness);
        Assert.Equal(3, resultLineThickness);

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

        // PreparedFilter.AREA_HIGHLIGHT should add BrightnessFilter and BlurFilter 
        Assert.NotNull(allFilter);
        Assert.Equal(2,allFilter?.Count);
        Assert.NotNull(brigthnessFilter);
        Assert.NotNull(blurFilter);
    }
}
