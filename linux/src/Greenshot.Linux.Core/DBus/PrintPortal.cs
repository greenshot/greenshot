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

using Greenshot.Linux.Core.Output;
using Tmds.DBus.Protocol;

namespace Greenshot.Linux.Core.DBus;

/// <summary>
/// The print dialog of the desktop and printing through it (org.freedesktop.portal.Print)
/// </summary>
public sealed class PrintPortal(DesktopPortal portal)
{
    private const string PrintInterface = "org.freedesktop.portal.Print";

    public static async Task<bool> IsAvailableAsync(DesktopPortal portal) =>
        await portal.GetVersionAsync(PrintInterface).ConfigureAwait(false) != null;

    /// <summary>
    /// Show the print dialog; the paper the user chose and the token for <see cref="PrintAsync"/>, null when cancelled
    /// </summary>
    public async Task<(PageSetup Page, uint Token)?> PrepareAsync(string title, CancellationToken cancellationToken = default)
    {
        var response = await portal.RequestAsync(PrintInterface, "PreparePrint", "ssa{sv}a{sv}a{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteString(string.Empty);
            writer.WriteString(title);
            writer.WriteOptions(new Dictionary<string, object>());
            writer.WriteOptions(new Dictionary<string, object>());
            writer.WriteOptions(new Dictionary<string, object> { ["handle_token"] = token, ["modal"] = true });
        }, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || !response.Results.TryGetValue("token", out var tokenValue))
        {
            return null;
        }

        var page = PageSetup.A4;
        if (response.Results.TryGetValue("page-setup", out var setup))
        {
            page = ReadPageSetup(setup) ?? page;
        }

        return (page, tokenValue.GetUInt32());
    }

    /// <summary>
    /// Print a PDF with the settings chosen in <see cref="PrepareAsync"/>
    /// </summary>
    public async Task<bool> PrintAsync(string title, string pdfPath, uint printToken, CancellationToken cancellationToken = default)
    {
        using var handle = File.OpenHandle(pdfPath, FileMode.Open, FileAccess.Read);
        var response = await portal.RequestAsync(PrintInterface, "Print", "ssha{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteString(string.Empty);
            writer.WriteString(title);
            writer.WriteHandle(handle);
            writer.WriteOptions(new Dictionary<string, object> { ["handle_token"] = token, ["token"] = printToken, ["modal"] = true });
        }, cancellationToken).ConfigureAwait(false);
        return response.IsSuccess;
    }

    /// <summary>
    /// The page-setup of the portal: Width, Height and the margins in mm, and the Orientation
    /// </summary>
    private static PageSetup? ReadPageSetup(VariantValue setup)
    {
        var values = new Dictionary<string, VariantValue>();
        for (int i = 0; i < setup.Count; i++)
        {
            var entry = setup.GetDictionaryEntry(i);
            values[entry.Key.GetString()] = entry.Value.GetVariantValue();
        }

        double Get(string key, double fallback) =>
            values.TryGetValue(key, out var value) && value.Type == VariantValueType.Double ? value.GetDouble() : fallback;

        double width = Get("Width", 0), height = Get("Height", 0);
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        double left = Get("MarginLeft", 10), top = Get("MarginTop", 10), right = Get("MarginRight", 10), bottom = Get("MarginBottom", 10);
        var orientation = values.TryGetValue("Orientation", out var o) && o.Type == VariantValueType.String ? o.GetString() : "portrait";
        if (orientation.Contains("landscape", StringComparison.OrdinalIgnoreCase))
        {
            (width, height) = (height, width);
            (left, top, right, bottom) = (top, right, bottom, left);
        }

        return PageSetup.FromMillimeters(width, height, left, top, right, bottom);
    }
}
