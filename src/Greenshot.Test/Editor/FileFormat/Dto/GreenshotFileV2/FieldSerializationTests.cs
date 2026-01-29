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

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

public class FieldSerializationTests
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
        yield return [FieldType.PREPARED_FILTER_HIGHLIGHT, typeof(PreparedFilter), PreparedFilter.AREA_HIGHLIGHT];
        yield return [FieldType.ARROWHEADS, typeof(ArrowHeadCombination), ArrowHeadCombination.END_POINT];
        yield return [FieldType.FLAGS, typeof(FieldFlag), FieldFlag.CONFIRMABLE];
        yield return [FieldType.TEXT_HORIZONTAL_ALIGNMENT, typeof(StringAlignment), StringAlignment.Center];
    }

    /// <summary>
    /// Convert a <see cref="Field"/> domain object to <see cref="FieldDto"/>, serialize , deserialize and convert back to domain object
    /// </summary>
    /// <remarks>Different from <see cref="FieldSerializationTests"/> this method tests <see cref="ConvertDomainToDto.ToDto(IField)"/> and <see cref="ConvertDtoToDomain.ToDomain(FieldDto)"/>
    /// instead of <see cref="ConvertDomainToDto.ConvertValueToDto"/> and <see cref="ConvertDtoToDomain.ConvertDtoToValue"/></remarks>
    /// <param name="field"></param>
    /// <param name="valueType"></param>
    /// <param name="value"></param>
    [Theory]
    [MemberData(nameof(GetFieldTestData))]
    public void SerializeDeserialize_Field(IFieldType field, Type valueType, object value)
    {
        // Arrange
        var original = new Field(field, "scope") { Value = value };

        // Act
        var dto = ConvertDomainToDto.ToDto(original);
        //var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = dto; // MessagePackSerializer.Deserialize<FieldDto>(serialized);
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto);

        // Assert
        Assert.IsType(valueType, result.Value);
        Assert.Equal(original.FieldType, result.FieldType);
        Assert.Equal(original.Scope, result.Scope);
        Assert.Equal(original.Value, result.Value);
    }

    /// <summary>
    /// Convert a <see cref="Field"/> domain object with a <see cref="Color"/> value to <see cref="FieldDto"/>, serialize , deserialize and convert back to domain object
    /// </summary>
    [Fact]
    public void SerializeDeserialize_ColorField()
    {
        // Arrange
        var redColor = Color.Red;
        var original = new Field(FieldType.LINE_COLOR, "scope") { Value = redColor };

        // Act
        var dto = ConvertDomainToDto.ToDto(original);
        //var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = dto;// MessagePackSerializer.Deserialize<FieldDto>(serialized);
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto);

        // Assert
        Assert.Equal(original.FieldType, result.FieldType);
        Assert.Equal(original.Scope, result.Scope);

        Assert.IsType<Color>(result.Value);
        // special compare, because we only store the ARGB value
        Assert.True(DtoHelper.CompareColorValue(redColor, (Color)result.Value),
            $"The color values are different. expected:{DtoHelper.ArgbString(redColor)} result:{DtoHelper.ArgbString((Color)result.Value)}");
    }

    /// <summary>
    /// Convert a <see cref="Field"/> domain object with a null value to <see cref="FieldDto"/>, serialize, deserialize and convert back to domain object
    /// </summary>
    [Fact]
    public void SerializeDeserialize_NullField()
    {
        // Arrange
        var original = new Field(FieldType.FONT_FAMILY, "scope") { Value = null };

        // Act
        var dto = ConvertDomainToDto.ToDto(original);
        //var serialized = MessagePackSerializer.Serialize(dto);
        var deserializedDto = dto;// MessagePackSerializer.Deserialize<FieldDto>(serialized);
        Assert.Fail("Temporarily disabled serialization test - to be fixed later");
        var result = ConvertDtoToDomain.ToDomain(deserializedDto);

        // Assert
        Assert.Null(result.Value);
    }
}