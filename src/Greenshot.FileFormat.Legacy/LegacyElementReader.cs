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
using System.Formats.Nrbf;
using System.IO;
using System.Runtime.Serialization;

namespace Greenshot.FileFormat.Legacy
{
    /// <summary>
    /// Reads the elements (drawable containers) of a legacy .greenshot or .gst file into <see cref="LegacyElement"/>s,
    /// without BinaryFormatter: the NRBF records are decoded by System.Formats.Nrbf and nothing is instantiated by type name,
    /// so a crafted file can't make us create arbitrary objects.
    ///
    /// The member names are those of the Greenshot 1.2 - 1.4 classes. A member is looked up by its exact name, or by
    /// "Class+name" (the way BinaryFormatter names fields of base classes), this covers the renames between the versions.
    /// This class is the frozen description of the old format: don't change it because the editor classes change.
    /// </summary>
    public static class LegacyElementReader
    {
        /// <summary>
        /// Read the elements from the NRBF stream at the current position of the stream (a .gst file, or the part of a .greenshot file
        /// after the PNG, see <see cref="LegacyGreenshotFile"/>). The stream is left open.
        /// </summary>
        /// <param name="stream">Stream positioned at the start of the NRBF payload</param>
        /// <returns>The elements in z-order (first is at the bottom), unknown types have the kind <see cref="LegacyElementKind.Unknown"/></returns>
        /// <exception cref="LegacyFormatException">When the stream isn't a valid payload</exception>
        public static IReadOnlyList<LegacyElement> Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            SerializationRecord root;
            try
            {
                root = NrbfDecoder.Decode(stream, leaveOpen: true);
            }
            catch (Exception ex) when (ex is SerializationException or NotSupportedException or EndOfStreamException or ArgumentException or InvalidOperationException)
            {
                throw new LegacyFormatException("The elements of the file could not be decoded.", ex);
            }

            if (root is not ClassRecord rootRecord)
            {
                throw new LegacyFormatException($"Expected a list of elements, found {root.RecordType}.");
            }

            try
            {
                var elements = new List<LegacyElement>();
                foreach (var item in ReadListItems(rootRecord))
                {
                    if (item is ClassRecord elementRecord)
                    {
                        elements.Add(ReadElement(elementRecord));
                    }
                }
                return elements;
            }
            catch (Exception ex) when (ex is InvalidCastException or InvalidOperationException or KeyNotFoundException or SerializationException or FormatException or OverflowException)
            {
                throw new LegacyFormatException("The elements of the file have an unexpected structure.", ex);
            }
        }

        private static LegacyElement ReadElement(ClassRecord record)
        {
            string typeName = record.TypeName.FullName;
            var kind = KindFromTypeName(SimpleName(typeName));
            LegacyElement element;
            switch (kind)
            {
                case LegacyElementKind.Text:
                    element = new LegacyTextElement
                    {
                        Text = GetString(record, "text")
                    };
                    break;
                case LegacyElementKind.Speechbubble:
                    element = new LegacySpeechbubbleElement
                    {
                        Text = GetString(record, "text"),
                        TargetGripperLocation = GetPoint(record, "_storedTargetGripperLocation")
                    };
                    break;
                case LegacyElementKind.StepLabel:
                    element = new LegacyStepLabelElement
                    {
                        Number = GetInt32(record, "_number"),
                        CounterStart = GetInt32(record, "_counterStart", 1)
                    };
                    break;
                case LegacyElementKind.Freehand:
                    element = new LegacyFreehandElement
                    {
                        Points = ReadPoints(GetRecord(record, "capturePoints"))
                    };
                    break;
                case LegacyElementKind.Image:
                    element = new LegacyImageElement
                    {
                        // 1.2 named the field "image", 1.3 "_image"
                        ImageData = GetBytes(GetRecord(record, "_image") ?? GetRecord(record, "image"), "Data")
                    };
                    break;
                case LegacyElementKind.Icon:
                    element = new LegacyIconElement
                    {
                        IconData = GetBytes(GetRecord(record, "icon"), "IconData")
                    };
                    break;
                case LegacyElementKind.Metafile:
                    element = new LegacyMetafileElement
                    {
                        RotationAngle = GetInt32(record, "_rotationAngle"),
                        ImageData = GetBytes(GetRecord(record, "_metafile"), "Data")
                    };
                    break;
                case LegacyElementKind.Svg:
                    element = new LegacySvgElement
                    {
                        RotationAngle = GetInt32(record, "_rotationAngle"),
                        SvgData = ReadMemoryStream(GetRecord(record, "_svgContent"))
                    };
                    break;
                case LegacyElementKind.Emoji:
                    element = new LegacyEmojiElement
                    {
                        RotationAngle = GetInt32(record, "_rotationAngle"),
                        Emoji = GetString(record, "_emoji")
                    };
                    break;
                case LegacyElementKind.Cursor:
                    element = ReadCursor(GetRecord(record, "savedCursor"));
                    break;
                default:
                    element = new LegacyElement();
                    break;
            }

            element.Kind = kind;
            element.TypeName = typeName;
            element.Left = GetInt32(record, "left");
            element.Top = GetInt32(record, "top");
            element.Width = GetInt32(record, "width");
            element.Height = GetInt32(record, "height");
            element.Fields = ReadFields(record);
            element.Filters = ReadFilters(record);
            return element;
        }

        private static LegacyCursorElement ReadCursor(ClassRecord wrapper)
        {
            // 1.3 stored a System.Windows.Forms.Cursor here (which 1.3 itself couldn't read back), 1.4 a CaptureCursorSerializationWrapper
            if (wrapper == null || FindMember(wrapper, "ColorLayer") == null)
            {
                return new LegacyCursorElement();
            }

            return new LegacyCursorElement
            {
                ColorLayerData = GetBytes(GetRecord(wrapper, "ColorLayer"), "Data"),
                MaskLayerData = GetBytes(GetRecord(wrapper, "MaskLayer"), "Data"),
                CursorWidth = GetInt32(wrapper, "SizeWidth"),
                CursorHeight = GetInt32(wrapper, "SizeHeight"),
                HotspotX = GetInt32(wrapper, "HotspotX"),
                HotspotY = GetInt32(wrapper, "HotspotY")
            };
        }

        private static IReadOnlyList<LegacyFilter> ReadFilters(ClassRecord element)
        {
            var filters = new List<LegacyFilter>();
            foreach (var child in ReadListItems(GetRecord(element, "Children")))
            {
                if (child is not ClassRecord filterRecord)
                {
                    continue;
                }

                filters.Add(new LegacyFilter
                {
                    Name = SimpleName(filterRecord.TypeName.FullName),
                    Invert = GetBoolean(filterRecord, "invert"),
                    Fields = ReadFields(filterRecord)
                });
            }

            return filters;
        }

        private static IReadOnlyList<LegacyField> ReadFields(ClassRecord fieldHolder)
        {
            var fields = new List<LegacyField>();
            foreach (var item in ReadListItems(GetRecord(fieldHolder, "fields")))
            {
                if (item is not ClassRecord fieldRecord)
                {
                    continue;
                }

                var fieldType = GetRecord(fieldRecord, "FieldType");
                string fieldTypeName = fieldType == null ? null : GetString(fieldType, "Name");
                if (string.IsNullOrEmpty(fieldTypeName))
                {
                    continue;
                }

                fields.Add(new LegacyField
                {
                    FieldTypeName = fieldTypeName,
                    Scope = GetString(fieldRecord, "Scope"),
                    Value = ReadValue(GetRaw(fieldRecord, "_myValue") ?? GetRaw(fieldRecord, "myValue"))
                });
            }

            return fields;
        }

        /// <summary>
        /// Convert the raw value of an "object" member: primitives come as they are, records are converted
        /// </summary>
        private static object ReadValue(object raw)
        {
            switch (raw)
            {
                case null:
                    return null;
                case PrimitiveTypeRecord primitive:
                    return primitive.Value;
                case ClassRecord classRecord:
                    return ReadValueRecord(classRecord);
                case SerializationRecord record:
                    return new LegacyUnsupportedValue(record.RecordType.ToString());
                default:
                    return raw;
            }
        }

        private static object ReadValueRecord(ClassRecord record)
        {
            string typeName = record.TypeName.FullName;
            if (typeName == "System.Drawing.Color")
            {
                return new LegacyColor(
                    Convert.ToInt64(GetRaw(record, "value") ?? 0L),
                    Convert.ToInt16(GetRaw(record, "knownColor") ?? (short)0),
                    Convert.ToInt16(GetRaw(record, "state") ?? (short)0),
                    GetString(record, "name"));
            }

            // Enums are serialized as a record with only the "value__" member
            var enumValue = GetRaw(record, "value__");
            if (enumValue is IConvertible)
            {
                return new LegacyEnumValue(SimpleName(typeName), Convert.ToInt64(enumValue));
            }

            return new LegacyUnsupportedValue(typeName);
        }

        private static IReadOnlyList<LegacyPoint> ReadPoints(ClassRecord list)
        {
            var points = new List<LegacyPoint>();
            foreach (var item in ReadListItems(list))
            {
                if (item is ClassRecord pointRecord)
                {
                    points.Add(ToPoint(pointRecord));
                }
            }

            return points;
        }

        private static byte[] ReadMemoryStream(ClassRecord memoryStream)
        {
            var buffer = GetBytes(memoryStream, "_buffer");
            if (buffer == null)
            {
                return null;
            }

            int origin = Math.Max(0, GetInt32(memoryStream, "_origin"));
            int length = GetInt32(memoryStream, "_length", buffer.Length);
            length = Math.Min(length, buffer.Length);
            if (origin == 0 && length == buffer.Length)
            {
                return buffer;
            }

            if (origin >= length)
            {
                return new byte[0];
            }

            var result = new byte[length - origin];
            Array.Copy(buffer, origin, result, 0, result.Length);
            return result;
        }

        /// <summary>
        /// The items of a serialized List&lt;T&gt; (or a class derived from it, like DrawableContainerList), only the first _size are used
        /// </summary>
        private static IEnumerable<SerializationRecord> ReadListItems(ClassRecord list)
        {
            if (list == null)
            {
                yield break;
            }

            var itemsMember = FindMember(list, "_items");
            // Arrays of classes, interfaces and structs (Point) are all exposed as single dimension arrays of records
            if (itemsMember == null || list.GetRawValue(itemsMember) is not SZArrayRecord<SerializationRecord> itemsArray)
            {
                yield break;
            }

            int size = GetInt32(list, "_size", int.MaxValue);
            var items = itemsArray.GetArray();
            int count = Math.Min(size, items.Length);
            for (int i = 0; i < count; i++)
            {
                if (items[i] != null)
                {
                    yield return items[i];
                }
            }
        }

        private static LegacyElementKind KindFromTypeName(string simpleName)
        {
            switch (simpleName)
            {
                case "RectangleContainer": return LegacyElementKind.Rectangle;
                case "EllipseContainer": return LegacyElementKind.Ellipse;
                case "LineContainer": return LegacyElementKind.Line;
                case "ArrowContainer": return LegacyElementKind.Arrow;
                case "TextContainer": return LegacyElementKind.Text;
                case "SpeechbubbleContainer": return LegacyElementKind.Speechbubble;
                case "StepLabelContainer": return LegacyElementKind.StepLabel;
                case "FreehandContainer": return LegacyElementKind.Freehand;
                case "HighlightContainer": return LegacyElementKind.Highlight;
                case "ObfuscateContainer": return LegacyElementKind.Obfuscate;
                case "ImageContainer": return LegacyElementKind.Image;
                case "IconContainer": return LegacyElementKind.Icon;
                case "MetafileContainer": return LegacyElementKind.Metafile;
                case "SvgContainer": return LegacyElementKind.Svg;
                case "EmojiContainer": return LegacyElementKind.Emoji;
                case "CursorContainer": return LegacyElementKind.Cursor;
                default: return LegacyElementKind.Unknown;
            }
        }

        /// <summary>
        /// The type name without the namespace, nested types keep their outer type: "FilterContainer+PreparedFilter"
        /// </summary>
        private static string SimpleName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return fullName;
            }

            int genericStart = fullName.IndexOf('[');
            string withoutGenerics = genericStart > 0 ? fullName.Substring(0, genericStart) : fullName;
            int lastDot = withoutGenerics.LastIndexOf('.');
            return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
        }

        /// <summary>
        /// Find the member with the exact name, or the name prefixed by a (base) class name "Class+name",
        /// or the backing field of an auto property "&lt;name&gt;k__BackingField"
        /// </summary>
        private static string FindMember(ClassRecord record, string name)
        {
            string prefixedName = "+" + name;
            string backingField = "<" + name + ">k__BackingField";
            string candidate = null;
            foreach (var memberName in record.MemberNames)
            {
                if (memberName == name || memberName == backingField)
                {
                    return memberName;
                }

                if (candidate == null && (memberName.EndsWith(prefixedName, StringComparison.Ordinal) || memberName.EndsWith("+" + backingField, StringComparison.Ordinal)))
                {
                    candidate = memberName;
                }
            }

            return candidate;
        }

        private static object GetRaw(ClassRecord record, string name)
        {
            if (record == null)
            {
                return null;
            }

            var memberName = FindMember(record, name);
            return memberName == null ? null : record.GetRawValue(memberName);
        }

        private static ClassRecord GetRecord(ClassRecord record, string name) => GetRaw(record, name) as ClassRecord;

        private static string GetString(ClassRecord record, string name)
        {
            return GetRaw(record, name) switch
            {
                string value => value,
                PrimitiveTypeRecord primitive => primitive.Value as string,
                _ => null
            };
        }

        private static int GetInt32(ClassRecord record, string name, int defaultValue = 0)
        {
            var raw = GetRaw(record, name);
            if (raw is PrimitiveTypeRecord primitive)
            {
                raw = primitive.Value;
            }

            return raw is IConvertible and not string ? Convert.ToInt32(raw) : defaultValue;
        }

        private static bool GetBoolean(ClassRecord record, string name)
        {
            var raw = GetRaw(record, name);
            if (raw is PrimitiveTypeRecord primitive)
            {
                raw = primitive.Value;
            }

            return raw is bool value && value;
        }

        private static LegacyPoint GetPoint(ClassRecord record, string name)
        {
            var pointRecord = GetRecord(record, name);
            return pointRecord == null ? default : ToPoint(pointRecord);
        }

        private static LegacyPoint ToPoint(ClassRecord pointRecord) => new LegacyPoint(GetInt32(pointRecord, "x"), GetInt32(pointRecord, "y"));

        private static byte[] GetBytes(ClassRecord record, string name)
        {
            return GetRaw(record, name) is SZArrayRecord<byte> bytes ? bytes.GetArray() : null;
        }
    }
}
