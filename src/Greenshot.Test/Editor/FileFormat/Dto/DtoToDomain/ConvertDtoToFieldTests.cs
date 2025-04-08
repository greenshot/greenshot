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
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;
using static Greenshot.Editor.Drawing.ArrowContainer;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;

public class ConvertDtoToFieldTests
{
    /// <summary>
    /// A collectioan of all supported value types for <see cref="FieldDto"/>.
    /// </summary>
    public static IEnumerable<object[]> GetFieldValueTestData()
    {
        yield return [FieldType.FONT_BOLD, typeof(bool), new BoolFieldValueDto { Value = true }, true];
        yield return [FieldType.FONT_FAMILY, typeof(string), new StringFieldValueDto { Value = "Arial" }, "Arial"];
        yield return [FieldType.LINE_THICKNESS, typeof(int), new IntFieldValueDto { Value = 42 }, 42];
        yield return [FieldType.PREVIEW_QUALITY, typeof(float), new SingleFieldValueDto { Value = 3.14f }, 3.14f];
        yield return [FieldType.PREVIEW_QUALITY, typeof(double), new DoubleFieldValueDto { Value = 3.14d }, 3.14d];
        yield return [FieldType.PREVIEW_QUALITY, typeof(decimal), new DecimalFieldValueDto { Value = 3.14m }, 3.14m];
        yield return [FieldType.PREVIEW_QUALITY, typeof(ArrowHeadCombination), new ArrowHeadCombinationFieldValueDto { Value = ArrowHeadCombination.END_POINT }, ArrowHeadCombination.END_POINT];
        yield return [FieldType.PREPARED_FILTER_HIGHLIGHT, typeof(PreparedFilter), new PreparedFilterFieldValueDto { Value = PreparedFilter.AREA_HIGHLIGHT }, PreparedFilter.AREA_HIGHLIGHT];
        yield return [FieldType.TEXT_HORIZONTAL_ALIGNMENT, typeof(StringAlignment), new StringAlignmentFieldValueDto { Value = StringAlignment.Center }, StringAlignment.Center];
        yield return [FieldType.FLAGS, typeof(FieldFlag), new FieldFlagFieldValueDto { Value = FieldFlag.CONFIRMABLE }, FieldFlag.CONFIRMABLE];
    }
    
    /// <summary>
    /// This test verifies that the <see cref="ConvertDtoToDomain.ToDomain(Dto)"/> method correctly maps
    /// the properties of a <see cref="FieldDto"/> to a <see cref="Field"/> object.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetFieldValueTestData))]
    public void ConvertDTOToDomain_FieldDto_Returns_Field(IFieldType field, Type valueType, FieldValueDto dto, object value)
    {
        // Arrange
        var fieldDto = new FieldDto
        {
            FieldTypeName = field.Name,
            Scope = "TestScope",
            Value = dto
        };

        // Act
        var result = ConvertDtoToDomain.ToDomain(fieldDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(field.Name, result.FieldType.Name);
        Assert.Equal("TestScope", result.Scope);
        Assert.IsType(valueType, result.Value);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ConvertDTOToDomain_ColorFieldValueDto_ReturnsColorField()
    {
        // Arrange
        var redColor = Color.Red;
        var dto = new ColorFieldValueDto { Value = redColor };
        var fieldDto = new FieldDto
        {
            FieldTypeName = FieldType.FILL_COLOR.Name,
            Scope = "TestScope",
            Value = dto
        };
        // Act
        var result = ConvertDtoToDomain.ToDomain(fieldDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FieldType.FILL_COLOR.Name, result.FieldType.Name);
        Assert.Equal("TestScope", result.Scope);

        Assert.IsType<Color>(result.Value); 
        // special compare, because we only store the ARGB value
        Assert.True(DtoHelper.CompareColorValue(redColor, (Color)result.Value),
            $"The color values are different. expected:{DtoHelper.ArgbString(redColor)} result:{DtoHelper.ArgbString((Color)result.Value)}");
    }

}