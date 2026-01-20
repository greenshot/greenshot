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
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Provides methods for converting legacy drawable container objects into their corresponding Data Transfer Object
/// (DTO) representations.
/// </summary>
/// <remarks>This class contains a collection of static ToDto() methods that handle the transformation</remarks>
internal static class ConvertLegacyToDto
{
    public static DrawableContainerDto ToDto(LegacyDrawableContainer container)
    {
        return container switch
        {
            LegacyArrowContainer arrow => ToDto(arrow),
            LegacyLineContainer line => ToDto(line),
            LegacyRectangleContainer rect => ToDto(rect),
            LegacyEllipseContainer ellipse => ToDto(ellipse),
            LegacyHighlightContainer highlight => ToDto(highlight),
            LegacyObfuscateContainer obfuscate => ToDto(obfuscate),
            LegacyTextContainer text => ToDto(text),
            LegacyImageContainer image => ToDto(image),
            LegacyIconContainer icon => ToDto(icon),
            LegacySpeechbubbleContainer speech => ToDto(speech),
            LegacyFreehandContainer freehand => ToDto(freehand),
            LegacyMetafileContainer meta => ToDto(meta),
            LegacySvgContainer svg => ToDto(svg),
            LegacyEmojiContainer emoji => ToDto(emoji),
            LegacyStepLabelContainer step => ToDto(step),
            _ => throw new NotSupportedException($"Cannot convert unknown container type {container.GetType()} to a concrete ContainerDto."),
        };
    }

    public static DrawableContainerListDto ToDto(LegacyDrawableContainerList containerList)
    {
        var dto = new DrawableContainerListDto { ContainerList = new List<DrawableContainerDto>() };
        foreach (var w in containerList)
        {
            dto.ContainerList.Add(ToDto(w));
        }
        return dto;
    }

    private static List<FieldDto> ToFieldDtos(IList<LegacyField> fields)
    {
        var list = new List<FieldDto>();
        if (fields == null) return list;
        list.AddRange(fields.Select(f => new FieldDto { FieldTypeName = f.FieldType?.Name, Scope = f.Scope, Value = ConvertValueToDto(f.Value) }));
        return list;
    }

    public static ArrowContainerDto ToDto(LegacyArrowContainer container)
    {
        return new ArrowContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    public static LineContainerDto ToDto(LegacyLineContainer container)
    {
        return new LineContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    public static RectangleContainerDto ToDto(LegacyRectangleContainer container)
    {
        return new RectangleContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    public static EllipseContainerDto ToDto(LegacyEllipseContainer container)
    {
        return new EllipseContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    public static HighlightContainerDto ToDto(LegacyHighlightContainer container)
    {
        return new HighlightContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    public static ObfuscateContainerDto ToDto(LegacyObfuscateContainer container)
    {
        return new ObfuscateContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    public static TextContainerDto ToDto(LegacyTextContainer container)
    {
        return new TextContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            Text = container.Text
        };
    }

    public static ImageContainerDto ToDto(LegacyImageContainer container)
    {
        return new ImageContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            Image = container.Image != null ? ImageToByteArray(container.Image) : null
        };
    }

    public static IconContainerDto ToDto(LegacyIconContainer container)
    {
        return new IconContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            Icon = container.Icon != null ? IconToByteArray(container.Icon) : null
        };
    }

    public static SpeechbubbleContainerDto ToDto(LegacySpeechbubbleContainer container)
    {
        return new SpeechbubbleContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            Text = container.Text,
            StoredTargetGripperLocation = new PointDto { X = container.StoredTargetGripperLocation.X, Y = container.StoredTargetGripperLocation.Y }
        };
    }

    public static FreehandContainerDto ToDto(LegacyFreehandContainer container)
    {
        var points = new List<PointDto>();
        if (container.CapturePoints != null)
        {
            foreach (var p in container.CapturePoints)
            {
                points.Add(new PointDto { X = p.X, Y = p.Y });
            }
        }
        return new FreehandContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            CapturePoints = points
        };
    }

    public static MetafileContainerDto ToDto(LegacyMetafileContainer container)
    {
        return new MetafileContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            RotationAngle = container.RotationAngle,
            MetafileData = container.MetafileContent?.ToArray()
        };
    }

    public static SvgContainerDto ToDto(LegacySvgContainer container)
    {
        return new SvgContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            RotationAngle = container.RotationAngle,
            SvgData = container.SvgContent?.ToArray()
        };
    }

    public static EmojiContainerDto ToDto(LegacyEmojiContainer container)
    {
        return new EmojiContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            RotationAngle = container.RotationAngle,
            Emoji = container.Emoji
        };
    }

    public static StepLabelContainerDto ToDto(LegacyStepLabelContainer container)
    {
        return new StepLabelContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            Number = container.Number,
            CounterStart = container.CounterStart
        };
    }

    private static byte[] ImageToByteArray(Image image)
    {
        if (image == null) return null;
        using var ms = new MemoryStream();
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
    private static byte[] IconToByteArray(Icon icon)
    {
        if (icon == null) return null;
        using var ms = new MemoryStream();
        icon.Save(ms);
        return ms.ToArray();
    }

    private static FieldValueDto ConvertValueToDto(object value)
    {
        if (value == null) return new NullFieldValueDto();
        return value switch
        {
            int i => new IntFieldValueDto { Value = i },
            string s => new StringFieldValueDto { Value = s },
            bool b => new BoolFieldValueDto { Value = b },
            float f => new SingleFieldValueDto { Value = f },
            double d => new DoubleFieldValueDto { Value = d },
            decimal m => new DecimalFieldValueDto { Value = m },
            Color c => new ColorFieldValueDto { Value = c },
            FilterContainer.PreparedFilter filter => new PreparedFilterFieldValueDto { Value = filter },
            ArrowContainer.ArrowHeadCombination arrowHead => new ArrowHeadCombinationFieldValueDto { Value = arrowHead },
            FieldFlag fieldFlag => new FieldFlagFieldValueDto { Value = fieldFlag },
            StringAlignment alignment => new StringAlignmentFieldValueDto { Value = alignment },
            _ => throw new ArgumentException($"Unsupported type: {value.GetType()}")
        };
    }
}

