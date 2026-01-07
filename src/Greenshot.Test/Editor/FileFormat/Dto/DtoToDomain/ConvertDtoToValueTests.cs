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
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;
using static Greenshot.Editor.Drawing.ArrowContainer;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.DtoToDomain;
public class ConvertDtoToValueTests
{
    /// <summary>
    /// A collectioan of all supported value types for <see cref="FieldDto"/>.
    /// </summary>
    public static IEnumerable<object[]> GetFieldValueTestData()
    {
        yield return [typeof(bool), new BoolFieldValueDto { Value = true }, true];
        yield return [typeof(string), new StringFieldValueDto { Value = "test" }, "test"];
        yield return [typeof(int), new IntFieldValueDto { Value = 42 }, 42];
        yield return [typeof(float), new SingleFieldValueDto { Value = 3.14f }, 3.14f];
        yield return [typeof(double), new DoubleFieldValueDto { Value = 3.14d }, 3.14d];
        yield return [typeof(decimal), new DecimalFieldValueDto { Value = 3.14m }, 3.14m];
        yield return [typeof(ArrowHeadCombination), new ArrowHeadCombinationFieldValueDto { Value = ArrowHeadCombination.END_POINT }, ArrowHeadCombination.END_POINT];
        yield return [typeof(FieldFlag), new FieldFlagFieldValueDto { Value = FieldFlag.CONFIRMABLE }, FieldFlag.CONFIRMABLE];
        yield return [typeof(PreparedFilter), new PreparedFilterFieldValueDto { Value = PreparedFilter.AREA_HIGHLIGHT }, PreparedFilter.AREA_HIGHLIGHT];
        yield return [typeof(StringAlignment), new StringAlignmentFieldValueDto { Value = StringAlignment.Center }, StringAlignment.Center];
    }
    /// <summary>
    /// Verifies that the <see cref="ConvertDtoToDomain.ConvertDtoToValue"/> method correctly converts a <see
    /// cref="FieldValueDto"/> to its corresponding domain value type.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetFieldValueTestData))]
    public void ConvertDtoToValue_FieldValueDtoType_Returns_DomainValueType(Type valueType, FieldValueDto dto, object value)
    {
        // Act
        var result = ConvertDtoToDomain.ConvertDtoToValue(dto);

        // Assert
        Assert.IsType(valueType, result);
        Assert.Equal(value, result);
    }

    /// <summary>
    /// Tests the conversion of a <see cref="ColorFieldValueDto"/> to a <see cref="Color"/> value.
    /// </summary>
    /// <remarks>It's a dedicated test because <see cref="Color"/> needs a special compare.</remarks>
    [Fact]
    public void ConvertDtoToValue_ColorFieldValueDto_ReturnsColor()
    {
        // Arrange
        var redColor = Color.Red;
        var dto = new ColorFieldValueDto { Value = redColor };

        // Act
        var result = ConvertDtoToDomain.ConvertDtoToValue(dto);

        // Assert
        Assert.IsType<Color>(result);

        // special compare, because we only store the ARGB value
        Assert.True(DtoHelper.CompareColorValue(redColor, (Color)result),
            $"The color values are different. expected:{DtoHelper.ArgbString(redColor)} result:{DtoHelper.ArgbString((Color)result)}");
    }

    [Fact]
    public void ConvertDtoToValue_NullFieldValueDto_ReturnsNull()
    {
        // Arrange
        var dto = new NullFieldValueDto();

        // Act
        var result = ConvertDtoToDomain.ConvertDtoToValue(dto);

        // Assert
        Assert.Null(result);
    }
}