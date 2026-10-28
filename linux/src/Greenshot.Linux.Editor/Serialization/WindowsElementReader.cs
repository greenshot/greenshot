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

using System.Formats.Nrbf;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Serialization;

/// <summary>
/// The elements of a .greenshot file of the Windows editor, which writes them with the BinaryFormatter. They are read with
/// the NRBF decoder, which only reads the records and never creates types from the file, and the known containers are
/// turned into the elements of this editor: so a file can't run code, unknown things are left out.
/// </summary>
public static class WindowsElementReader
{
    /// <summary>
    /// What was read: the elements, the number of the first step label, and what couldn't be taken over
    /// </summary>
    public sealed record Result(List<DrawableElement> Elements, int CounterStart, IReadOnlyList<string> Skipped);

    public static Result Read(byte[] data, int offset, int length)
    {
        using var stream = new MemoryStream(data, offset, length, writable: false);
        var root = NrbfDecoder.DecodeClassRecord(stream, new PayloadOptions { UndoTruncatedTypeNames = true });
        var elements = new List<DrawableElement>();
        var skipped = new List<string>();
        int counterStart = 1;
        foreach (var container in Items(root))
        {
            string type = ShortTypeName(container);
            try
            {
                var element = Convert(container, type, ref counterStart);
                if (element != null)
                {
                    elements.Add(element);
                }
                else
                {
                    skipped.Add(type);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or InvalidCastException or FormatException or ArgumentException)
            {
                Core.Log.Warn($"An element {type} of a Windows .greenshot file can't be read: {ex.Message}");
                skipped.Add(type);
            }
        }

        return new Result(elements, counterStart, skipped);
    }

    /// <summary>
    /// The containers of the DrawableContainerList, a List&lt;IDrawableContainer&gt;: its _items up to _size
    /// </summary>
    private static IEnumerable<ClassRecord> Items(ClassRecord list)
    {
        int size = Member(list, "_size") is int count ? count : int.MaxValue;
        if (Member(list, "_items") is not SZArrayRecord<SerializationRecord> items)
        {
            yield break;
        }

        foreach (var item in items.GetArray().Take(size))
        {
            if (item is ClassRecord container)
            {
                yield return container;
            }
        }
    }

    private static string ShortTypeName(SerializationRecord record)
    {
        string name = record is ClassRecord classRecord ? classRecord.TypeName.FullName : record.RecordType.ToString();
        int bracket = name.IndexOf('[');
        name = bracket >= 0 ? name[..bracket] : name;
        return name[(name.LastIndexOfAny(['.', '+']) + 1)..];
    }

    /// <summary>
    /// A member by its name; the fields of a base class are stored as "BaseClass+name"
    /// </summary>
    private static object? Member(ClassRecord record, string name)
    {
        string? found = record.MemberNames.FirstOrDefault(m => m == name)
                        ?? record.MemberNames.FirstOrDefault(m => m.EndsWith("+" + name, StringComparison.Ordinal))
                        ?? record.MemberNames.FirstOrDefault(m => m == $"<{name}>k__BackingField" || m.EndsWith($"+<{name}>k__BackingField", StringComparison.Ordinal));
        if (found == null)
        {
            return null;
        }

        var value = record.GetRawValue(found);
        return value is PrimitiveTypeRecord primitive ? primitive.Value : value;
    }

    private static int Int(ClassRecord record, string name, int fallback = 0) => Member(record, name) switch
    {
        int value => value,
        long value => (int)value,
        short value => value,
        _ => fallback,
    };

    /// <summary>
    /// The field values of the container and of its children (the filters), by the name of the field type
    /// </summary>
    private static Dictionary<string, object?> FieldValues(ClassRecord container)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        void Collect(ClassRecord holder, bool overwrite)
        {
            if (Member(holder, "fields") is ClassRecord fieldList)
            {
                foreach (var field in Items(fieldList))
                {
                    if (Member(field, "FieldType") is ClassRecord fieldType && Member(fieldType, "Name") is string name && (overwrite || !values.ContainsKey(name)))
                    {
                        values[name] = Value(Member(field, "_myValue"));
                    }
                }
            }

            if (Member(holder, "Children") is ClassRecord children)
            {
                foreach (var child in Items(children))
                {
                    Collect(child, overwrite: false);
                }
            }
        }

        Collect(container, overwrite: true);
        return values;
    }

    /// <summary>
    /// A field value: a primitive, an enum (by its name when known), a color or a string
    /// </summary>
    private static object? Value(object? raw)
    {
        if (raw is not ClassRecord record)
        {
            return raw;
        }

        string type = ShortTypeName(record);
        if (type == "Color")
        {
            return Color(record);
        }

        // A boxed enum: its number
        return record.HasMember("value__") ? Member(record, "value__") : record;
    }

    /// <summary>
    /// System.Drawing.Color: the ARGB value, or a known color (Color.Red, ...) by its number
    /// </summary>
    private static SKColor Color(ClassRecord color)
    {
        const short stateKnownColorValid = 1;
        const short stateValueValid = 2;
        short state = (short)Int(color, "state");
        if ((state & stateKnownColorValid) != 0)
        {
            var known = System.Drawing.Color.FromKnownColor((System.Drawing.KnownColor)Int(color, "knownColor"));
            return new SKColor((uint)known.ToArgb());
        }

        long value = Member(color, "value") is long argb ? argb : 0;
        return (state & stateValueValid) != 0 ? new SKColor((uint)value) : SKColors.Transparent;
    }

    private static SKPoint Point(object? raw) =>
        raw is ClassRecord point ? new SKPoint(Int(point, "x"), Int(point, "y")) : SKPoint.Empty;

    /// <summary>
    /// The PNG of a System.Drawing.Bitmap (ISerializable: "Data") or the data of an icon ("IconData")
    /// </summary>
    private static byte[]? ImageData(object? raw) => raw is ClassRecord image
        ? (Member(image, "Data") ?? Member(image, "IconData")) is SZArrayRecord<byte> bytes ? bytes.GetArray() : null
        : null;

    private static DrawableElement? Convert(ClassRecord container, string type, ref int counterStart)
    {
        int left = Int(container, "left"), top = Int(container, "top"), width = Int(container, "width"), height = Int(container, "height");
        var bounds = new SKRect(left, top, left + width, top + height);
        var values = FieldValues(container);
        DrawableElement? element = type switch
        {
            "RectangleContainer" => new RectangleElement(),
            "EllipseContainer" => new EllipseElement(),
            "LineContainer" => new LineElement(),
            "ArrowContainer" => new ArrowElement(),
            "TextContainer" => new TextElement { Text = Member(container, "text") as string ?? string.Empty },
            "SpeechbubbleContainer" => new SpeechBubbleElement
            {
                Text = Member(container, "text") as string ?? string.Empty,
                Target = Point(Member(container, "_storedTargetGripperLocation")),
            },
            "StepLabelContainer" => new StepLabelElement(),
            "HighlightContainer" => new HighlightElement(),
            "ObfuscateContainer" => new ObfuscateElement(),
            "FreehandContainer" => Freehand(container, left, top),
            "EmojiContainer" => new EmojiElement { Emoji = Member(container, "_emoji") as string ?? EmojiElement.DefaultEmoji },
            "CutMarkContainer" => CutMark(container),
            "TornEdgeContainer" => TornEdges(container),
            // A barcode of the Zxing plugin keeps only its picture in a file
            "ImageContainer" or "IconContainer" or "CursorContainer" or "BarcodeContainer" => Image(container, bounds),
            "SvgContainer" => Svg(container, bounds),
            _ => null,
        };
        if (element == null)
        {
            return null;
        }

        if (element is StepLabelElement && Int(container, "_counterStart", 1) is var start)
        {
            counterStart = start;
        }

        if (element is not FreehandElement)
        {
            element.Bounds = bounds;
        }

        element.Style = element.CreateDefaultStyle();
        ApplyFields(element, values);
        return element;
    }

    /// <summary>
    /// The field values of Windows, as far as the element uses them
    /// </summary>
    private static void ApplyFields(DrawableElement element, Dictionary<string, object?> values)
    {
        // Twice: which fields a filter has depends on its mode (blur radius or pixel size), which is a field too
        ApplyFieldsOnce(element, values);
        ApplyFieldsOnce(element, values);
    }

    private static void ApplyFieldsOnce(DrawableElement element, Dictionary<string, object?> values)
    {
        foreach (var field in element.Fields.ToList())
        {
            if (!values.TryGetValue(field.WindowsName(), out var value) || value == null)
            {
                continue;
            }

            object? converted = field switch
            {
                FieldType.ArrowHeads when value is int heads => (ArrowHeads)heads,
                // StringAlignment: Near, Center, Far
                FieldType.TextHorizontalAlignment when value is int alignment => (TextHorizontalAlignment)Math.Clamp(alignment, 0, 2),
                FieldType.TextVerticalAlignment when value is int alignment => (TextVerticalAlignment)Math.Clamp(alignment, 0, 2),
                FieldType.HighlightFilter when value is int filter => filter switch
                {
                    3 => HighlightFilter.AreaHighlight,
                    4 => HighlightFilter.Grayscale,
                    5 => HighlightFilter.Magnification,
                    _ => HighlightFilter.TextHighlight,
                },
                FieldType.ObfuscateFilter when value is int filter => filter == 0 ? ObfuscateFilter.Blur : ObfuscateFilter.Pixelize,
                FieldType.CutMarkStyle when value is int style => (CutMarkStyle)Math.Clamp(style, 0, 4),
                FieldType.FontSize when value is float or double or int => System.Convert.ToSingle(value),
                _ => value,
            };
            try
            {
                element.Style.Set(field, converted);
            }
            catch (InvalidCastException)
            {
                // A value of another type than this editor uses, the default stays
            }
        }
    }

    private static FreehandElement Freehand(ClassRecord container, int left, int top)
    {
        var element = new FreehandElement();
        if (Member(container, "capturePoints") is ClassRecord list)
        {
            // The points are relative to the place of the element, it is drawn moved by left and top
            element.Points = Items(list).Select(p => new SKPoint(Int(p, "x") + left, Int(p, "y") + top)).ToList();
        }

        if (element.Points.Count > 0)
        {
            element.Bounds = new SKRect(element.Points.Min(p => p.X), element.Points.Min(p => p.Y), element.Points.Max(p => p.X), element.Points.Max(p => p.Y));
        }

        return element;
    }

    private static void ReadShadow(ClassRecord container, CutElement element)
    {
        element.Seed = Int(container, "_seed", element.Seed);
        element.ShadowDarkness = Member(container, "_shadowDarkness") is float darkness ? darkness : element.ShadowDarkness;
        element.ShadowSize = Int(container, "_shadowSize", element.ShadowSize);
        element.ShadowOffsetX = Int(container, "_shadowOffsetX", element.ShadowOffsetX);
        element.ShadowOffsetY = Int(container, "_shadowOffsetY", element.ShadowOffsetY);
    }

    private static CutMarkElement CutMark(ClassRecord container)
    {
        var element = new CutMarkElement { Horizontal = Member(container, "_horizontal") is not false };
        ReadShadow(container, element);
        return element;
    }

    private static TornEdgeElement TornEdges(ClassRecord container)
    {
        var element = new TornEdgeElement();
        ReadShadow(container, element);
        if (Member(container, "_edges") is SZArrayRecord<bool> edges && edges.GetArray() is { Length: 4 } edgeValues)
        {
            element.Edges = edgeValues;
        }

        if (Member(container, "_margins") is SZArrayRecord<int> margins && margins.GetArray() is { Length: 4 } marginValues)
        {
            element.Margins = marginValues;
        }

        return element;
    }

    /// <summary>
    /// A pasted image, an icon or the mouse cursor: an image element with the picture
    /// </summary>
    private static ImageElement? Image(ClassRecord container, SKRect bounds)
    {
        var data = ImageData(Member(container, "_image") ?? Member(container, "icon"));
        if (data == null && Member(container, "savedCursor") is ClassRecord cursor)
        {
            data = ImageData(Member(cursor, "ColorLayer"));
        }

        using var bitmap = data == null ? null : SKBitmap.Decode(data);
        return bitmap == null ? null : ImageElement.FromBitmap(bitmap, new SKPoint(bounds.Left, bounds.Top));
    }

    /// <summary>
    /// An SVG: drawn into an image element of its size
    /// </summary>
    private static ImageElement? Svg(ClassRecord container, SKRect bounds)
    {
        if (Member(container, "_svgContent") is not ClassRecord memoryStream || Member(memoryStream, "_buffer") is not SZArrayRecord<byte> buffer)
        {
            return null;
        }

        int length = Int(memoryStream, "_length", buffer.Length);
        using var svgStream = new MemoryStream(buffer.GetArray(), 0, Math.Min(length, buffer.Length));
        using var svg = new Svg.Skia.SKSvg();
        if (svg.Load(svgStream) is not { } picture)
        {
            return null;
        }

        var rect = bounds.Standardized;
        int width = Math.Max(1, (int)rect.Width), height = Math.Max(1, (int)rect.Height);
        using var bitmap = ImageEffects.NewBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Scale(width / Math.Max(1, picture.CullRect.Width), height / Math.Max(1, picture.CullRect.Height));
            canvas.DrawPicture(picture);
        }

        return ImageElement.FromBitmap(bitmap, new SKPoint(rect.Left, rect.Top));
    }
}
