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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Converts current editor containers to the restricted compatibility model used by the V1 writer.
/// </summary>
internal static class ConvertDomainToLegacy
{
    internal static LegacyDrawableContainerList ToLegacy(IEnumerable<IDrawableContainer> containers)
    {
        var result = new LegacyDrawableContainerList();
        foreach (var container in containers)
        {
            var dto = ConvertDomainToDto.ToDto(container);
            var legacyContainer = ToLegacyContainer(dto);
            CopyChildren(container as DrawableContainer, legacyContainer);
            result.Add(legacyContainer);
        }

        return result;
    }

    private static LegacyDrawableContainer ToLegacyContainer(DrawableContainerDto dto)
    {
        LegacyDrawableContainer legacy = dto switch
        {
            ArrowContainerDto => new LegacyArrowContainer(),
            LineContainerDto => new LegacyLineContainer(),
            RectangleContainerDto => new LegacyRectangleContainer(),
            EllipseContainerDto => new LegacyEllipseContainer(),
            HighlightContainerDto => new LegacyHighlightContainer(),
            ObfuscateContainerDto => new LegacyObfuscateContainer(),
            TextContainerDto => new LegacyTextContainer(),
            ImageContainerDto => new LegacyImageContainer(),
            IconContainerDto => new LegacyIconContainer(),
            CursorContainerDto => new LegacyCursorContainer(),
            SpeechbubbleContainerDto => new LegacySpeechbubbleContainer(),
            FreehandContainerDto => new LegacyFreehandContainer(),
            MetafileContainerDto => new LegacyMetafileContainer(),
            SvgContainerDto => new LegacySvgContainer(),
            EmojiContainerDto => new LegacyEmojiContainer(),
            StepLabelContainerDto => new LegacyStepLabelContainer(),
            CutMarkContainerDto => new LegacyRectangleContainer(),
            TornEdgeContainerDto => new LegacyRectangleContainer(),
            _ => throw new NotSupportedException($"The V1 .greenshot format does not support {dto?.GetType().Name ?? "null"}.")
        };

        legacy.Left = dto.Left;
        legacy.Top = dto.Top;
        legacy.Width = dto.Width;
        legacy.Height = dto.Height;
        var fields = dto.Fields;
        if (dto is CutMarkContainerDto or TornEdgeContainerDto)
        {
            fields = dto.Fields?.Where(field => field.FieldTypeName != FieldType.CUT_MARK_STYLE.Name).ToList();
        }
        legacy.Fields = ToLegacyFields(fields);

        switch (dto)
        {
            case CutMarkContainerDto cutMark:
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ContainerKind, LegacyV1ContainerMetadata.CutMark);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.Horizontal, cutMark.Horizontal);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.Seed, cutMark.Seed);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.Style, (int)GetCutMarkStyle(dto));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.StyleScope, GetCutMarkStyleScope(dto));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowDarkness, cutMark.ShadowDarkness);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowSize, cutMark.ShadowSize);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowOffsetX, cutMark.ShadowOffsetX);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowOffsetY, cutMark.ShadowOffsetY);
                break;
            case TornEdgeContainerDto tornEdge:
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ContainerKind, LegacyV1ContainerMetadata.TornEdge);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.Seed, tornEdge.Seed);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.Style, (int)GetCutMarkStyle(dto));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.StyleScope, GetCutMarkStyleScope(dto));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowDarkness, tornEdge.ShadowDarkness);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowSize, tornEdge.ShadowSize);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowOffsetX, tornEdge.ShadowOffsetX);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.ShadowOffsetY, tornEdge.ShadowOffsetY);
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.EdgeTop, tornEdge.Edges.ElementAtOrDefault(0));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.EdgeRight, tornEdge.Edges.ElementAtOrDefault(1));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.EdgeBottom, tornEdge.Edges.ElementAtOrDefault(2));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.EdgeLeft, tornEdge.Edges.ElementAtOrDefault(3));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.MarginTop, tornEdge.Margins.ElementAtOrDefault(0));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.MarginRight, tornEdge.Margins.ElementAtOrDefault(1));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.MarginBottom, tornEdge.Margins.ElementAtOrDefault(2));
                AddMetadataField(legacy.Fields, LegacyV1ContainerMetadata.MarginLeft, tornEdge.Margins.ElementAtOrDefault(3));
                break;
            case TextContainerDto text:
                ((LegacyTextContainer)legacy).Text = text.Text;
                break;
            case ImageContainerDto image:
                ((LegacyImageContainer)legacy).Image = ToBitmap(image.Image);
                break;
            case IconContainerDto icon:
                ((LegacyIconContainer)legacy).Icon = ToIcon(icon.Icon);
                break;
            case CursorContainerDto cursor:
                ((LegacyCursorContainer)legacy).savedCursor = new LegacyCaptureCursorSerializationWrapper
                {
                    ColorLayer = ToBitmap(cursor.ColorLayer),
                    MaskLayer = ToBitmap(cursor.MaskLayer),
                    SizeWidth = cursor.CursorWidth,
                    SizeHeight = cursor.CursorHeight,
                    HotspotX = cursor.HotspotX,
                    HotspotY = cursor.HotspotY
                };
                break;
            case SpeechbubbleContainerDto speechbubble:
                ((LegacySpeechbubbleContainer)legacy).Text = speechbubble.Text;
                ((LegacySpeechbubbleContainer)legacy).StoredTargetGripperLocation = new Point(
                    speechbubble.StoredTargetGripperLocation.X,
                    speechbubble.StoredTargetGripperLocation.Y);
                break;
            case FreehandContainerDto freehand:
                ((LegacyFreehandContainer)legacy).CapturePoints = freehand.CapturePoints.ConvertAll(point => new Point(point.X, point.Y));
                break;
            case MetafileContainerDto metafile:
                ((LegacyMetafileContainer)legacy).RotationAngle = metafile.RotationAngle;
                ((LegacyMetafileContainer)legacy).MetafileContent = ToMemoryStream(metafile.MetafileData);
                break;
            case SvgContainerDto svg:
                ((LegacySvgContainer)legacy).RotationAngle = svg.RotationAngle;
                ((LegacySvgContainer)legacy).SvgContent = ToMemoryStream(svg.SvgData);
                break;
            case EmojiContainerDto emoji:
                ((LegacyEmojiContainer)legacy).RotationAngle = emoji.RotationAngle;
                ((LegacyEmojiContainer)legacy).Emoji = emoji.Emoji;
                break;
            case StepLabelContainerDto stepLabel:
                ((LegacyStepLabelContainer)legacy).Number = stepLabel.Number;
                ((LegacyStepLabelContainer)legacy).CounterStart = stepLabel.CounterStart;
                break;
        }

        return legacy;
    }

    private static CutMarkStyle GetCutMarkStyle(DrawableContainerDto dto)
    {
        var styleField = dto.Fields?.FirstOrDefault(field => field.FieldTypeName == FieldType.CUT_MARK_STYLE.Name);
        return styleField?.Value?.GetValue() is CutMarkStyle style ? style : CutMarkStyle.None;
    }

    private static string GetCutMarkStyleScope(DrawableContainerDto dto) =>
        dto.Fields?.FirstOrDefault(field => field.FieldTypeName == FieldType.CUT_MARK_STYLE.Name)?.Scope ?? string.Empty;

    private static void AddMetadataField(IList<LegacyField> fields, string name, object value)
    {
        var field = new LegacyField
        {
            FieldType = new LegacyFieldType { Name = name },
            Scope = LegacyV1ContainerMetadata.Scope
        };
        field.ResetValue(value);
        fields.Add(field);
    }

    private static void CopyChildren(DrawableContainer source, LegacyDrawableContainer destination)
    {
        if (source == null || destination is not (LegacyHighlightContainer or LegacyObfuscateContainer))
        {
            return;
        }

        foreach (var child in source.Children)
        {
            LegacyFieldHolder legacyChild = child switch
            {
                HighlightFilter => new LegacyHighlightFilter(),
                BlurFilter => new LegacyBlurFilter(),
                BrightnessFilter => new LegacyBrightnessFilter(),
                GrayscaleFilter => new LegacyGrayscaleFilter(),
                MagnifierFilter => new LegacyMagnifierFilter(),
                PixelizationFilter => new LegacyPixelizationFilter(),
                _ => throw new NotSupportedException($"The V1 .greenshot format does not support filter {child.GetType().Name}.")
            };

            legacyChild.Fields = ToLegacyFields(child.GetFields());
            destination.Children.Add(legacyChild);
        }
    }

    private static IList<LegacyField> ToLegacyFields(IList<Greenshot.Editor.FileFormat.Dto.Fields.FieldDto> fields)
    {
        var result = new List<LegacyField>(fields?.Count ?? 0);
        if (fields == null)
        {
            return result;
        }

        foreach (var field in fields)
        {
            var legacyField = new LegacyField
            {
                FieldType = new LegacyFieldType { Name = field.FieldTypeName },
                Scope = field.Scope
            };
            legacyField.ResetValue(ConvertDtoToDomain.ConvertDtoToValue(field.Value));
            result.Add(legacyField);
        }

        return result;
    }

    private static IList<LegacyField> ToLegacyFields(IList<IField> fields)
    {
        var dtoFields = new List<Greenshot.Editor.FileFormat.Dto.Fields.FieldDto>(fields?.Count ?? 0);
        if (fields != null)
        {
            foreach (var field in fields)
            {
                dtoFields.Add(ConvertDomainToDto.ToDto(field));
            }
        }

        return ToLegacyFields(dtoFields);
    }

    private static Bitmap ToBitmap(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        using var stream = new MemoryStream(data, false);
        using var image = Image.FromStream(stream, true, true);
        return new Bitmap(image);
    }

    private static Icon ToIcon(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        using var stream = new MemoryStream(data, false);
        return new Icon(stream);
    }

    private static MemoryStream ToMemoryStream(byte[] data) => data == null ? null : new MemoryStream(data, false);
}
