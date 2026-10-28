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

using Greenshot.Linux.Core.DBus;
using SkiaSharp;
using Tmds.DBus.Protocol;

namespace Greenshot.Linux.Core.Capturing;

/// <summary>
/// Captures with the Screenshot portal, which every desktop offers (GNOME, KDE, wlroots, ...).
/// The portal writes a file and returns its URI; GNOME and KDE ask the user once for permission.
/// </summary>
public sealed class PortalScreenGrabber(DesktopPortal portal) : IScreenGrabber
{
    private const string ScreenshotInterface = "org.freedesktop.portal.Screenshot";

    public string Name => "xdg-desktop-portal";

    public bool SupportsCursor => false;

    public static async Task<bool> IsAvailableAsync(DesktopPortal portal) =>
        await portal.GetVersionAsync(ScreenshotInterface).ConfigureAwait(false) != null;

    public Task<SKBitmap> GrabAsync(ScreenLayout layout, bool includeCursor, CancellationToken cancellationToken = default) =>
        RequestAsync(interactive: false, cancellationToken);

    /// <summary>
    /// The desktop's own screenshot dialog, where the user picks a window or an area (GNOME and KDE): window capture on desktops
    /// which don't tell applications where windows are
    /// </summary>
    /// <exception cref="OperationCanceledException">The user cancelled the dialog</exception>
    public Task<SKBitmap> GrabInteractiveAsync(CancellationToken cancellationToken = default) => RequestAsync(interactive: true, cancellationToken);

    private async Task<SKBitmap> RequestAsync(bool interactive, CancellationToken cancellationToken)
    {
        var requestedAt = DateTime.UtcNow;
        var response = await portal.RequestAsync(ScreenshotInterface, "Screenshot", "sa{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteString(string.Empty);
            writer.WriteOptions(new Dictionary<string, object>
            {
                ["handle_token"] = token,
                ["modal"] = interactive,
                ["interactive"] = interactive,
            });
        }, cancellationToken).ConfigureAwait(false);

        // The dialog closed in another way (code 2), e.g. the area picker of wlroots was ended: nothing is captured either
        if (response.IsCancelled || (interactive && !response.IsSuccess))
        {
            throw new OperationCanceledException("The screenshot was cancelled or not allowed");
        }

        if (!response.IsSuccess || !response.Results.TryGetValue("uri", out var uriValue))
        {
            throw new CaptureException($"The screenshot portal failed with code {response.Code}");
        }

        var path = new Uri(uriValue.GetString()).LocalPath;
        try
        {
            return SKBitmap.Decode(path) ?? throw new CaptureException($"Could not read the screenshot {path}");
        }
        finally
        {
            DeleteOwnFile(path, requestedAt);
        }
    }

    /// <summary>
    /// The portal saves a file for every request (GNOME in ~/Pictures/Screenshots), it is ours and only an intermediate.
    /// Only a file written after the request is removed, never an older one.
    /// </summary>
    private static void DeleteOwnFile(string path, DateTime requestedAt)
    {
        try
        {
            if (File.Exists(path) && File.GetLastWriteTimeUtc(path) >= requestedAt.AddSeconds(-1))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not remove the portal screenshot {path}", ex);
        }
    }
}
