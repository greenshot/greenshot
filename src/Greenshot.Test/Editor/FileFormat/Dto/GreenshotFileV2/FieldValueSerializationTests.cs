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
using Greenshot.Editor.FileFormat.V2;
using Xunit;
using static Greenshot.Editor.Drawing.ArrowContainer;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Test.Editor.FileFormat.Dto.GreenshotFileV2;

[Collection("DefaultCollection")]
public class FieldValueSerializationTests
{
    /// <summary>
    /// A collectioan of all supported value types for <see cref="Field"/>.
    /// </summary>
    public static IEnumerable<object[]> GetValueTestData()
    {
        yield return [typeof(bool), true];
        yield return [typeof(string), "Arial"];
        yield return [typeof(int), 42];
        yield return [typeof(float), 3.14f];
        yield return [typeof(double), 3.14d];
        yield return [typeof(decimal), 3.14m];
        yield return [typeof(ArrowHeadCombination), ArrowHeadCombination.END_POINT];
        yield return [typeof(PreparedFilter), PreparedFilter.BLUR];
        yield return [typeof(StringAlignment), StringAlignment.Center];
        yield return [typeof(FieldFlag), FieldFlag.CONFIRMABLE];
    }

    /// <summary>
    /// Convert a value (basic types, that are allowed in a <see cref="Field.Value"/>) to <see cref="FieldValueDto"/>, serialize , deserialize and convert back to domain object
    /// </summary>
    /// <remarks>Different from <see cref="FieldSerializationTests"/> this method tests <see cref="ConvertDomainToDto.ConvertValueToDto"/> and <see cref="ConvertDtoToDomain.ConvertDtoToValue"/>
    /// instead of <see cref="ConvertDomainToDto.ToDto(IField)"/> and <see cref="ConvertDtoToDomain.ToDomain(FieldDto)"/></remarks>
    /// <param name="valueType"></param>
    /// <param name="value"></param>
    [Theory]
    [MemberData(nameof(GetValueTestData))]
    public void SerializeDeserialize_FieldValue(Type valueType, object value)
    {
        // Act
        var dto = ConvertDomainToDto.ConvertValueToDto(value);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<FieldValueDto>(serialized);
        var result = ConvertDtoToDomain.ConvertDtoToValue(deserializedDto);

        // Assert
        Assert.IsType(valueType, result);
        Assert.Equal(value, result);
    }

    /// <summary>
    /// Convert a <see cref="Color"/> to <see cref="FieldValueDto"/>, serialize , deserialize and convert back to domain object
    /// </summary>
    /// <remarks>It's a dedicated test because <see cref="Color"/> needs a special compare.</remarks>
    [Fact]
    public void SerializeDeserialize_ColorFieldValue()
    {
        // Arrange
        var redColor = Color.Red;

        // Act
        var dto = ConvertDomainToDto.ConvertValueToDto(redColor);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<FieldValueDto>(serialized);
        var result = ConvertDtoToDomain.ConvertDtoToValue(deserializedDto);

        // Assert
        Assert.IsType<Color>( result);
        // special compare, because we only store the ARGB value
        Assert.True(DtoHelper.CompareColorValue(redColor, (Color)result),
            $"The color values are different. expected:{DtoHelper.ArgbString(redColor)} result:{DtoHelper.ArgbString((Color)result)}");
    }

    [Fact]
    public void SerializeDeserialize_NullFieldValue()
    {
        // Act
        var dto = ConvertDomainToDto.ConvertValueToDto(null);
        var serialized = V2Helper.SerializeDto(dto);
        var deserializedDto = V2Helper.DeserializeDto<FieldValueDto>(serialized);
        var result = ConvertDtoToDomain.ConvertDtoToValue(deserializedDto);

        // Assert
        Assert.Null(result);
    }
}