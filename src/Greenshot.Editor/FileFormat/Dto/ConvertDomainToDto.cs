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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.FileFormat.Dto.Container;
using Greenshot.Editor.FileFormat.Dto.Fields;
using static Greenshot.Editor.Drawing.ArrowContainer;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// Provides methods to convert domain objects, mainly drawable container and fields, into their corresponding Data Transfer Object (DTO) representations.
/// </summary>
/// <remarks>This class contains a collection of static ToDto() methods that handle the transformation</remarks>
public static class ConvertDomainToDto
{

    public static GreenshotFileDto ToDto(GreenshotFile domain)
    {
        if (domain == null) return null;

        return new GreenshotFileDto
        {
            ContainerList = ToDto(domain.ContainerList),
            Image = ImageToByteArray(domain.Image),
            RenderedImage = ImageToByteArray(domain.RenderedImage),
            MetaInformation = ToDto(domain.MetaInformation)
        };
    }

    public static GreenshotFileMetaInformationDto ToDto(GreenshotFileMetaInformation domain)
    {
        if (domain == null) return null;

        return new GreenshotFileMetaInformationDto
        {
            FormatVersion = domain.FormatVersion,
            SchemaVersion = domain.SchemaVersion,
            SavedByGreenshotVersion = domain.SavedByGreenshotVersion,
            CaptureDate = domain.CaptureDate,
            CaptureSize = domain.CaptureSize
        };
    }

    public static GreenshotTemplateDto ToDto(GreenshotTemplate domain)
    {
        if (domain == null) return null;

        return new GreenshotTemplateDto
        {
            ContainerList = ToDto(domain.ContainerList),
            FormatVersion = domain.FormatVersion,
            SchemaVersion = domain.SchemaVersion
        };
    }

    public static DrawableContainerListDto ToDto(DrawableContainerList domain)
    {
        if (domain == null) return null;

        var dtoList = new DrawableContainerListDto();
        foreach (var item in domain)
        {
            dtoList.ContainerList.Add(ToDto(item));
        }
        return dtoList;
    }

    public static DrawableContainerDto ToDto(IDrawableContainer domain) =>
        domain switch
        {
            null => null,
            ImageContainer imageContainer => ToDto(imageContainer),
            ArrowContainer arrowContainer => ToDto(arrowContainer),
            LineContainer lineContainer => ToDto(lineContainer),
            SpeechbubbleContainer speechbubbleContainer => ToDto(speechbubbleContainer),
            TextContainer textContainer => ToDto(textContainer),
            RectangleContainer rectangleContainer => ToDto(rectangleContainer),
            IconContainer iconContainer => ToDto(iconContainer),
            StepLabelContainer stepLabelContainer => ToDto(stepLabelContainer),
            EllipseContainer ellipseContainer => ToDto(ellipseContainer),
            HighlightContainer highlightContainer => ToDto(highlightContainer),
            ObfuscateContainer obfuscateContainer => ToDto(obfuscateContainer),
            CursorContainer cursorContainer => ToDto(cursorContainer),
            FreehandContainer freehandContainer => ToDto(freehandContainer),
            MetafileContainer metafileContainer => ToDto(metafileContainer),
            SvgContainer svgContainer => ToDto(svgContainer),
            EmojiContainer emojiContainer => ToDto(emojiContainer),
            _ => throw new ArgumentException($"Unsupported IDrawableContainer type: {domain.GetType()}"),
        };

    public static ImageContainerDto ToDto(ImageContainer domain)
    {
        if (domain == null) return null;

        var dto = new ImageContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            Image = ImageToByteArray(domain.Image)
        };
        return dto;
    }

    public static MetafileContainerDto ToDto(MetafileContainer domain)
    {
        if (domain == null) return null;

        var dto = new MetafileContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            MetafileData = domain.MetafileContent.ToArray(),
            RotationAngle = domain.RotationAngle
        };
        return dto;
    }

    public static SvgContainerDto ToDto(SvgContainer domain)
    {
        if (domain == null) return null;

        var dto = new SvgContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            SvgData = domain.SvgContent.ToArray(),
            RotationAngle = domain.RotationAngle
        };
        return dto;
    }
    public static EmojiContainerDto ToDto(EmojiContainer domain)
    {
        if (domain == null) return null;

        var dto = new EmojiContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            Emoji = domain.Emoji,
            RotationAngle = domain.RotationAngle
        };
        return dto;
    }

    public static LineContainerDto ToDto(LineContainer domain)
    {
        if (domain == null) return null;

        var dto = new LineContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static RectangleContainerDto ToDto(RectangleContainer domain)
    {
        if (domain == null) return null;

        var dto = new RectangleContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static TextContainerDto ToDto(TextContainer domain)
    {
        if (domain == null) return null;

        var dto = new TextContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Text = domain.Text,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static SpeechbubbleContainerDto ToDto(SpeechbubbleContainer domain)
    {
        if (domain == null) return null;

        var dto = new SpeechbubbleContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Text = domain.Text,
            StoredTargetGripperLocation = new PointDto { X = domain.StoredTargetGripperLocation.X, Y = domain.StoredTargetGripperLocation.Y },
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static ArrowContainerDto ToDto(ArrowContainer domain)
    {
        if (domain == null) return null;

        var dto = new ArrowContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static IconContainerDto ToDto(IconContainer domain)
    {
        if (domain == null) return null;

        var dto = new IconContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            Icon = IconToByteArray(domain.Icon)
        };
        return dto;
    }

    public static StepLabelContainerDto ToDto(StepLabelContainer domain)
    {
        if (domain == null) return null;

        // recalculate the StepLabel number from parent Surface if it exists
        if (domain.Parent is Surface parentSurface)
        {
            domain.Number = parentSurface.CountStepLabels(domain);
            domain.CounterStart = parentSurface.CounterStart;
        }
        
        var dto = new StepLabelContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            Number = domain.Number,
            CounterStart = domain.CounterStart
        };
        return dto;
    }

    public static EllipseContainerDto ToDto(EllipseContainer domain)
    {
        if (domain == null) return null;

        var dto = new EllipseContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static HighlightContainerDto ToDto(HighlightContainer domain)
    {
        if (domain == null) return null;

        var dto = new HighlightContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static ObfuscateContainerDto ToDto(ObfuscateContainer domain)
    {
        if (domain == null) return null;

        var dto = new ObfuscateContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static CursorContainerDto ToDto(CursorContainer domain)
    {
        if (domain == null) return null;

        var dto = new CursorContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList()
        };
        return dto;
    }

    public static FreehandContainerDto ToDto(FreehandContainer domain)
    {
        if (domain == null) return null;

        var dto = new FreehandContainerDto
        {
            Left = domain.Left,
            Top = domain.Top,
            Width = domain.Width,
            Height = domain.Height,
            Fields = domain.GetFields() == null ? [] : domain.GetFields().Select(ToDto).ToList(),
            CapturePoints = domain.CapturePoints.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
        };
        return dto;
    }

    public static FieldDto ToDto(IField domain)
    {
        if (domain == null) return null;

        return new FieldDto
        {
            FieldTypeName = domain.FieldType.Name,
            Scope = domain.Scope,
            Value = ConvertValueToDto(domain.Value)
        };
    }

    /// <summary>
    /// Converts a given value to its corresponding <see cref="FieldValueDto"/> representation.
    /// </summary>
    /// <remarks>The method is public mainly because of testing.</remarks>
    /// <param name="value">The value to convert.</param>
    /// <returns>A specific subclass of <see cref="FieldValueDto"/> instance representing the provided value.</returns>
    /// <exception cref="ArgumentException">Thrown if the type of <paramref name="value"/> is not supported.</exception>
    public static FieldValueDto ConvertValueToDto(object value) =>
        value switch
        {
            null => new NullFieldValueDto(),
            int intValue => new IntFieldValueDto { Value = intValue },
            string stringValue => new StringFieldValueDto { Value = stringValue },
            bool boolValue => new BoolFieldValueDto { Value = boolValue },
            float singleValue => new SingleFieldValueDto { Value = singleValue },
            double doubleValue => new DoubleFieldValueDto { Value = doubleValue },
            decimal decimalValue => new DecimalFieldValueDto { Value = decimalValue },
            Color colorValue => new ColorFieldValueDto { Value = colorValue },
            ArrowHeadCombination arrowHeadCombinationValue => new ArrowHeadCombinationFieldValueDto { Value = arrowHeadCombinationValue },
            FieldFlag fieldFlagValue => new FieldFlagFieldValueDto { Value = fieldFlagValue },
            PreparedFilter preparedFilterValue => new PreparedFilterFieldValueDto { Value = preparedFilterValue },
            StringAlignment stringAlignmentValue => new StringAlignmentFieldValueDto { Value = stringAlignmentValue },
            _ => throw new ArgumentException($"Unsupported type: {value.GetType()}"),
        };

    /// <summary>
    /// Converts the specified <see cref="Image"/> to a byte array in PNG format.
    /// </summary>
    private static byte[] ImageToByteArray(Image image)
    {
        if (image == null) return null;

        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, ImageFormat.Png);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Converts the specified <see cref="Icon"/> to a byte array representation.
    /// </summary>
    private static byte[] IconToByteArray(Icon icon)
    {
        if (icon == null) return null;

        using var memoryStream = new MemoryStream();
        icon.Save(memoryStream);
        return memoryStream.ToArray();
    }
}