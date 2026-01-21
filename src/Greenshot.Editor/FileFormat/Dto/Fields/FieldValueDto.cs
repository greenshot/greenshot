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
using Greenshot.Editor.Drawing.Fields;
using MessagePack;
using System.Text.Json.Serialization;

namespace Greenshot.Editor.FileFormat.Dto.Fields;

/// <summary>
/// This is a specific Dto to support serialization for the possible types in <see cref="Field.Value"/>
/// </summary>
[MessagePackObject]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NullFieldValueDto), nameof(NullFieldValueDto))]
[JsonDerivedType(typeof(IntFieldValueDto), nameof(IntFieldValueDto))]
[JsonDerivedType(typeof(StringFieldValueDto), nameof(StringFieldValueDto))]
[JsonDerivedType(typeof(BoolFieldValueDto), nameof(BoolFieldValueDto))]
[JsonDerivedType(typeof(SingleFieldValueDto), nameof(SingleFieldValueDto))]
[JsonDerivedType(typeof(DoubleFieldValueDto), nameof(DoubleFieldValueDto))]
[JsonDerivedType(typeof(DecimalFieldValueDto), nameof(DecimalFieldValueDto))]
[JsonDerivedType(typeof(ColorFieldValueDto), nameof(ColorFieldValueDto))]
[JsonDerivedType(typeof(ArrowHeadCombinationFieldValueDto), nameof(ArrowHeadCombinationFieldValueDto))]
[JsonDerivedType(typeof(FieldFlagFieldValueDto), nameof(FieldFlagFieldValueDto))]
[JsonDerivedType(typeof(PreparedFilterFieldValueDto), nameof(PreparedFilterFieldValueDto))]
[JsonDerivedType(typeof(StringAlignmentFieldValueDto), nameof(StringAlignmentFieldValueDto))]
[Union(0, typeof(NullFieldValueDto))]
[Union(1, typeof(IntFieldValueDto))]
[Union(2, typeof(StringFieldValueDto))]
[Union(3, typeof(BoolFieldValueDto))]
[Union(4, typeof(SingleFieldValueDto))]
[Union(5, typeof(DoubleFieldValueDto))]
[Union(6, typeof(DecimalFieldValueDto))]
[Union(7, typeof(ColorFieldValueDto))]
[Union(8, typeof(ArrowHeadCombinationFieldValueDto))]
[Union(9, typeof(FieldFlagFieldValueDto))]
[Union(10, typeof(PreparedFilterFieldValueDto))]
[Union(11, typeof(StringAlignmentFieldValueDto))]
public abstract class FieldValueDto
{
    public abstract object GetValue();
}
