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
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Fields;
using Xunit;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

public class ConvertValueToDtoTests
{   
    /// <summary>
    /// A collectioan of all supported value types for <see cref="FieldDto"/>.
    /// </summary>
    public static IEnumerable<object[]> GetValueTestData()
    {
        yield return [typeof(int), typeof(IntFieldValueDto), 42];
        yield return [typeof(string), typeof(StringFieldValueDto), "test"];
        yield return [typeof(bool), typeof(BoolFieldValueDto), true];
        yield return [typeof(float), typeof(SingleFieldValueDto), 3.14f];
        yield return [typeof(double), typeof(DoubleFieldValueDto), 3.14d];
        yield return [typeof(decimal), typeof(DecimalFieldValueDto), 3.14m];
        yield return [typeof(ArrowContainer.ArrowHeadCombination), typeof(ArrowHeadCombinationFieldValueDto), ArrowContainer.ArrowHeadCombination.END_POINT];
        yield return [typeof(FilterContainer.PreparedFilter), typeof(PreparedFilterFieldValueDto), FilterContainer.PreparedFilter.AREA_HIGHLIGHT];
        yield return [typeof(StringAlignment), typeof(StringAlignmentFieldValueDto), StringAlignment.Center];
        yield return [typeof(FieldFlag), typeof(FieldFlagFieldValueDto), FieldFlag.CONFIRMABLE];
    }

    /// <summary>
    /// This test ensures that the <see cref="ConvertDomainToDto.ConvertValueToDto"/> method produces
    /// a DTO of the corresponding type and that the DTO encapsulates the original domain value correctly.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetValueTestData))]
    public void ConvertValueToDto_DomainType_Returns_FieldValueDtoType(Type domainValueType, Type dtoType, object value)
    {
        // Act
        var result = ConvertDomainToDto.ConvertValueToDto(value);

        // Assert
        Assert.IsType(dtoType, result);
        Assert.IsType(domainValueType, ((FieldValueDto)result).GetValue());
        Assert.Equal(value, ((FieldValueDto)result).GetValue());
    }

    /// <summary>
    /// Tests that the <see cref="ConvertDomainToDto.ConvertValueToDto"/> method correctly converts a <see
    /// cref="Color"/> value into an instance of <see cref="ColorFieldValueDto"/>.
    /// </summary>
    /// <remarks>It's a dedicated test because <see cref="Color"/> needs a special compare.</remarks>
    [Fact]
    public void ConvertValueToDto_Color_Returns_ColorFieldValueDtoType()
    {
        // Arrange
        Color value = Color.Red;

        // Act
        var result = ConvertDomainToDto.ConvertValueToDto(value);

        // Assert
        Assert.IsType<ColorFieldValueDto>(result);
        Assert.IsType<Color>(((ColorFieldValueDto)result).GetValue());
        var resultColorValue = (Color)((ColorFieldValueDto)result).GetValue();

        // special compare, because we only store the ARGB value
        Assert.True(DtoHelper.CompareColorValue(value, resultColorValue),
            $"The color values are different. expected:{DtoHelper.ArgbString(value)} result:{DtoHelper.ArgbString(resultColorValue)}");
    }

    [Fact]
    public void ConvertValueToDto_NullValue_ReturnsNullFieldValue()
    {
        // Arrange
        object value = null;

        // Act
        // ReSharper disable once ExpressionIsAlwaysNull
        var result = ConvertDomainToDto.ConvertValueToDto(value);

        // Assert
        Assert.IsType<NullFieldValueDto>(result);
    }

    [Fact]
    public void ConvertValueToDto_UnsupportedType_ThrowsArgumentException()
    {
        // Arrange
        var value = new object();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ConvertDomainToDto.ConvertValueToDto(value));
    }
}