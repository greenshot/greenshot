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
using Xunit;
using static Greenshot.Editor.Drawing.ArrowContainer;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.DomainToDto;

[Collection("DefaultCollection")]
public class ConvertFieldToDtoTests
{
    /// <summary>
    /// A collectioan of all supported value types for <see cref="Field"/>.
    /// </summary>
    public static IEnumerable<object[]> GetFieldTestData()
    {
        yield return [FieldType.FONT_BOLD, typeof(bool), true];
        yield return [FieldType.FONT_FAMILY, typeof(string), "Arial"];
        yield return [FieldType.LINE_THICKNESS, typeof(int), 42];
        yield return [FieldType.PREVIEW_QUALITY, typeof(float), 3.14f];
        yield return [FieldType.PREVIEW_QUALITY, typeof(double), 3.14d];
        yield return [FieldType.PREVIEW_QUALITY, typeof(decimal), 3.14m];
        yield return [FieldType.ARROWHEADS, typeof(ArrowHeadCombination), ArrowHeadCombination.END_POINT];
        yield return [FieldType.TEXT_HORIZONTAL_ALIGNMENT, typeof(StringAlignment), StringAlignment.Center];
        yield return [FieldType.ARROWHEADS, typeof(PreparedFilter), PreparedFilter.TEXT_HIGHTLIGHT];
        yield return [FieldType.FLAGS, typeof(FieldFlag), FieldFlag.COUNTER];
    }

    [Theory]
    [MemberData(nameof(GetFieldTestData))]
    public void ConvertDomainToDto_Field_Returns_FieldDto(IFieldType field, Type valueType, object value)
    {
        // Arrange
        var original = new Field(field, "TestScope") { Value = value };

        // Act
        var result = ConvertDomainToDto.ToDto(original);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(field.Name, result.FieldTypeName);
        Assert.Equal("TestScope", result.Scope);
        Assert.IsType(valueType, result.Value.GetValue());
        Assert.Equal(value, result.Value.GetValue());
    }

    /// <summary>
    /// trivial test to ensure that null Field returns null DTO.
    /// </summary>
    [Fact]
    public void ToDto_NullField_ReturnsNull()
    {
        // Arrange
        Field domain = null;
        // Act
        // ReSharper disable once ExpressionIsAlwaysNull
        var result = ConvertDomainToDto.ToDto(domain);
        // Assert
        Assert.Null(result);
    }

}