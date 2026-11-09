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
using System.Diagnostics;
using System.Linq;
using log4net;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// Opens the target of an item: only web addresses (http, https), e-mail (mailto) and phone numbers (tel) are handed to Windows,
/// anything else is copy-only. The target isn't logged, it comes from the capture.
/// </summary>
public static class SmartTextActions
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextActions));

    private static readonly string[] AllowedSchemes = { Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto, "tel" };

    /// <summary>
    /// Can the target be opened: an absolute address with one of the allowed schemes
    /// </summary>
    public static bool CanOpen(string target) =>
        !string.IsNullOrWhiteSpace(target) && Uri.TryCreate(target.Trim(), UriKind.Absolute, out var uri) &&
        AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Open the target with the program Windows has for it (browser, mail program, phone app)
    /// </summary>
    /// <returns>false when it can't be opened</returns>
    public static bool Open(string target)
    {
        if (!CanOpen(target)) return false;
        try
        {
            using var process = Process.Start(new ProcessStartInfo(target.Trim()) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.WarnFormat("Can't open the {0} address of a Smart text item: {1}", new Uri(target.Trim()).Scheme, ex.Message);
            return false;
        }
    }
}
