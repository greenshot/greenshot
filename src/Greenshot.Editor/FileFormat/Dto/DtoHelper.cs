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
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.Forms;
using MessagePack;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// All DTO classes should not contain any business logic. This applies to helper methods as well
/// So this is the place for them.
/// </summary>
public static class DtoHelper
{
    /// <summary>
    /// Get the value of a field from a <see cref="DrawableContainerDto"/> object. Null if not found.
    /// </summary>
    /// <param name="drawableContainer"></param>
    /// <param name="fieldType"></param>
    /// <returns></returns>
    public static object GetFieldValue(DrawableContainerDto drawableContainer, IFieldType fieldType)
    {
        return drawableContainer.Fields?
            .FirstOrDefault(x => x.FieldTypeName == fieldType.Name)?
            .Value.GetValue();
    }

    /// <summary>
    /// We store Color as an ARGB integer, so we have to compare two colors by their ARGB values
    /// </summary>
    /// <param name="leftColor"></param>
    /// <param name="rightColor"></param>
    /// <returns></returns>
    public static bool CompareColorValue(Color leftColor, Color rightColor)
    {
        return leftColor.ToArgb() == rightColor.ToArgb();
    }

    /// <summary>
    /// Helper method.
    /// <see cref="Color.ToString()"/> hides the ARGB value for KnownColor, so we have to use this method to print ARGB value every time.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ArgbString(Color color) => $"ARGB({color.A}, {color.R}, {color.G}, {color.B})";

    /// <summary>
    /// Serializes a <see cref="DrawableContainerList"/> into a byte array using MessagePack serialization.
    /// </summary>
    /// <remarks>This method converts the DrawableContainerList into a DTO before serializing it.
    /// <br/> It is mainly used for copying the DrawableContainerList to Clipboard with "<c>Greenshot.Editor.FileFormat.Dto.Container.DrawableContainerListDto</c>" as identifier.
    /// See also: <seealso cref="ImageEditorForm.SupportedClipboardFormats"/> </remarks>
    /// <param name="drawableContainerList">The <see cref="DrawableContainerList"/> instance to serialize. Must not be <see langword="null"/>.</param>
    /// <returns>A byte array containing the serialized representation of the <see cref="DrawableContainerListDto"/>.</returns>
    public static byte[] SerializeDrawableContainerList(DrawableContainerList drawableContainerList)
    {
        var dto = ConvertDomainToDto.ToDto(drawableContainerList);
        return MessagePackSerializer.Serialize(dto);
    }

    /// <summary>
    /// Deserializes a byte array into a <see cref="DrawableContainerList"/> using MessagePack serialization.
    /// </summary>
    /// <remarks>It deserializes the byte array into a <see cref="DrawableContainerListDto"/> and then converts it to a <see cref="DrawableContainerList"/>.
    /// <br/> It is mainly used for reading the DrawableContainerList from Clipboard with "<c>Greenshot.Editor.FileFormat.Dto.Container.DrawableContainerListDto</c>" as identifier.
    /// See also: <seealso cref="ImageEditorForm.SupportedClipboardFormats"/>
    /// </remarks>
    /// <param name="data">A byte array containing the serialized representation of the <see cref="DrawableContainerListDto"/>.</param>
    public static DrawableContainerList DeserializeDrawableContainerList(byte[] data)
    {
        var dto = MessagePackSerializer.Deserialize<DrawableContainerListDto>(data);
        return ConvertDtoToDomain.ToDomain(dto);
    }
}