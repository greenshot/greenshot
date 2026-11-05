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

using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.CustomUploader.Destinations;

/// <summary>
/// The icons of the destinations: the icon of the service, fetched at the import, else the generic upload icon.
/// Key "customuploader:" ("Custom uploader": the icon of the only uploader, else the generic one) or "customuploader:name".
/// </summary>
internal sealed class CustomUploaderIconProvider : IIconProvider
{
    private const string Prefix = "customuploader:";

    /// <summary>The generic upload icon</summary>
    public const string GenericKey = Prefix;

    public static string KeyFor(string uploaderName) => Prefix + uploaderName;

    public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(Prefix, System.StringComparison.Ordinal);

    public Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
    {
        string name = iconKey.Substring(Prefix.Length);
        var store = CustomUploaderPlugin.CreateStore();
        // "Custom uploader" itself: with one uploader that is what it uploads with, so it gets its icon
        var uploader = name.Length > 0 ? store.Find(name) : SingleUploader(store);
        byte[] png = store.GetIcon(uploader?.Id);
        if (png == null && uploader?.Definition != null)
        {
            // Shown later, the menus ask again the next time
            ServiceIcon.FetchInBackground(store, uploader.Id, uploader.Definition);
        }

        return Task.FromResult(png == null ? GenericIcon() : FromPng(png) ?? GenericIcon());
    }

    private static StoredUploader SingleUploader(UploaderStore store)
    {
        var uploaders = store.GetUploaders().Where(uploader => uploader.Definition != null).ToList();
        return uploaders.Count == 1 ? uploaders[0] : null;
    }

    private static Image GenericIcon() => EmbeddedResources.GetImage(typeof(CustomUploaderPlugin), "Upload");

    /// <summary>
    /// A new bitmap, the stream isn't kept (an Image from a stream needs it as long as it lives)
    /// </summary>
    private static Image FromPng(byte[] png)
    {
        try
        {
            using var stream = new MemoryStream(png);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }
}
