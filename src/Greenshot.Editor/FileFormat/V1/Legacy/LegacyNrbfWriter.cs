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
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Writes the NRBF records used by legacy V1 files without constructing serialized runtime objects.
/// </summary>
internal static class LegacyNrbfWriter
{
    private const byte SerializedStreamHeader = 0;
    private const byte ClassWithMembersAndTypes = 5;
    private const byte SystemClassWithMembersAndTypes = 4;
    private const byte BinaryArray = 7;
    private const byte MemberPrimitiveTyped = 8;
    private const byte MemberReference = 9;
    private const byte ObjectNull = 10;
    private const byte ArraySingleObject = 16;
    private const byte BinaryLibrary = 12;
    private const byte BinaryObjectString = 6;
    private const byte MessageEnd = 11;
    private const string V104Marker = "Greenshot01.04";
    private const byte BinaryTypePrimitive = 0;
    private const byte BinaryTypeString = 1;
    private const byte BinaryTypeSystemClass = 3;
    private const byte BinaryTypeClass = 4;
    private const byte BinaryTypeObject = 2;
    private const byte BinaryTypeObjectArray = 5;
    private const byte BinaryArrayTypeSingle = 0;
    private const byte PrimitiveTypeByte = 2;
    private const byte PrimitiveTypeDouble = 6;
    private const byte PrimitiveTypeInt16 = 7;
    private const byte PrimitiveTypeInt32 = 8;
    private const byte PrimitiveTypeInt64 = 9;
    private const byte PrimitiveTypeBoolean = 1;
    private const byte PrimitiveTypeSingle = 11;

    private const int RootObjectId = 1;
    private const int ParentIdObjectId = 2;
    private const int ItemsObjectId = 3;
    private const int EditorLibraryId = 10;
    private const int BaseLibraryId = 11;

    private static readonly string[] RootMemberNames =
    [
        "<ParentID>k__BackingField",
        "_disposedValue",
        "List`1+_items",
        "List`1+_size",
        "List`1+_version"
    ];

    /// <summary>
    /// Writes a V1.04 drawable-container list containing supported container types.
    /// </summary>
    internal static void WriteEmptyContainerList(Stream destination)
    {
        WriteContainerList(destination, new LegacyDrawableContainerList());
    }

    /// <summary>
    /// Writes the supported V1.04 drawable-container-list records to a stream.
    /// </summary>
    internal static void WriteContainerList(Stream destination, LegacyDrawableContainerList containers)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }
        if (containers == null)
        {
            throw new ArgumentNullException(nameof(containers));
        }
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must support writing.", nameof(destination));
        }
        foreach (var container in containers)
        {
            if (container is not LegacyRectangleContainer && container is not LegacyEllipseContainer && container is not LegacyLineContainer &&
                container is not LegacyArrowContainer && container is not LegacyTextContainer && container is not LegacyFreehandContainer &&
                container is not LegacySpeechbubbleContainer && container is not LegacyImageContainer && container is not LegacyIconContainer &&
                container is not LegacyStepLabelContainer && container is not LegacyHighlightContainer && container is not LegacyObfuscateContainer &&
                container is not LegacySvgContainer && container is not LegacyMetafileContainer && container is not LegacyEmojiContainer &&
                container is not LegacyCursorContainer)
            {
                throw new NotSupportedException($"V1 writing does not support {container?.GetType().FullName ?? "null"} yet.");
            }

            if (container is LegacyStepLabelContainer stepLabel)
            {
                stepLabel.EnsureBackwardCompatibleFields();
            }

            if (container is LegacyHighlightContainer or LegacyObfuscateContainer)
            {
                foreach (var child in container.Children)
                {
                    if (child is not LegacyHighlightFilter && child is not LegacyBlurFilter && child is not LegacyBrightnessFilter &&
                        child is not LegacyGrayscaleFilter && child is not LegacyMagnifierFilter && child is not LegacyPixelizationFilter)
                    {
                        throw new NotSupportedException($"V1 writing does not support filter {child?.GetType().FullName ?? "null"} yet.");
                    }
                }
            }
        }

        var drawableArrayPlans = new List<DrawableArrayPlan>(containers.Count);
        foreach (var container in containers)
        {
            var drawable = (LegacyDrawableContainer)container;
            drawableArrayPlans.Add(new DrawableArrayPlan(
                NextObjectId(),
                NextObjectId(),
                NextObjectId(),
                NextObjectId(),
                NextObjectId(),
                NextObjectId(),
                NextObjectId(),
                drawable is LegacyFreehandContainer freehand && freehand.CapturePoints != null
                    ? new List<Point>(freehand.CapturePoints)
                    : new List<Point>(),
                GetContainerData(drawable),
                GetCursorMaskData(drawable),
                new List<LegacyField>(drawable.Fields),
                drawable is LegacyHighlightContainer or LegacyObfuscateContainer
                    ? CreateFilterPlans(drawable.Children)
                    : new List<FilterArrayPlan>()));
        }

        using var writer = new BinaryWriter(destination, Encoding.UTF8, true);
        writer.Write(SerializedStreamHeader);
        writer.Write(RootObjectId);
        writer.Write(-1);
        writer.Write(1);
        writer.Write(0);

        WriteLibrary(writer, EditorLibraryId, "Greenshot.Editor, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null");
        WriteLibrary(writer, BaseLibraryId, "Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null");
        WriteLibrary(writer, 12, "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        WriteRootRecord(writer);
        WriteGuidRecord(writer);
        writer.Write(false);
        writer.Write(MemberReference);
        writer.Write(ItemsObjectId);
        writer.Write(containers.Count);
        writer.Write(containers.Count);
        for (var index = 0; index < drawableArrayPlans.Count; index++)
        {
            var arrayPlan = drawableArrayPlans[index];
            if (arrayPlan.Children.Count == 0)
            {
                WriteTypedArray(writer, arrayPlan.ChildrenArrayId, "Greenshot.Base.Interfaces.Drawing.IFieldHolder", BaseLibraryId, Array.Empty<LegacyField>());
            }
            else
            {
                foreach (var childPlan in arrayPlan.Children)
                {
                    WriteTypedArray(writer, childPlan.FieldsArrayId, "Greenshot.Base.Interfaces.Drawing.IField", BaseLibraryId, childPlan.Fields);
                }
                WriteFilterArray(writer, arrayPlan.ChildrenArrayId, arrayPlan.Children);
            }
            WriteTypedArray(writer, arrayPlan.FieldsArrayId, "Greenshot.Base.Interfaces.Drawing.IField", BaseLibraryId, arrayPlan.Fields);
            if (containers[index] is LegacyFreehandContainer)
            {
                WritePointArray(writer, arrayPlan.PointsArrayId, arrayPlan.Points);
            }
            else if (arrayPlan.ContainerData != null)
            {
                WriteByteArray(writer, arrayPlan.ContainerDataArrayId, arrayPlan.ContainerData);
            }
            if (arrayPlan.CursorMaskData != null)
            {
                WriteByteArray(writer, arrayPlan.CursorMaskDataArrayId, arrayPlan.CursorMaskData);
            }
        }
        WriteItemsArray(writer, containers, drawableArrayPlans);
        writer.Write(MessageEnd);
    }

    /// <summary>
    /// Writes a V1.04 editor file containing the screenshot and supported drawable containers.
    /// </summary>
    internal static void WriteEditorFile(Stream destination, Image screenshot, LegacyDrawableContainerList containers)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must support writing.", nameof(destination));
        }
        if (screenshot == null)
        {
            throw new ArgumentNullException(nameof(screenshot));
        }

        using var pngStream = new MemoryStream();
        screenshot.Save(pngStream, ImageFormat.Png);
        pngStream.Position = 0;

        using var payloadStream = new MemoryStream();
        WriteContainerList(payloadStream, containers);

        pngStream.CopyTo(destination);
        payloadStream.Position = 0;
        payloadStream.CopyTo(destination);

        using var writer = new BinaryWriter(destination, Encoding.ASCII, true);
        writer.Write(payloadStream.Length);
        writer.Write(Encoding.ASCII.GetBytes(V104Marker));
    }

    private static void WriteRootRecord(BinaryWriter writer)
    {
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(RootObjectId);
        writer.Write("Greenshot.Editor.Drawing.DrawableContainerList");
        writer.Write(RootMemberNames.Length);
        foreach (var name in RootMemberNames)
        {
            writer.Write(name);
        }

        writer.Write(BinaryTypeSystemClass);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);

        writer.Write("System.Guid");
        writer.Write(PrimitiveTypeBoolean);
        writer.Write("Greenshot.Base.Interfaces.Drawing.IDrawableContainer[]");
        writer.Write(BaseLibraryId);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(EditorLibraryId);
    }

    private static void WriteGuidRecord(BinaryWriter writer)
    {
        writer.Write(SystemClassWithMembersAndTypes);
        writer.Write(ParentIdObjectId);
        writer.Write("System.Guid");
        writer.Write(11);
        writer.Write("_a");
        writer.Write("_b");
        writer.Write("_c");
        writer.Write("_d");
        writer.Write("_e");
        writer.Write("_f");
        writer.Write("_g");
        writer.Write("_h");
        writer.Write("_i");
        writer.Write("_j");
        writer.Write("_k");

        for (var i = 0; i < 3; i++)
        {
            writer.Write(BinaryTypePrimitive);
        }
        for (var i = 0; i < 8; i++)
        {
            writer.Write(BinaryTypePrimitive);
        }

        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt16);
        writer.Write(PrimitiveTypeInt16);
        for (var i = 0; i < 8; i++)
        {
            writer.Write(PrimitiveTypeByte);
        }

        writer.Write(0);
        writer.Write((short)0);
        writer.Write((short)0);
        for (var i = 0; i < 8; i++)
        {
            writer.Write((byte)0);
        }
    }

    private static void WriteItemsArray(BinaryWriter writer, LegacyDrawableContainerList containers, IList<DrawableArrayPlan> arrayPlans)
    {
        writer.Write(BinaryArray);
        writer.Write(ItemsObjectId);
        writer.Write(BinaryArrayTypeSingle);
        writer.Write(1);
        writer.Write(containers.Count);
        writer.Write(BinaryTypeClass);
        writer.Write("Greenshot.Base.Interfaces.Drawing.IDrawableContainer");
        writer.Write(BaseLibraryId);
        for (var index = 0; index < containers.Count; index++)
        {
            WriteGeometryContainer(writer, (LegacyDrawableContainer)containers[index], arrayPlans[index]);
        }
    }

    private static void WriteGeometryContainer(BinaryWriter writer, LegacyDrawableContainer rectangle, DrawableArrayPlan arrayPlan)
    {
        var containerTypeName = GetContainerTypeName(rectangle);
        var editModeMemberName = rectangle is LegacyRectangleContainer || rectangle is LegacyTextContainer
            ? "RectangleContainer+_defaultEditMode"
            : "DrawableContainer+_defaultEditMode";
        var hasText = rectangle is LegacyTextContainer;
        var hasFreehand = rectangle is LegacyFreehandContainer;
        var isSpeechbubble = rectangle is LegacySpeechbubbleContainer;
        var isImage = rectangle is LegacyImageContainer;
        var isIcon = rectangle is LegacyIconContainer;
        var isStepLabel = rectangle is LegacyStepLabelContainer;
        var isSvg = rectangle is LegacySvgContainer;
        var isMetafile = rectangle is LegacyMetafileContainer;
        var isEmoji = rectangle is LegacyEmojiContainer;
        var isCursor = rectangle is LegacyCursorContainer;
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(arrayPlan.ContainerObjectId);
        writer.Write(containerTypeName);
        writer.Write(hasText || hasFreehand || isImage || isIcon || isCursor ? 10 : isSpeechbubble || isStepLabel || isSvg || isMetafile || isEmoji ? 11 : 9);
        writer.Write("_defaultEditMode");
        writer.Write("Children");
        writer.Write(editModeMemberName);
        writer.Write("DrawableContainer+left");
        writer.Write("DrawableContainer+top");
        writer.Write("DrawableContainer+width");
        writer.Write("DrawableContainer+height");
        writer.Write("DrawableContainer+accountForShadowChange");
        writer.Write("AbstractFieldHolder+fields");
        if (hasText)
        {
            writer.Write("text");
        }
        else if (hasFreehand)
        {
            writer.Write("capturePoints");
        }
        else if (isSpeechbubble)
        {
            writer.Write("TextContainer+text");
            writer.Write("_storedTargetGripperLocation");
        }
        else if (isImage)
        {
            writer.Write("_image");
        }
        else if (isIcon)
        {
            writer.Write("icon");
        }
        if (isSvg)
        {
            writer.Write("VectorGraphicsContainer+_rotationAngle");
            writer.Write("_svgContent");
        }
        if (isMetafile)
        {
            writer.Write("VectorGraphicsContainer+_rotationAngle");
            writer.Write("_metafile");
        }
        if (isEmoji)
        {
            writer.Write("VectorGraphicsContainer+_rotationAngle");
            writer.Write("_emoji");
        }
        if (isCursor)
        {
            writer.Write("savedCursor");
        }
        if (isStepLabel)
        {
            writer.Write("_number");
            writer.Write("_counterStart");
        }

        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypeSystemClass);
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypeSystemClass);
        if (hasText)
        {
            writer.Write(BinaryTypeString);
        }
        else if (hasFreehand)
        {
            writer.Write(BinaryTypeSystemClass);
        }
        else if (isSpeechbubble)
        {
            writer.Write(BinaryTypeString);
            writer.Write(BinaryTypeClass);
        }
        else if (isImage || isIcon)
        {
            writer.Write(BinaryTypeClass);
        }
        if (isSvg)
        {
            writer.Write(BinaryTypePrimitive);
            writer.Write(BinaryTypeSystemClass);
        }
        if (isMetafile)
        {
            writer.Write(BinaryTypePrimitive);
            writer.Write(BinaryTypeClass);
        }
        if (isEmoji)
        {
            writer.Write(BinaryTypePrimitive);
            writer.Write(BinaryTypeString);
        }
        if (isCursor)
        {
            writer.Write(BinaryTypeClass);
        }
        if (isStepLabel)
        {
            writer.Write(BinaryTypePrimitive);
            writer.Write(BinaryTypePrimitive);
        }
        writer.Write("Greenshot.Base.Interfaces.Drawing.EditStatus");
        writer.Write(BaseLibraryId);
        writer.Write("System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IFieldHolder, Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null]]");
        writer.Write("Greenshot.Base.Interfaces.Drawing.EditStatus");
        writer.Write(BaseLibraryId);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeBoolean);
        writer.Write("System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IField, Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null]]");
        if (hasFreehand)
        {
            writer.Write("System.Collections.Generic.List`1[[System.Drawing.Point, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]");
        }
        if (isSpeechbubble)
        {
            writer.Write("System.Drawing.Point");
            writer.Write(12);
        }
        if (isImage)
        {
            writer.Write("System.Drawing.Bitmap");
            writer.Write(12);
        }
        else if (isIcon)
        {
            writer.Write("System.Drawing.Icon");
            writer.Write(12);
        }
        if (isSvg)
        {
            writer.Write(PrimitiveTypeInt32);
            writer.Write("System.IO.MemoryStream");
        }
        if (isMetafile)
        {
            writer.Write(PrimitiveTypeInt32);
            writer.Write("System.Drawing.Imaging.Metafile");
            writer.Write(12);
        }
        if (isEmoji)
        {
            writer.Write(PrimitiveTypeInt32);
        }
        if (isCursor)
        {
            writer.Write("Greenshot.Editor.Drawing.CursorContainer+CaptureCursorSerializationWrapper");
            writer.Write(EditorLibraryId);
        }
        if (isStepLabel)
        {
            writer.Write(PrimitiveTypeInt32);
            writer.Write(PrimitiveTypeInt32);
        }
        writer.Write(EditorLibraryId);

        WriteEditStatusRecord(writer, arrayPlan.EditStatusObjectId, 1);
        WriteFieldHolderList(writer, arrayPlan.ChildrenArrayId, arrayPlan.Children.Count);
        WriteEditStatusRecord(writer, NextObjectId(), 1);
        writer.Write(rectangle.Left);
        writer.Write(rectangle.Top);
        writer.Write(rectangle.Width);
        writer.Write(rectangle.Height);
        writer.Write(false);
        WriteFieldList(writer, rectangle.Fields, arrayPlan.FieldsArrayId);
        if (rectangle is LegacyTextContainer textContainer)
        {
            WriteString(writer, textContainer.Text);
        }
        else if (rectangle is LegacyFreehandContainer)
        {
            WritePointList(writer, arrayPlan.PointsArrayId, arrayPlan.Points);
        }
        else if (rectangle is LegacySpeechbubbleContainer speechbubble)
        {
            WriteString(writer, speechbubble.Text);
            WritePoint(writer, speechbubble.StoredTargetGripperLocation);
        }
        else if (rectangle is LegacyImageContainer image)
        {
            WriteImage(writer, arrayPlan.ContainerDataArrayId, arrayPlan.ContainerData);
        }
        else if (rectangle is LegacyIconContainer icon)
        {
            WriteIcon(writer, arrayPlan.ContainerDataArrayId, arrayPlan.ContainerData, new Size(icon.Width, icon.Height));
        }
        else if (rectangle is LegacySvgContainer svg)
        {
            writer.Write(svg.RotationAngle);
            WriteMemoryStream(writer, arrayPlan.ContainerDataArrayId, arrayPlan.ContainerData);
        }
        else if (rectangle is LegacyMetafileContainer metafile)
        {
            writer.Write(metafile.RotationAngle);
            WriteMetafile(writer, arrayPlan.ContainerDataArrayId, arrayPlan.ContainerData);
        }
        else if (rectangle is LegacyEmojiContainer emoji)
        {
            writer.Write(emoji.RotationAngle);
            WriteString(writer, emoji.Emoji);
        }
        else if (rectangle is LegacyCursorContainer cursor)
        {
            WriteCursor(writer, cursor.savedCursor, arrayPlan);
        }
        else if (rectangle is LegacyStepLabelContainer stepLabel)
        {
            writer.Write(stepLabel.Number);
            writer.Write(stepLabel.CounterStart);
        }
    }

    private static int _nextObjectId = 100;

    private static int NextObjectId() => _nextObjectId++;

    private static string GetContainerTypeName(LegacyDrawableContainer container) => container switch
    {
        LegacyRectangleContainer => "Greenshot.Editor.Drawing.RectangleContainer",
        LegacyEllipseContainer => "Greenshot.Editor.Drawing.EllipseContainer",
        LegacyLineContainer => "Greenshot.Editor.Drawing.LineContainer",
        LegacyArrowContainer => "Greenshot.Editor.Drawing.ArrowContainer",
        LegacyTextContainer => "Greenshot.Editor.Drawing.TextContainer",
        LegacyFreehandContainer => "Greenshot.Editor.Drawing.FreehandContainer",
        LegacySpeechbubbleContainer => "Greenshot.Editor.Drawing.SpeechbubbleContainer",
        LegacyImageContainer => "Greenshot.Editor.Drawing.ImageContainer",
        LegacyIconContainer => "Greenshot.Editor.Drawing.IconContainer",
        LegacyStepLabelContainer => "Greenshot.Editor.Drawing.StepLabelContainer",
        LegacyHighlightContainer => "Greenshot.Editor.Drawing.HighlightContainer",
        LegacyObfuscateContainer => "Greenshot.Editor.Drawing.ObfuscateContainer",
        LegacySvgContainer => "Greenshot.Editor.Drawing.SvgContainer",
        LegacyMetafileContainer => "Greenshot.Editor.Drawing.MetafileContainer",
        LegacyEmojiContainer => "Greenshot.Editor.Drawing.Emoji.EmojiContainer",
        LegacyCursorContainer => "Greenshot.Editor.Drawing.CursorContainer",
        _ => throw new NotSupportedException($"V1 writing does not support {container.GetType().FullName} yet.")
    };

    private static List<FilterArrayPlan> CreateFilterPlans(IList<LegacyFieldHolder> children)
    {
        var result = new List<FilterArrayPlan>(children?.Count ?? 0);
        if (children == null)
        {
            return result;
        }

        foreach (var child in children)
        {
            result.Add(new FilterArrayPlan(NextObjectId(), child, child.Fields == null ? new List<LegacyField>() : new List<LegacyField>(child.Fields)));
        }

        return result;
    }

    private static void WriteEditStatusRecord(BinaryWriter writer, int metadataId, int value)
    {
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(metadataId);
        writer.Write("Greenshot.Base.Interfaces.Drawing.EditStatus");
        writer.Write(1);
        writer.Write("value__");
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(BaseLibraryId);
        writer.Write(value);
    }

    private static void WriteFieldHolderList(BinaryWriter writer, int arrayObjectId, int count)
    {
        var metadataId = NextObjectId();

        writer.Write(SystemClassWithMembersAndTypes);
        writer.Write(metadataId);
        writer.Write("System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IFieldHolder, Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null]]");
        writer.Write(3);
        writer.Write("_items");
        writer.Write("_size");
        writer.Write("_version");
        writer.Write(BinaryTypeObjectArray);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);

        writer.Write(MemberReference);
        writer.Write(arrayObjectId);
        writer.Write(count);
        writer.Write(count);
    }

    private static void WriteFilterArray(BinaryWriter writer, int arrayObjectId, IList<FilterArrayPlan> filters)
    {
        writer.Write(BinaryArray);
        writer.Write(arrayObjectId);
        writer.Write(BinaryArrayTypeSingle);
        writer.Write(1);
        writer.Write(filters.Count);
        writer.Write(BinaryTypeClass);
        writer.Write("Greenshot.Base.Interfaces.Drawing.IFieldHolder");
        writer.Write(BaseLibraryId);
        foreach (var filter in filters)
        {
            WriteFilter(writer, filter);
        }
    }

    private static void WriteFilter(BinaryWriter writer, FilterArrayPlan filter)
    {
        var isBlurFilter = filter.Filter is LegacyBlurFilter;
        const string parentTypeName = "Greenshot.Editor.Drawing.DrawableContainer";
        var filterTypeName = filter.Filter switch
        {
            LegacyHighlightFilter => "Greenshot.Editor.Drawing.Filters.HighlightFilter",
            LegacyBlurFilter => "Greenshot.Editor.Drawing.Filters.BlurFilter",
            LegacyBrightnessFilter => "Greenshot.Editor.Drawing.Filters.BrightnessFilter",
            LegacyGrayscaleFilter => "Greenshot.Editor.Drawing.Filters.GrayscaleFilter",
            LegacyMagnifierFilter => "Greenshot.Editor.Drawing.Filters.MagnifierFilter",
            LegacyPixelizationFilter => "Greenshot.Editor.Drawing.Filters.PixelizationFilter",
            _ => throw new NotSupportedException($"V1 writing does not support filter {filter.Filter.GetType().FullName} yet.")
        };

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write(filterTypeName);
        writer.Write(isBlurFilter ? 5 : 4);
        if (isBlurFilter)
        {
            writer.Write("previewQuality");
        }
        writer.Write("parent");
        writer.Write("AbstractFilter+invert");
        writer.Write("AbstractFilter+parent");
        writer.Write("AbstractFieldHolder+fields");
        if (isBlurFilter)
        {
            writer.Write(BinaryTypePrimitive);
        }
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypeSystemClass);
        if (isBlurFilter)
        {
            writer.Write(PrimitiveTypeDouble);
        }
        writer.Write(parentTypeName);
        writer.Write(EditorLibraryId);
        writer.Write(PrimitiveTypeBoolean);
        writer.Write(parentTypeName);
        writer.Write(EditorLibraryId);
        writer.Write("System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IField, Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null]]");
        writer.Write(EditorLibraryId);

        if (isBlurFilter)
        {
            writer.Write(0d);
        }
        writer.Write(ObjectNull);
        writer.Write(false);
        writer.Write(ObjectNull);
        WriteFieldList(writer, filter.Fields, filter.FieldsArrayId);
    }

    private static void WriteFieldList(BinaryWriter writer, IList<LegacyField> fields, int arrayObjectId)
    {
        var metadataId = NextObjectId();
        writer.Write(SystemClassWithMembersAndTypes);
        writer.Write(metadataId);
        writer.Write("System.Collections.Generic.List`1[[Greenshot.Base.Interfaces.Drawing.IField, Greenshot.Base, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null]]");
        writer.Write(3);
        writer.Write("_items");
        writer.Write("_size");
        writer.Write("_version");
        writer.Write(BinaryTypeObjectArray);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);

        writer.Write(MemberReference);
        writer.Write(arrayObjectId);
        writer.Write(fields.Count);
        writer.Write(fields.Count);
    }

    private static void WriteTypedArray(BinaryWriter writer, int arrayObjectId, string elementTypeName, int libraryId, IList<LegacyField> fields)
    {
        writer.Write(BinaryArray);
        writer.Write(arrayObjectId);
        writer.Write(BinaryArrayTypeSingle);
        writer.Write(1);
        writer.Write(fields.Count);
        writer.Write(BinaryTypeClass);
        writer.Write(elementTypeName);
        writer.Write(libraryId);
        foreach (var field in fields)
        {
            WriteField(writer, field);
        }
    }

    private sealed class DrawableArrayPlan
    {
        internal DrawableArrayPlan(
            int childrenArrayId,
            int fieldsArrayId,
            int pointsArrayId,
            int containerDataArrayId,
            int cursorMaskDataArrayId,
            int containerObjectId,
            int editStatusObjectId,
            IList<Point> points,
            byte[] containerData,
            byte[] cursorMaskData,
            IList<LegacyField> fields,
            IList<FilterArrayPlan> children)
        {
            ChildrenArrayId = childrenArrayId;
            FieldsArrayId = fieldsArrayId;
            PointsArrayId = pointsArrayId;
            ContainerDataArrayId = containerDataArrayId;
            CursorMaskDataArrayId = cursorMaskDataArrayId;
            ContainerObjectId = containerObjectId;
            EditStatusObjectId = editStatusObjectId;
            Points = points;
            ContainerData = containerData;
            CursorMaskData = cursorMaskData;
            Fields = fields;
            Children = children;
        }

        internal int ChildrenArrayId { get; }
        internal int FieldsArrayId { get; }
        internal int PointsArrayId { get; }
        internal int ContainerDataArrayId { get; }
        internal int CursorMaskDataArrayId { get; }
        internal int ContainerObjectId { get; }
        internal int EditStatusObjectId { get; }
        internal IList<Point> Points { get; }
        internal byte[] ContainerData { get; }
        internal byte[] CursorMaskData { get; }
        internal IList<LegacyField> Fields { get; }
        internal IList<FilterArrayPlan> Children { get; }
    }

    private sealed class FilterArrayPlan
    {
        internal FilterArrayPlan(int fieldsArrayId, LegacyFieldHolder filter, IList<LegacyField> fields)
        {
            FieldsArrayId = fieldsArrayId;
            Filter = filter;
            Fields = fields;
        }

        internal int FieldsArrayId { get; }
        internal LegacyFieldHolder Filter { get; }
        internal IList<LegacyField> Fields { get; }
    }

    private static byte[] GetContainerData(LegacyDrawableContainer container)
    {
        if (container is LegacyImageContainer image && image.Image != null)
        {
            using var imageStream = new MemoryStream();
            image.Image.Save(imageStream, ImageFormat.Png);
            return imageStream.ToArray();
        }

        if (container is LegacyIconContainer icon && icon.Icon != null)
        {
            using var iconStream = new MemoryStream();
            icon.Icon.Save(iconStream);
            return iconStream.ToArray();
        }

        if (container is LegacySvgContainer svg && svg.SvgContent != null)
        {
            return svg.SvgContent.ToArray();
        }

        if (container is LegacyMetafileContainer metafile && metafile.MetafileContent != null)
        {
            return metafile.MetafileContent.ToArray();
        }

        if (container is LegacyCursorContainer cursor && cursor.savedCursor?.ColorLayer != null)
        {
            return GetBitmapData(cursor.savedCursor.ColorLayer);
        }

        return null;
    }

    private static byte[] GetCursorMaskData(LegacyDrawableContainer container)
    {
        return container is LegacyCursorContainer cursor && cursor.savedCursor?.MaskLayer != null
            ? GetBitmapData(cursor.savedCursor.MaskLayer)
            : null;
    }

    private static byte[] GetBitmapData(Bitmap bitmap)
    {
        using var imageStream = new MemoryStream();
        bitmap.Save(imageStream, ImageFormat.Png);
        return imageStream.ToArray();
    }

    private static void WriteMemoryStream(BinaryWriter writer, int dataArrayId, byte[] data)
    {
        if (data == null)
        {
            writer.Write(ObjectNull);
            return;
        }

        writer.Write(SystemClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.IO.MemoryStream");
        writer.Write(10);
        writer.Write("_buffer");
        writer.Write("_origin");
        writer.Write("_position");
        writer.Write("_length");
        writer.Write("_capacity");
        writer.Write("_expandable");
        writer.Write("_writable");
        writer.Write("_exposable");
        writer.Write("_isOpen");
        writer.Write("MarshalByRefObject+__identity");
        writer.Write(BinaryTypeObjectArray);
        for (var index = 0; index < 4; index++)
        {
            writer.Write(BinaryTypePrimitive);
        }
        for (var index = 0; index < 4; index++)
        {
            writer.Write(BinaryTypePrimitive);
        }
        writer.Write(BinaryTypeObject);
        for (var index = 0; index < 4; index++)
        {
            writer.Write(PrimitiveTypeInt32);
        }
        for (var index = 0; index < 4; index++)
        {
            writer.Write(PrimitiveTypeBoolean);
        }

        writer.Write(MemberReference);
        writer.Write(dataArrayId);
        writer.Write(0);
        writer.Write(0);
        writer.Write(data.Length);
        writer.Write(data.Length);
        writer.Write(true);
        writer.Write(true);
        writer.Write(true);
        writer.Write(true);
        writer.Write(ObjectNull);
    }

    private static void WriteImage(BinaryWriter writer, int dataArrayId, byte[] imageData)
    {
        if (imageData == null)
        {
            writer.Write(ObjectNull);
            return;
        }

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.Drawing.Bitmap");
        writer.Write(1);
        writer.Write("Data");
        writer.Write(BinaryTypeObjectArray);
        writer.Write(12);
        writer.Write(MemberReference);
        writer.Write(dataArrayId);
    }

    private static void WriteMetafile(BinaryWriter writer, int dataArrayId, byte[] metafileData)
    {
        if (metafileData == null)
        {
            writer.Write(ObjectNull);
            return;
        }

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.Drawing.Imaging.Metafile");
        writer.Write(1);
        writer.Write("Data");
        writer.Write(BinaryTypeObjectArray);
        writer.Write(12);
        writer.Write(MemberReference);
        writer.Write(dataArrayId);
    }

    private static void WriteCursor(BinaryWriter writer, LegacyCaptureCursorSerializationWrapper cursor, DrawableArrayPlan arrayPlan)
    {
        if (cursor == null)
        {
            writer.Write(ObjectNull);
            return;
        }

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("Greenshot.Editor.Drawing.CursorContainer+CaptureCursorSerializationWrapper");
        writer.Write(6);
        writer.Write("<ColorLayer>k__BackingField");
        writer.Write("<MaskLayer>k__BackingField");
        writer.Write("<SizeWidth>k__BackingField");
        writer.Write("<SizeHeight>k__BackingField");
        writer.Write("<HotspotX>k__BackingField");
        writer.Write("<HotspotY>k__BackingField");
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypeClass);
        for (var index = 0; index < 4; index++)
        {
            writer.Write(BinaryTypePrimitive);
        }
        writer.Write("System.Drawing.Bitmap");
        writer.Write(12);
        writer.Write("System.Drawing.Bitmap");
        writer.Write(12);
        for (var index = 0; index < 4; index++)
        {
            writer.Write(PrimitiveTypeInt32);
        }
        writer.Write(EditorLibraryId);

        WriteImage(writer, arrayPlan.ContainerDataArrayId, arrayPlan.ContainerData);
        WriteImage(writer, arrayPlan.CursorMaskDataArrayId, arrayPlan.CursorMaskData);
        writer.Write(cursor.SizeWidth);
        writer.Write(cursor.SizeHeight);
        writer.Write(cursor.HotspotX);
        writer.Write(cursor.HotspotY);
    }

    private static void WriteIcon(BinaryWriter writer, int dataArrayId, byte[] iconData, Size iconSize)
    {
        if (iconData == null)
        {
            writer.Write(ObjectNull);
            return;
        }

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.Drawing.Icon");
        writer.Write(2);
        writer.Write("IconSize");
        writer.Write("IconData");
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypeObjectArray);
        writer.Write("System.Drawing.Size");
        writer.Write(12);
        writer.Write(12);
        WriteSize(writer, iconSize);
        writer.Write(MemberReference);
        writer.Write(dataArrayId);
    }

    private static void WriteSize(BinaryWriter writer, Size size)
    {
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.Drawing.Size");
        writer.Write(2);
        writer.Write("width");
        writer.Write("height");
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(12);
        writer.Write(size.Width);
        writer.Write(size.Height);
    }

    private static void WriteByteArray(BinaryWriter writer, int arrayObjectId, byte[] bytes)
    {
        writer.Write(BinaryArray);
        writer.Write(arrayObjectId);
        writer.Write(BinaryArrayTypeSingle);
        writer.Write(1);
        writer.Write(bytes.Length);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeByte);
        writer.Write(bytes);
    }

    private static void WritePointArray(BinaryWriter writer, int arrayObjectId, IList<Point> points)
    {
        writer.Write(BinaryArray);
        writer.Write(arrayObjectId);
        writer.Write(BinaryArrayTypeSingle);
        writer.Write(1);
        writer.Write(points.Count);
        writer.Write(BinaryTypeClass);
        writer.Write("System.Drawing.Point");
        writer.Write(12);
        foreach (var point in points)
        {
            WritePoint(writer, point);
        }
    }

    private static void WritePointList(BinaryWriter writer, int arrayObjectId, IList<Point> points)
    {
        var listMetadataId = NextObjectId();
        writer.Write(SystemClassWithMembersAndTypes);
        writer.Write(listMetadataId);
        writer.Write("System.Collections.Generic.List`1[[System.Drawing.Point, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]");
        writer.Write(3);
        writer.Write("_items");
        writer.Write("_size");
        writer.Write("_version");
        writer.Write(BinaryTypeObjectArray);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);

        writer.Write(MemberReference);
        writer.Write(arrayObjectId);
        writer.Write(points.Count);
        writer.Write(points.Count);
    }

    private static void WritePoint(BinaryWriter writer, Point point)
    {
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.Drawing.Point");
        writer.Write(2);
        writer.Write("x");
        writer.Write("y");
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(12);
        writer.Write(point.X);
        writer.Write(point.Y);
    }

    private static void WriteField(BinaryWriter writer, LegacyField field)
    {
        if (field?.FieldType?.Name == null)
        {
            throw new NotSupportedException("V1 field records require a field type name.");
        }
        if (field.Value != null && field.Value is not int && field.Value is not bool && field.Value is not float && field.Value is not double &&
            field.Value is not string && field.Value is not Color && !IsSupportedEnum(field.Value))
        {
            throw new NotSupportedException($"V1 writing does not support field value type {field.Value.GetType().FullName} yet.");
        }

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("Greenshot.Editor.Drawing.Fields.Field");
        writer.Write(3);
        writer.Write("_myValue");
        writer.Write("<FieldType>k__BackingField");
        writer.Write("<Scope>k__BackingField");
        writer.Write(BinaryTypeObject);
        writer.Write(BinaryTypeClass);
        writer.Write(BinaryTypeString);
        writer.Write("Greenshot.Editor.Drawing.Fields.FieldType");
        writer.Write(EditorLibraryId);
        writer.Write(EditorLibraryId);

        WriteFieldValue(writer, field.Value);
        WriteFieldType(writer, field.FieldType.Name);
        WriteString(writer, field.Scope);
    }

    private static void WriteFieldValue(BinaryWriter writer, object value)
    {
        if (value == null)
        {
            writer.Write(ObjectNull);
        }
        else if (value is int intValue)
        {
            writer.Write(MemberPrimitiveTyped);
            writer.Write(PrimitiveTypeInt32);
            writer.Write(intValue);
        }
        else if (value is bool boolValue)
        {
            writer.Write(MemberPrimitiveTyped);
            writer.Write(PrimitiveTypeBoolean);
            writer.Write(boolValue);
        }
        else if (value is float singleValue)
        {
            writer.Write(MemberPrimitiveTyped);
            writer.Write(PrimitiveTypeSingle);
            writer.Write(singleValue);
        }
        else if (value is double doubleValue)
        {
            writer.Write(MemberPrimitiveTyped);
            writer.Write(PrimitiveTypeDouble);
            writer.Write(doubleValue);
        }
        else if (value is string stringValue)
        {
            WriteString(writer, stringValue);
        }
        else if (value is Color colorValue)
        {
            WriteColor(writer, colorValue);
        }
        else if (IsSupportedEnum(value))
        {
            WriteEnum(writer, (Enum)value);
        }
    }

    private static bool IsSupportedEnum(object value) => value is Enum;

    private static void WriteEnum(BinaryWriter writer, Enum value)
    {
        var libraryId = GetEnumLibraryId(value.GetType());

        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write(value.GetType().FullName);
        writer.Write(1);
        writer.Write("value__");
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt32);
        writer.Write(libraryId);
        writer.Write(Convert.ToInt32(value));
    }

    private static int GetEnumLibraryId(Type enumType)
    {
        var fullName = enumType.FullName ?? string.Empty;
        if (fullName.StartsWith("System.Drawing.", StringComparison.Ordinal))
        {
            return 12;
        }

        if (fullName.StartsWith("Greenshot.Base.", StringComparison.Ordinal))
        {
            return BaseLibraryId;
        }

        if (fullName.StartsWith("Greenshot.Editor.", StringComparison.Ordinal))
        {
            return EditorLibraryId;
        }

        return BaseLibraryId;
    }

    private static void WriteFieldType(BinaryWriter writer, string name)
    {
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("Greenshot.Editor.Drawing.Fields.FieldType");
        writer.Write(1);
        writer.Write("<Name>k__BackingField");
        writer.Write(BinaryTypeString);
        writer.Write(EditorLibraryId);
        WriteString(writer, name);
    }

    private static void WriteColor(BinaryWriter writer, Color color)
    {
        writer.Write(ClassWithMembersAndTypes);
        writer.Write(NextObjectId());
        writer.Write("System.Drawing.Color");
        writer.Write(4);
        writer.Write("name");
        writer.Write("value");
        writer.Write("knownColor");
        writer.Write("state");
        writer.Write(BinaryTypeString);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(BinaryTypePrimitive);
        writer.Write(PrimitiveTypeInt64);
        writer.Write(PrimitiveTypeInt16);
        writer.Write(PrimitiveTypeInt16);
        writer.Write(12);
        writer.Write(ObjectNull);
        writer.Write((long)color.ToArgb());
        writer.Write((short)(color.IsKnownColor ? color.ToKnownColor() : 0));
        writer.Write((short)(color.IsKnownColor ? 1 : 2));
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        if (value == null)
        {
            writer.Write(ObjectNull);
            return;
        }

        writer.Write(BinaryObjectString);
        writer.Write(NextObjectId());
        writer.Write(value);
    }

    private static void WriteLibrary(BinaryWriter writer, int libraryId, string libraryName)
    {
        writer.Write(BinaryLibrary);
        writer.Write(libraryId);
        writer.Write(libraryName);
    }
}
