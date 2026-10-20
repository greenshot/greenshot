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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Plugin.Stamps.Library;

/// <summary>
/// What is done with the image of a stamp, with the file format handlers of Greenshot: so every format Greenshot can open
/// is a stamp, and an SVG stays a vector element in the editor.
/// </summary>
internal static class StampImages
{
    private const string FormatSvg = "image/svg+xml";

    private static IEnumerable<IFileFormatHandler> Handlers => SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

    /// <summary>
    /// The extensions of the files which can be stamps: an element and a preview can be made of them.
    /// A .greenshot file is a capture with its elements, not a stamp.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedExtensions()
    {
        var handlers = Handlers.ToList();
        return handlers.ExtensionsFor(FileFormatHandlerActions.LoadDrawableFromStream)
            .Intersect(handlers.ExtensionsFor(FileFormatHandlerActions.LoadFromStream), StringComparer.OrdinalIgnoreCase)
            .Where(extension => !string.Equals(extension, ".greenshot", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// The image of the stamp, the caller disposes it
    /// </summary>
    /// <returns>Bitmap, null when the file can't be read</returns>
    public static Bitmap LoadBitmap(Stamp stamp)
    {
        using var stream = File.OpenRead(stamp.FullPath);
        return Handlers.TryLoadFromStream(stream, stamp.Extension, out var bitmap) ? bitmap : null;
    }

    /// <summary>
    /// A preview of the stamp which fits into a square, for the list
    /// </summary>
    /// <param name="stamp">Stamp</param>
    /// <param name="size">the side of the square, in pixels</param>
    /// <returns>frozen BitmapSource, null when the file can't be read</returns>
    public static BitmapSource CreateThumbnail(Stamp stamp, int size)
    {
        using var bitmap = LoadBitmap(stamp);
        if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
        {
            return null;
        }

        double factor = Math.Min(1d, Math.Min(size / (double)bitmap.Width, size / (double)bitmap.Height));
        int width = Math.Max(1, (int)Math.Round(bitmap.Width * factor));
        int height = Math.Max(1, (int)Math.Round(bitmap.Height * factor));
        using var thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(thumbnail))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(bitmap, 0, 0, width, height);
        }

        thumbnail.SetResolution(96, 96);
        return thumbnail.ToBitmapSource();
    }

    /// <summary>
    /// The editable elements of the stamp for the surface: an image, an icon, or an SVG which stays a vector
    /// </summary>
    public static IList<IDrawableContainer> LoadElements(Stamp stamp, ISurface surface)
    {
        using var stream = File.OpenRead(stamp.FullPath);
        // Read now, the handlers can read the stream lazily
        return Handlers.LoadDrawablesFromStream(stream, stamp.Extension, surface).ToList();
    }

    /// <summary>
    /// Put the stamp on the clipboard for other programs: the file (Explorer, Office, chat programs), PNG and DIBV5 with the transparency,
    /// and the SVG itself for programs which read it
    /// </summary>
    public static async Task CopyToClipboardAsync(Stamp stamp, CancellationToken cancellationToken = default)
    {
        using var bitmap = LoadBitmap(stamp) ?? throw new InvalidDataException($"Can't read {stamp.FullPath}");
        using var content = ClipboardHelper.CreateContent(bitmap, new[] { ClipboardFormat.PNG, ClipboardFormat.DIBV5 });
        if (string.Equals(stamp.Extension, ".svg", StringComparison.OrdinalIgnoreCase))
        {
            content.Contents.AddBytes(File.ReadAllBytes(stamp.FullPath), FormatSvg);
        }

        content.Contents.AddFileNames(new[] { stamp.FullPath });
        await ClipboardHelper.SetClipboardDataAsync(content.Contents, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
