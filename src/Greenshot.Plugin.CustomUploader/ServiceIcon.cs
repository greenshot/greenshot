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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Threading;
using Greenshot.Plugin.CustomUploader.Sxcu;
using log4net;

namespace Greenshot.Plugin.CustomUploader;

/// <summary>
/// The icon of the service, fetched once at the import with <see cref="SiteIcon"/>: of the server the user approved, else
/// of its parent domain (api.example.com, example.com). Stored as a 32x32 PNG.
/// </summary>
internal static class ServiceIcon
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceIcon));

    // The uploaders (ids) whose icon was fetched since Greenshot started: one try per start, not one per menu
    private static readonly ConcurrentDictionary<string, bool> Fetched = new ConcurrentDictionary<string, bool>();

    /// <summary>
    /// Fetch and keep the icon of the uploader in the background when it has none: after the import (again = true),
    /// or the first time Greenshot needs it (an import whose fetch failed, an uploader imported before icons existed).
    /// </summary>
    public static void FetchInBackground(UploaderStore store, string id, UploaderDefinition definition, bool again = false)
    {
        if (id == null || definition == null) return;
        if (again)
        {
            Fetched.TryRemove(id, out _);
        }

        if (!Fetched.TryAdd(id, true) || store.GetIcon(id) != null)
        {
            return;
        }

        FetchAndKeepAsync(store, id, definition).FireAndLog($"The icon of the uploader {definition.Name}", Log);
    }

    private static async Task FetchAndKeepAsync(UploaderStore store, string id, UploaderDefinition definition)
    {
        // UI event → background work, like an export (the proxy lookup of the first request can take a while)
        await ThreadPoolSwitch.SwitchToThreadPoolAsync();
        var png = await FetchAsync(HttpService.Current, definition, CancellationToken.None).ConfigureAwait(false);
        store.SetIcon(id, png);
        Log.InfoFormat("Icon of the uploader {0}: {1}", definition.Name, png == null ? "none found, the generic one is used" : "from the service");
    }

    /// <summary>
    /// The icon as PNG, null when the service has none Greenshot can read. Never throws, except for the cancellation.
    /// </summary>
    public static Task<byte[]> FetchAsync(HttpService httpService, UploaderDefinition definition, CancellationToken cancellationToken) =>
        SiteIcon.FetchAsync(httpService, UploaderFile.GetServer(definition), true, cancellationToken);
}
