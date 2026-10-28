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

using System.Text;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Serialization;

/// <summary>
/// What was read from a .greenshot file
/// </summary>
public sealed record GreenshotFileContent(SKBitmap Image, string? ElementsJson, string? Warning);

/// <summary>
/// .greenshot files: the capture as PNG, then the elements, their length (Int64) and a 14 character marker.
/// The Windows editor writes the elements with the BinaryFormatter and the marker "Greenshot01.04", these are read with
/// <see cref="WindowsElementReader"/>; this one writes JSON and the marker "GreenshotJ1.00". Any image viewer shows the file, it starts with a PNG.
/// </summary>
public static class GreenshotFile
{
    public const string Extension = ".greenshot";
    private const int MarkerSize = 14;
    private const string JsonMarker = "GreenshotJ1.00";

    public static void Save(Stream stream, SKBitmap image, string elementsJson)
    {
        using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
        {
            png.SaveTo(stream);
        }

        var json = Encoding.UTF8.GetBytes(elementsJson);
        stream.Write(json);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write((long)json.Length);
        writer.Write(Encoding.ASCII.GetBytes(JsonMarker));
    }

    public static void Save(string path, SKBitmap image, string elementsJson)
    {
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            Save(stream, image, elementsJson);
        }

        File.Move(temp, path, overwrite: true);
    }

    public static GreenshotFileContent Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static GreenshotFileContent Load(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var data = memory.ToArray();

        var image = SKBitmap.Decode(data) ?? throw new InvalidDataException("The file has no image");
        if (data.Length < MarkerSize + 8)
        {
            return new GreenshotFileContent(image, null, "The file has no elements");
        }

        var marker = Encoding.ASCII.GetString(data, data.Length - MarkerSize, MarkerSize);
        if (!marker.StartsWith("Greenshot", StringComparison.Ordinal))
        {
            return new GreenshotFileContent(image, null, "This is not a Greenshot file, only the image was loaded");
        }

        long length = BitConverter.ToInt64(data, data.Length - MarkerSize - 8);
        long start = data.Length - MarkerSize - 8 - length;
        if (length < 0 || start < 0)
        {
            return new GreenshotFileContent(image, null, "The elements in the file are damaged, only the image was loaded");
        }

        if (marker == JsonMarker)
        {
            return new GreenshotFileContent(image, Encoding.UTF8.GetString(data, (int)start, (int)length), null);
        }

        // Written by the Windows editor with the BinaryFormatter: read without creating types from the file
        try
        {
            var windows = WindowsElementReader.Read(data, (int)start, (int)length);
            string? warning = windows.Skipped.Count == 0
                ? null
                : $"Some elements of this file of Greenshot for Windows can't be shown here and were left out: {string.Join(", ", windows.Skipped.Distinct())}";
            return new GreenshotFileContent(image, ElementSerializer.Serialize(windows.Elements, windows.CounterStart), warning);
        }
        catch (Exception ex) when (ex is System.Runtime.Serialization.SerializationException or NotSupportedException or InvalidOperationException
                                       or EndOfStreamException or ArgumentException or OverflowException)
        {
            Core.Log.Warn($"The elements of the Windows .greenshot file ({marker}) can't be read: {ex.Message}");
            return new GreenshotFileContent(image, null,
                $"The elements of this file of Greenshot for Windows ({marker}) can't be read, only the image was loaded");
        }
    }
}
