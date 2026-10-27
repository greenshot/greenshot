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

using System.Buffers.Binary;
using System.Text;
using SkiaSharp;
using Svg.Skia;

namespace Greenshot.Linux.Editor.Rendering;

/// <summary>
/// Images for elements from other sources than a capture: SVG, icons and cursors (.ico, .cur), the cursors of the desktop's
/// cursor theme and color emoji. The caller owns the bitmaps.
/// </summary>
public static class ImageSources
{
    /// <summary>
    /// The Windows cursor names of recipes with the names of the freedesktop cursor themes
    /// </summary>
    private static readonly Dictionary<string, string[]> CursorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arrow"] = ["default", "left_ptr", "arrow"],
        ["AppStarting"] = ["progress", "left_ptr_watch"],
        ["Cross"] = ["crosshair", "cross"],
        ["Hand"] = ["pointer", "hand2", "pointing_hand"],
        ["Help"] = ["help", "question_arrow"],
        ["IBeam"] = ["text", "xterm"],
        ["No"] = ["not-allowed", "crossed_circle", "forbidden"],
        ["SizeAll"] = ["move", "fleur", "all-scroll"],
        ["SizeNESW"] = ["nesw-resize", "fd_double_arrow", "size_bdiag"],
        ["SizeNS"] = ["ns-resize", "sb_v_double_arrow", "size_ver"],
        ["SizeNWSE"] = ["nwse-resize", "bd_double_arrow", "size_fdiag"],
        ["SizeWE"] = ["ew-resize", "sb_h_double_arrow", "size_hor"],
        ["UpArrow"] = ["up-arrow", "sb_up_arrow", "center_ptr"],
        ["Wait"] = ["wait", "watch"],
    };

    public static bool IsSvg(byte[] bytes)
    {
        string start = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
        return start.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A bitmap or SVG image; an SVG is drawn in the size, or its own; null when it can't be read
    /// </summary>
    public static SKBitmap? Decode(byte[] bytes, int width = 0, int height = 0)
    {
        if (IsSvg(bytes))
        {
            return RenderSvg(bytes, width, height);
        }

        var bitmap = SKBitmap.Decode(bytes);
        if (bitmap == null && bytes.Length > 6 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 2 && bytes[3] == 0)
        {
            // A .cur is an .ico with type 2 and hotspots instead of planes and bit counts: as icon Skia reads it
            var icon = (byte[])bytes.Clone();
            icon[2] = 1;
            bitmap = SKBitmap.Decode(icon);
        }

        return bitmap;
    }

    private static SKBitmap? RenderSvg(byte[] bytes, int width, int height)
    {
        using var svg = new SKSvg();
        using var stream = new MemoryStream(bytes);
        var picture = svg.Load(stream);
        if (picture == null)
        {
            return null;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }

        if (width <= 0 && height <= 0)
        {
            width = (int)Math.Ceiling(bounds.Width);
            height = (int)Math.Ceiling(bounds.Height);
        }
        else if (width <= 0)
        {
            width = (int)Math.Ceiling(bounds.Width * height / bounds.Height);
        }
        else if (height <= 0)
        {
            height = (int)Math.Ceiling(bounds.Height * width / bounds.Width);
        }

        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(width / bounds.Width, height / bounds.Height);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        return bitmap;
    }

    /// <summary>
    /// The cursor of the desktop's theme for a Windows cursor name (Arrow, Hand, IBeam, ...), with its hotspot; null without a theme
    /// </summary>
    public static (SKBitmap Image, SKPointI HotSpot)? LoadCursor(string? name, int size = 32)
    {
        if (!CursorNames.TryGetValue(string.IsNullOrWhiteSpace(name) ? "Arrow" : name.Trim(), out var names))
        {
            names = CursorNames["Arrow"];
        }

        foreach (var directory in CursorDirectories())
        {
            foreach (var cursorName in names)
            {
                string path = Path.Combine(directory, cursorName);
                if (File.Exists(path) && ReadXcursor(File.ReadAllBytes(path), size) is { } cursor)
                {
                    return cursor;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The cursor directories of the theme (XCURSOR_THEME, the GNOME setting, default) and the themes it inherits from
    /// </summary>
    private static IEnumerable<string> CursorDirectories()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] roots = [Path.Combine(home, ".icons"), Path.Combine(home, ".local", "share", "icons"), "/usr/share/icons", "/usr/local/share/icons"];
        var themes = new List<string>();
        void Add(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme) || themes.Contains(theme))
            {
                return;
            }

            themes.Add(theme);
            foreach (var root in roots)
            {
                string index = Path.Combine(root, theme, "index.theme");
                if (!File.Exists(index))
                {
                    continue;
                }

                var inherits = File.ReadLines(index).FirstOrDefault(l => l.StartsWith("Inherits=", StringComparison.Ordinal));
                foreach (var parent in inherits?.Substring("Inherits=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
                {
                    Add(parent);
                }
            }
        }

        Add(Environment.GetEnvironmentVariable("XCURSOR_THEME"));
        Add("default");
        Add("Adwaita");
        Add("breeze_cursors");
        foreach (var theme in themes)
        {
            foreach (var root in roots)
            {
                string directory = Path.Combine(root, theme, "cursors");
                if (Directory.Exists(directory))
                {
                    yield return directory;
                }
            }
        }
    }

    /// <summary>
    /// The image of an Xcursor file in the size closest to the requested one
    /// </summary>
    internal static (SKBitmap Image, SKPointI HotSpot)? ReadXcursor(byte[] data, int size)
    {
        const uint ImageType = 0xFFFD0002;
        if (data.Length < 16 || Encoding.ASCII.GetString(data, 0, 4) != "Xcur")
        {
            return null;
        }

        uint Read(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
        int headerSize = (int)Read(4);
        int entries = (int)Read(12);
        int best = -1;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < entries; i++)
        {
            int entry = headerSize + i * 12;
            if (entry + 12 > data.Length || Read(entry) != ImageType)
            {
                continue;
            }

            int distance = Math.Abs((int)Read(entry + 4) - size);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = (int)Read(entry + 8);
            }
        }

        if (best < 0 || best + 36 > data.Length)
        {
            return null;
        }

        int width = (int)Read(best + 16);
        int height = (int)Read(best + 20);
        var hotSpot = new SKPointI((int)Read(best + 24), (int)Read(best + 28));
        int pixels = best + 36;
        if (width <= 0 || height <= 0 || width > 1024 || height > 1024 || pixels + width * height * 4 > data.Length)
        {
            return null;
        }

        // ARGB, premultiplied, little endian: the bytes of Bgra8888
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
        System.Runtime.InteropServices.Marshal.Copy(data, pixels, bitmap.GetPixels(), width * height * 4);
        return (bitmap, hotSpot);
    }

    /// <summary>
    /// A simple arrow, when the desktop has no cursor theme
    /// </summary>
    public static SKBitmap DrawArrowCursor(int size = 32)
    {
        var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        float s = size / 32f;
        using var path = new SKPath();
        path.MoveTo(2 * s, 2 * s);
        path.LineTo(2 * s, 24 * s);
        path.LineTo(8 * s, 18 * s);
        path.LineTo(12 * s, 28 * s);
        path.LineTo(16 * s, 26 * s);
        path.LineTo(12 * s, 17 * s);
        path.LineTo(20 * s, 17 * s);
        path.Close();
        using var fill = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var outline = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f * s };
        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, outline);
        return bitmap;
    }

    /// <summary>
    /// An emoji in the color emoji font of the desktop, in a square of the size
    /// </summary>
    public static SKBitmap RenderEmoji(string emoji, int size)
    {
        size = Math.Max(8, size);
        int codepoint = char.ConvertToUtf32(emoji, 0);
        using var typeface = SKFontManager.Default.MatchCharacter(codepoint) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, size * 0.8f);
        using var paint = new SKPaint { IsAntialias = true };
        var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        font.MeasureText(emoji, out var bounds, paint);
        canvas.DrawText(emoji, (size - bounds.Width) / 2 - bounds.Left, (size - bounds.Height) / 2 - bounds.Top, font, paint);
        return bitmap;
    }
}
