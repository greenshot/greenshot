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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat.Dto.Fields;
using MessagePack;

namespace Greenshot.Editor.FileFormat.Dto.Container;

/// <summary>
/// Data transfer object to serialize <see cref="DrawableContainer"/> objects.
/// Simplified Version that supports Properties from <see cref="AbstractFieldHolderWithChildren"/> and <see cref="AbstractFieldHolder"/> as well.
/// Ignore <see cref="AbstractFieldHolderWithChildren.Children"/> because it is only used for filters at the moment and all field values from the filters are already in <see cref="Fields"/>.
/// </summary>
[Union(0, typeof(ImageContainerDto))]
[Union(1, typeof(LineContainerDto))]
[Union(2, typeof(RectangleContainerDto))]
[Union(3, typeof(IconContainerDto))]
[Union(4, typeof(TextContainerDto))]
[Union(5, typeof(SpeechbubbleContainerDto))]
[Union(6, typeof(ArrowContainerDto))]
[Union(7, typeof(CursorContainerDto))]
[Union(8, typeof(EllipseContainerDto))]
[Union(9, typeof(FreehandContainerDto))]
[Union(10, typeof(HighlightContainerDto))]
[Union(11, typeof(MetafileContainerDto))]
[Union(12, typeof(ObfuscateContainerDto))]
[Union(13, typeof(StepLabelContainerDto))]
[Union(14, typeof(SvgContainerDto))]
[Union(15, typeof(EmojiContainerDto))]
[MessagePackObject]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ImageContainerDto), "Image")]
[JsonDerivedType(typeof(LineContainerDto), "Line")]
[JsonDerivedType(typeof(RectangleContainerDto), "Rectangle")]
[JsonDerivedType(typeof(IconContainerDto), "Icon")]
[JsonDerivedType(typeof(TextContainerDto), "Text")]
[JsonDerivedType(typeof(SpeechbubbleContainerDto), "Speechbubble")]
[JsonDerivedType(typeof(ArrowContainerDto), "Arrow")]
[JsonDerivedType(typeof(CursorContainerDto), "Cursor")]  
[JsonDerivedType(typeof(EllipseContainerDto), "Ellipse")]
[JsonDerivedType(typeof(FreehandContainerDto), "Freehand")]
[JsonDerivedType(typeof(HighlightContainerDto), "Highlight")]
[JsonDerivedType(typeof(MetafileContainerDto), "Metafile")]
[JsonDerivedType(typeof(ObfuscateContainerDto), "Obfuscate" )]
[JsonDerivedType(typeof(StepLabelContainerDto), "StepLbel")]
[JsonDerivedType(typeof(SvgContainerDto), "Svg")]
[JsonDerivedType(typeof(EmojiContainerDto), "Emoji")]
public abstract class DrawableContainerDto 
{
  
    [Key(10)]
    public int Left { get; set; }
    [Key(11)]
    public int Top { get; set; }
    [Key(12)]
    public int Width { get; set; }
    [Key(13)]
    public int Height { get; set; }

    [Key(14)]
    public List<FieldDto> Fields { get; set; } = [];
}


