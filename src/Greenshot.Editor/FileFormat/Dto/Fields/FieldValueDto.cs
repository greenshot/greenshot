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
using System.Text.Json.Serialization;

namespace Greenshot.Editor.FileFormat.Dto.Fields;

/// <summary>
/// This is a specific Dto to support serialization for the possible types in <see cref="Field.Value"/>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NullFieldValueDto),"Null")]
[JsonDerivedType(typeof(IntFieldValueDto), "Int")]
[JsonDerivedType(typeof(StringFieldValueDto), "String")]
[JsonDerivedType(typeof(BoolFieldValueDto), "Bool")]
[JsonDerivedType(typeof(SingleFieldValueDto), "Single")]
[JsonDerivedType(typeof(DoubleFieldValueDto), "Double")]
[JsonDerivedType(typeof(DecimalFieldValueDto), "Decimal")]
[JsonDerivedType(typeof(ColorFieldValueDto), "Color")]
[JsonDerivedType(typeof(ArrowHeadCombinationFieldValueDto), "ArrowHeadCombination")]
[JsonDerivedType(typeof(FieldFlagFieldValueDto), "FieldFlag")]
[JsonDerivedType(typeof(PreparedFilterFieldValueDto), "PreparedFilter")]
[JsonDerivedType(typeof(StringAlignmentFieldValueDto), "StringAlignment")]
public abstract class FieldValueDto
{
    public abstract object GetValue();
}
