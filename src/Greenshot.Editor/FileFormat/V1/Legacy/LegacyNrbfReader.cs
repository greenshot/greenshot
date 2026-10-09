/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel.Security;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using System.Formats.Nrbf;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

/// <summary>
/// Parses NRBF data into the restricted legacy model without creating serialized runtime types.
/// </summary>
internal static class LegacyNrbfReader
{
    private const long MaximumPayloadSize = 128L * 1024 * 1024;
    private const int MaximumCollectionSize = 100_000;
    private const int MaximumImageSize = 64 * 1024 * 1024;

    public static LegacyDrawableContainerList ReadContainerList(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("The legacy stream must support reading and seeking.", nameof(stream));
        }

        if (stream.Length - stream.Position > MaximumPayloadSize)
        {
            throw new SerializationException("The legacy NRBF payload exceeds the maximum supported size.");
        }

        var root = NrbfDecoder.Decode(stream, leaveOpen: true);
        if (root is not ClassRecord rootClass || !TryGetLegacyType(rootClass.TypeName.FullName, out var rootType) || rootType != typeof(LegacyDrawableContainerList))
        {
            throw SuspiciousType(root.TypeName.FullName);
        }

        var result = new LegacyDrawableContainerList();
        foreach (var value in ReadListValues(rootClass))
        {
            if (value is not ClassRecord containerRecord)
            {
                throw new SerializationException("A legacy drawable container list contains an invalid entry.");
            }

            result.Add(ReadContainer(containerRecord));
        }

        return result;
    }

    private static LegacyDrawableContainer ReadContainer(ClassRecord record)
    {
        if (!TryGetLegacyType(record.TypeName.FullName, out var type))
        {
            throw SuspiciousType(record.TypeName.FullName);
        }

        LegacyDrawableContainer container = type == typeof(LegacyArrowContainer) ? new LegacyArrowContainer()
            : type == typeof(LegacyLineContainer) ? new LegacyLineContainer()
            : type == typeof(LegacyRectangleContainer) ? new LegacyRectangleContainer()
            : type == typeof(LegacyEllipseContainer) ? new LegacyEllipseContainer()
            : type == typeof(LegacyHighlightContainer) ? new LegacyHighlightContainer()
            : type == typeof(LegacyObfuscateContainer) ? new LegacyObfuscateContainer()
            : type == typeof(LegacyTextContainer) ? new LegacyTextContainer()
            : type == typeof(LegacyImageContainer) ? new LegacyImageContainer()
            : type == typeof(LegacyIconContainer) ? new LegacyIconContainer()
            : type == typeof(LegacyCursorContainer) ? new LegacyCursorContainer()
            : type == typeof(LegacySpeechbubbleContainer) ? new LegacySpeechbubbleContainer()
            : type == typeof(LegacyFreehandContainer) ? new LegacyFreehandContainer()
            : type == typeof(LegacyMetafileContainer) ? new LegacyMetafileContainer()
            : type == typeof(LegacySvgContainer) ? new LegacySvgContainer()
            : type == typeof(LegacyEmojiContainer) ? new LegacyEmojiContainer()
            : type == typeof(LegacyStepLabelContainer) ? new LegacyStepLabelContainer()
            : throw SuspiciousType(record.TypeName.FullName);

        container.Left = ReadInt32(record, "DrawableContainer+left");
        container.Top = ReadInt32(record, "DrawableContainer+top");
        container.Width = ReadInt32(record, "DrawableContainer+width");
        container.Height = ReadInt32(record, "DrawableContainer+height");
        container.Fields = ReadFields(record, "AbstractFieldHolder+fields");
        container.Children = ReadChildren(record, "Children");

        switch (container)
        {
            case LegacyTextContainer text:
                text.Text = ReadString(record, "text", "TextContainer+text");
                break;
            case LegacyImageContainer image:
                image.Image = ReadImage(GetValue(record, "_image", "image"));
                break;
            case LegacyIconContainer icon:
                icon.Icon = ReadIcon(GetValue(record, "icon"));
                break;
            case LegacyCursorContainer cursor:
                cursor.savedCursor = ReadCursor(GetValue(record, "savedCursor"));
                break;
            case LegacySpeechbubbleContainer speech:
                speech.Text = ReadString(record, "TextContainer+text", "text");
                speech.StoredTargetGripperLocation = ReadPoint(GetValue(record, "_storedTargetGripperLocation"));
                break;
            case LegacyFreehandContainer freehand:
                freehand.CapturePoints = ReadPointList(GetValue(record, "capturePoints"));
                break;
            case LegacyMetafileContainer metafile:
                metafile.RotationAngle = ReadInt32(record, "VectorGraphicsContainer+_rotationAngle");
                metafile.SetMetafileFromData(ReadImageData(GetValue(record, "_metafile")));
                break;
            case LegacySvgContainer svg:
                svg.RotationAngle = ReadInt32(record, "VectorGraphicsContainer+_rotationAngle");
                svg.SvgContent = ToMemoryStream(ReadMemoryStreamBytes(GetValue(record, "_svgContent")));
                break;
            case LegacyEmojiContainer emoji:
                emoji.RotationAngle = ReadInt32(record, "VectorGraphicsContainer+_rotationAngle");
                emoji.Emoji = ReadString(record, "_emoji");
                break;
            case LegacyStepLabelContainer step:
                step.Number = ReadInt32(record, "_number");
                step.CounterStart = ReadInt32(record, "_counterStart");
                step.EnsureBackwardCompatibleFields();
                break;
        }

        return container;
    }

    private static IList<LegacyField> ReadFields(ClassRecord holder, string memberName)
    {
        var result = new List<LegacyField>();
        var rawList = GetValue(holder, memberName);
        if (rawList == null)
        {
            return result;
        }

        foreach (var item in ReadListValues(rawList))
        {
            if (item is not ClassRecord fieldRecord || !TryGetLegacyType(fieldRecord.TypeName.FullName, out var fieldType) || fieldType != typeof(LegacyField))
            {
                throw SuspiciousType(item is ClassRecord invalidField ? invalidField.TypeName.FullName : item?.GetType().FullName ?? "null");
            }

            var fieldTypeRecord = GetValue(fieldRecord, "<FieldType>k__BackingField") as ClassRecord;
            var field = new LegacyField
            {
                FieldType = fieldTypeRecord == null ? null : new LegacyFieldType { Name = ReadString(fieldTypeRecord, "<Name>k__BackingField", "Name") },
                Scope = ReadString(fieldRecord, "<Scope>k__BackingField", "Scope")
            };
            field.ResetValue(ReadFieldValue(GetValue(fieldRecord, "_myValue")));
            result.Add(field);
        }

        return result;
    }

    private static IList<LegacyFieldHolder> ReadChildren(ClassRecord holder, string memberName)
    {
        var result = new List<LegacyFieldHolder>();
        var rawList = GetValue(holder, memberName);
        if (rawList == null)
        {
            return result;
        }

        foreach (var item in ReadListValues(rawList))
        {
            if (item is not ClassRecord childRecord)
            {
                throw new SerializationException("A legacy field-holder child is invalid.");
            }

            if (!TryGetLegacyType(childRecord.TypeName.FullName, out var type))
            {
                throw SuspiciousType(childRecord.TypeName.FullName);
            }

            LegacyFieldHolder child = type == typeof(LegacyHighlightFilter) ? new LegacyHighlightFilter()
                : type == typeof(LegacyBlurFilter) ? new LegacyBlurFilter()
                : type == typeof(LegacyBrightnessFilter) ? new LegacyBrightnessFilter()
                : type == typeof(LegacyGrayscaleFilter) ? new LegacyGrayscaleFilter()
                : type == typeof(LegacyMagnifierFilter) ? new LegacyMagnifierFilter()
                : type == typeof(LegacyPixelizationFilter) ? new LegacyPixelizationFilter()
                : throw SuspiciousType(childRecord.TypeName.FullName);

            child.Fields = ReadFields(childRecord, "AbstractFieldHolder+fields");
            result.Add(child);
        }

        return result;
    }

    private static object ReadFieldValue(object value)
    {
        if (value == null || value is int || value is string || value is bool || value is float || value is double || value is decimal)
        {
            return value;
        }

        if (value is not ClassRecord record)
        {
            throw SuspiciousType(value.GetType().FullName);
        }

        var typeName = record.TypeName.FullName;
        if (typeName == "System.Drawing.Color")
        {
            var knownColor = Convert.ToInt16(GetValue(record, "knownColor"));
            if (knownColor != 0)
            {
                return Color.FromKnownColor((KnownColor)knownColor);
            }

            var argb = unchecked((int)Convert.ToInt64(GetValue(record, "value")));
            return Color.FromArgb(argb);
        }

        if (typeName == "System.Drawing.StringAlignment")
        {
            return Enum.ToObject(typeof(StringAlignment), ReadInt32(record, "value__"));
        }

        if (TryGetLegacyType(typeName, out var type))
        {
            if (type == typeof(FieldFlag) || type == typeof(FilterContainer.PreparedFilter) || type == typeof(ArrowContainer.ArrowHeadCombination))
            {
                return Enum.ToObject(type, ReadInt32(record, "value__"));
            }
        }

        throw SuspiciousType(typeName);
    }

    private static LegacyCaptureCursorSerializationWrapper ReadCursor(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is not ClassRecord record)
        {
            throw new SerializationException("The legacy cursor data is invalid.");
        }

        return new LegacyCaptureCursorSerializationWrapper
        {
            ColorLayer = ReadBitmap(GetValue(record, "<ColorLayer>k__BackingField")),
            MaskLayer = ReadBitmap(GetValue(record, "<MaskLayer>k__BackingField")),
            SizeWidth = ReadInt32(record, "<SizeWidth>k__BackingField"),
            SizeHeight = ReadInt32(record, "<SizeHeight>k__BackingField"),
            HotspotX = ReadInt32(record, "<HotspotX>k__BackingField"),
            HotspotY = ReadInt32(record, "<HotspotY>k__BackingField")
        };
    }

    private static Icon ReadIcon(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is not ClassRecord record || record.TypeName.FullName != "System.Drawing.Icon")
        {
            throw new SerializationException("The legacy icon data is invalid.");
        }

        var bytes = ReadByteArray(GetValue(record, "IconData"));
        if (bytes == null)
        {
            return null;
        }

        using var stream = new MemoryStream(bytes, false);
        return new Icon(stream);
    }

    private static Image ReadImage(object value)
    {
        var bytes = ReadImageData(value);
        return bytes == null ? null : DecodeImage(bytes);
    }

    private static Bitmap ReadBitmap(object value)
    {
        var image = ReadImage(value);
        if (image == null)
        {
            return null;
        }

        using (image)
        {
            return new Bitmap(image);
        }
    }

    private static byte[] ReadImageData(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is not ClassRecord imageRecord ||
            imageRecord.TypeName.FullName != "System.Drawing.Bitmap" && imageRecord.TypeName.FullName != "System.Drawing.Imaging.Metafile")
        {
            throw new SerializationException("The legacy image data is invalid.");
        }

        return ReadByteArray(GetValue(imageRecord, "Data"));
    }

    private static Image DecodeImage(byte[] bytes)
    {
        if (bytes.Length > MaximumImageSize)
        {
            throw new SerializationException("The legacy image exceeds the maximum supported size.");
        }

        using var stream = new MemoryStream(bytes, false);
        using var decodedImage = Image.FromStream(stream, true, true);
        if (decodedImage.Width > 32768 || decodedImage.Height > 32768)
        {
            throw new SerializationException("The legacy image dimensions exceed the maximum supported size.");
        }

        return ImageHelper.Clone(decodedImage);
    }

    private static List<Point> ReadPointList(object value)
    {
        var points = new List<Point>();
        if (value == null)
        {
            return points;
        }

        foreach (var item in ReadListValues(value))
        {
            points.Add(ReadPoint(item));
        }

        return points;
    }

    private static Point ReadPoint(object value)
    {
        if (value == null)
        {
            return Point.Empty;
        }

        if (value is not ClassRecord record || record.TypeName.FullName != "System.Drawing.Point")
        {
            throw new SerializationException("The legacy point data is invalid.");
        }

        return new Point(ReadInt32(record, "x"), ReadInt32(record, "y"));
    }

    private static IEnumerable<object> ReadListValues(object value)
    {
        if (value is not ClassRecord listRecord)
        {
            throw new SerializationException("The legacy collection data is invalid.");
        }

        var typeName = listRecord.TypeName.FullName;
        var isListType = typeName.StartsWith("System.Collections.Generic.List`1", StringComparison.Ordinal) ||
            TryGetLegacyType(typeName, out var mappedType) && mappedType == typeof(LegacyDrawableContainerList);
        if (!isListType)
        {
            throw new SerializationException("The legacy collection data is invalid.");
        }

        var sizeValue = GetValue(listRecord, "List`1+_size", "_size");
        var size = Convert.ToInt32(sizeValue);
        if (size < 0 || size > MaximumCollectionSize)
        {
            throw new SerializationException("The legacy collection exceeds the maximum supported size.");
        }

        var items = GetValue(listRecord, "List`1+_items", "_items");
        if (items == null)
        {
            if (size == 0)
            {
                return Array.Empty<object>();
            }

            throw new SerializationException("The legacy collection is missing its item array.");
        }

        if (items is not ArrayRecord arrayRecord || arrayRecord.Rank != 1 || arrayRecord.Lengths[0] < size || arrayRecord.Lengths[0] > MaximumCollectionSize)
        {
            throw new SerializationException("The legacy collection has an invalid item array.");
        }

        var arrayMethod = arrayRecord.GetType().GetMethod("GetArray", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(bool) }, null);
        if (arrayMethod == null)
        {
            throw new SerializationException("The legacy collection item array has an unsupported encoding.");
        }

        var array = (Array)arrayMethod.Invoke(arrayRecord, new object[] { true });
        var result = new object[size];
        Array.Copy(array, result, size);
        return result;
    }

    private static byte[] ReadMemoryStreamBytes(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is not ClassRecord record || record.TypeName.FullName != "System.IO.MemoryStream")
        {
            throw new SerializationException("The legacy memory-stream data is invalid.");
        }

        var buffer = ReadByteArray(GetValue(record, "_buffer"));
        var origin = ReadInt32(record, "_origin");
        var length = ReadInt32(record, "_length");
        if (buffer == null || origin < 0 || length < 0 || origin > buffer.Length || length > buffer.Length - origin || length > MaximumImageSize)
        {
            throw new SerializationException("The legacy memory-stream bounds are invalid.");
        }

        var bytes = new byte[length];
        Buffer.BlockCopy(buffer, origin, bytes, 0, length);
        return bytes;
    }

    private static byte[] ReadByteArray(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is not ArrayRecord record || record.Rank != 1 || record.Lengths[0] > MaximumImageSize)
        {
            throw new SerializationException("The legacy byte-array data is invalid.");
        }

        return (byte[])record.GetArray(typeof(byte[]), true);
    }

    private static MemoryStream ToMemoryStream(byte[] bytes)
    {
        if (bytes == null)
        {
            return null;
        }

        var stream = RecyclableMemoryStreamFactory.GetStream("LegacyNrbfReader.ToMemoryStream");
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        return stream;
    }

    private static int ReadInt32(ClassRecord record, params string[] memberNames) => Convert.ToInt32(GetValue(record, memberNames));

    private static string ReadString(ClassRecord record, params string[] memberNames) => GetValue(record, memberNames) as string;

    private static object GetValue(ClassRecord record, params string[] memberNames)
    {
        foreach (var memberName in memberNames)
        {
            if (record.HasMember(memberName))
            {
                return record.GetRawValue(memberName);
            }
        }

        var suffixes = new HashSet<string>(memberNames, StringComparer.Ordinal);
        var matchingMember = record.MemberNames.FirstOrDefault(name => suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal)));
        return matchingMember == null ? null : record.GetRawValue(matchingMember);
    }

    private static bool TryGetLegacyType(string typeName, out Type type) => LegacyTypeMapper.TryGetType(typeName, out type);

    private static SecurityAccessDeniedException SuspiciousType(string typeName) =>
        new($"Suspicious type in .greenshot file: {typeName}");
}
