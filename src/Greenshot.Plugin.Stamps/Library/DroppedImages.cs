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
using System.Linq;
using Dapplo.Windows.Clipboard;
using Greenshot.Base.Core;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using DataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.IDataObject;

namespace Greenshot.Plugin.Stamps.Library;

/// <summary>
/// What was dropped on the stamps window, read during the drop: the data object is only valid then.
/// Files and folders (Explorer), else the virtual files (an image dragged from a browser), else the image urls and the image itself.
/// </summary>
internal sealed class DroppedImages : IDisposable
{
    private DroppedImages(IReadOnlyList<string> paths, IReadOnlyList<ImageFileContent> files, IReadOnlyList<string> urls, Bitmap image)
    {
        Paths = paths;
        Files = files;
        Urls = urls;
        Image = image;
    }

    /// <summary>
    /// The dropped files and folders
    /// </summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>
    /// The dropped virtual files with an image Greenshot can read, in their own format
    /// </summary>
    public IReadOnlyList<ImageFileContent> Files { get; }

    /// <summary>
    /// The urls of the dropped images (the img elements of the HTML, or the dropped url), to download the image in its own format
    /// </summary>
    public IReadOnlyList<string> Urls { get; }

    /// <summary>
    /// The dropped image as bitmap, for when the download doesn't work; null when there is none
    /// </summary>
    public Bitmap Image { get; private set; }

    /// <summary>
    /// Can the drop give stamps, for the cursor while dragging
    /// </summary>
    public static bool CanRead(WpfDataObject data)
    {
        if (data == null)
        {
            return false;
        }

        if (data.GetDataPresent(DataFormats.FileDrop))
        {
            return true;
        }

        if (data is not ComDataObject comDataObject)
        {
            return false;
        }

        using var reader = new DataObjectReader(comDataObject);
        return ClipboardHelper.ContainsImage(reader) || UrlOf(reader) != null;
    }

    /// <summary>
    /// Read what was dropped, only what is needed: the urls and the image only when there are no files
    /// </summary>
    public static DroppedImages Read(WpfDataObject data)
    {
        var paths = (data?.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>()).Where(path => !string.IsNullOrEmpty(path)).ToList();
        if (paths.Count > 0 || data is not ComDataObject comDataObject)
        {
            return new DroppedImages(paths, Array.Empty<ImageFileContent>(), Array.Empty<string>(), null);
        }

        using var reader = new DataObjectReader(comDataObject);
        var files = ClipboardHelper.GetVirtualImageFiles(reader).ToList();
        if (files.Count > 0)
        {
            return new DroppedImages(paths, files, Array.Empty<string>(), null);
        }

        // The img of the HTML first: a dropped url can be the link around the image
        var urls = ClipboardHelper.GetHtmlImageUrls(reader).Where(IsWebUrl).ToList();
        if (UrlOf(reader) is { } url)
        {
            urls.Add(url);
        }

        return new DroppedImages(paths, files, urls.Distinct(StringComparer.Ordinal).ToList(), ClipboardHelper.GetFirstImage(reader));
    }

    /// <summary>
    /// The dropped text, when it is a web url
    /// </summary>
    private static string UrlOf(IClipboardDataSource source)
    {
        if (!ClipboardHelper.ContainsText(source))
        {
            return null;
        }

        // Firefox: the url, a new line and the title
        string text = ClipboardHelper.GetText(source)?.Split('\r', '\n').FirstOrDefault()?.Trim();
        return IsWebUrl(text) ? text : null;
    }

    private static bool IsWebUrl(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
    }
}
