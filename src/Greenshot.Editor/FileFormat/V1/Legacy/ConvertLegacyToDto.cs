/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
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
            LegacyCursorContainer cursor => ToDto(cursor),
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

    public static DrawableContainerDto ToDto(LegacyRectangleContainer container)
    {
        var kind = ReadMetadata<string>(container, LegacyV1ContainerMetadata.ContainerKind, required: false);
        if (kind == LegacyV1ContainerMetadata.CutMark)
        {
            var style = ReadStyle(container);
            return new CutMarkContainerDto
            {
                Left = container.Left,
                Top = container.Top,
                Width = container.Width,
                Height = container.Height,
                Fields = ToContainerFields(container, style),
                Horizontal = ReadMetadata<bool>(container, LegacyV1ContainerMetadata.Horizontal),
                Seed = ReadMetadata<int>(container, LegacyV1ContainerMetadata.Seed),
                ShadowDarkness = ReadMetadata<float>(container, LegacyV1ContainerMetadata.ShadowDarkness),
                ShadowSize = ReadMetadata<int>(container, LegacyV1ContainerMetadata.ShadowSize),
                ShadowOffsetX = ReadMetadata<int>(container, LegacyV1ContainerMetadata.ShadowOffsetX),
                ShadowOffsetY = ReadMetadata<int>(container, LegacyV1ContainerMetadata.ShadowOffsetY)
            };
        }

        if (kind == LegacyV1ContainerMetadata.TornEdge)
        {
            var style = ReadStyle(container);
            return new TornEdgeContainerDto
            {
                Left = container.Left,
                Top = container.Top,
                Width = container.Width,
                Height = container.Height,
                Fields = ToContainerFields(container, style),
                Seed = ReadMetadata<int>(container, LegacyV1ContainerMetadata.Seed),
                ShadowDarkness = ReadMetadata<float>(container, LegacyV1ContainerMetadata.ShadowDarkness),
                ShadowSize = ReadMetadata<int>(container, LegacyV1ContainerMetadata.ShadowSize),
                ShadowOffsetX = ReadMetadata<int>(container, LegacyV1ContainerMetadata.ShadowOffsetX),
                ShadowOffsetY = ReadMetadata<int>(container, LegacyV1ContainerMetadata.ShadowOffsetY),
                Edges =
                [
                    ReadMetadata<bool>(container, LegacyV1ContainerMetadata.EdgeTop),
                    ReadMetadata<bool>(container, LegacyV1ContainerMetadata.EdgeRight),
                    ReadMetadata<bool>(container, LegacyV1ContainerMetadata.EdgeBottom),
                    ReadMetadata<bool>(container, LegacyV1ContainerMetadata.EdgeLeft)
                ],
                Margins =
                [
                    ReadMetadata<int>(container, LegacyV1ContainerMetadata.MarginTop),
                    ReadMetadata<int>(container, LegacyV1ContainerMetadata.MarginRight),
                    ReadMetadata<int>(container, LegacyV1ContainerMetadata.MarginBottom),
                    ReadMetadata<int>(container, LegacyV1ContainerMetadata.MarginLeft)
                ]
            };
        }

        return new RectangleContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields)
        };
    }

    private static List<FieldDto> ToContainerFields(LegacyRectangleContainer container, CutMarkStyle style)
    {
        var fields = ToFieldDtos(container.Fields?.Where(field => field.Scope != LegacyV1ContainerMetadata.Scope).ToList());
        fields.Add(new FieldDto
        {
            FieldTypeName = FieldType.CUT_MARK_STYLE.Name,
            Scope = ReadMetadata<string>(container, LegacyV1ContainerMetadata.StyleScope),
            Value = new CutMarkStyleFieldValueDto { Value = style }
        });
        return fields;
    }

    private static CutMarkStyle ReadStyle(LegacyRectangleContainer container)
    {
        var style = (CutMarkStyle)ReadMetadata<int>(container, LegacyV1ContainerMetadata.Style);
        if (!Enum.IsDefined(typeof(CutMarkStyle), style))
        {
            throw new InvalidDataException($"The V1 container metadata contains an unsupported cut mark style value {(int)style}.");
        }

        return style;
    }

    private static T ReadMetadata<T>(LegacyRectangleContainer container, string name, bool required = true)
    {
        var field = container.Fields?.FirstOrDefault(candidate =>
            candidate.Scope == LegacyV1ContainerMetadata.Scope && candidate.FieldType?.Name == name);
        if (field?.Value is T value)
        {
            return value;
        }

        if (!required && field == null)
        {
            return default;
        }

        throw new InvalidDataException($"The V1 container metadata field '{name}' is missing or has an invalid value.");
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

    public static CursorContainerDto ToDto(LegacyCursorContainer container)
    {
        return new CursorContainerDto
        {
            Left = container.Left,
            Top = container.Top,
            Width = container.Width,
            Height = container.Height,
            Fields = ToFieldDtos(container.Fields),
            ColorLayer  = container.savedCursor?.ColorLayer != null ? ImageToByteArray(container.savedCursor.ColorLayer) : null,
            MaskLayer  = container.savedCursor?.MaskLayer != null ? ImageToByteArray(container.savedCursor.MaskLayer) : null,
            CursorWidth = container.savedCursor?.SizeWidth ?? 0,
            CursorHeight = container.savedCursor?.SizeHeight ?? 0,
            HotspotX = container.savedCursor?.HotspotX ?? 0,
            HotspotY = container.savedCursor?.HotspotY ?? 0 
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
        using var ms = RecyclableMemoryStreamFactory.GetStream("ConvertLegacyToDto.ImageToByteArray");
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
    private static byte[] IconToByteArray(Icon icon)
    {
        if (icon == null) return null;
        using var ms = RecyclableMemoryStreamFactory.GetStream("ConvertLegacyToDto.IconToByteArray");
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

