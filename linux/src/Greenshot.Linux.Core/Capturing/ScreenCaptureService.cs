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

using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.DBus;
using Greenshot.Linux.Core.Naming;
using SkiaSharp;

namespace Greenshot.Linux.Core.Capturing;

/// <summary>
/// Takes the picture of the screens and cuts captures out of it. The interactive part (selecting a region or a window)
/// is done by the UI on the <see cref="ScreenSnapshot"/>.
/// </summary>
public sealed class ScreenCaptureService(CoreSettings settings, ICompositor compositor, IScreenGrabber grabber, Func<ScreenLayout> uiLayout)
{
    public ICompositor Compositor => compositor;

    public IScreenGrabber Grabber => grabber;

    /// <summary>
    /// Pick the grabber for CaptureBackend: Grim, Portal or Auto (grim when it works, else the portal)
    /// </summary>
    public static async Task<IScreenGrabber> CreateGrabberAsync(CoreSettings settings, DesktopPortal? portal)
    {
        var configured = settings.CaptureBackend.Trim().ToLowerInvariant();
        IScreenGrabber? portalGrabber = portal != null && await PortalScreenGrabber.IsAvailableAsync(portal).ConfigureAwait(false)
            ? new PortalScreenGrabber(portal)
            : null;
        IScreenGrabber? grimGrabber = GrimScreenGrabber.IsAvailable ? new GrimScreenGrabber() : null;

        IScreenGrabber? chosen = configured switch
        {
            "grim" => grimGrabber,
            "portal" => portalGrabber,
            _ => grimGrabber != null && portalGrabber != null
                ? new FallbackScreenGrabber(grimGrabber, portalGrabber)
                : grimGrabber ?? portalGrabber,
        };

        return chosen ?? throw new CaptureException(
            "Greenshot can't capture the screen: install grim (wlroots compositors) or xdg-desktop-portal with a backend for your desktop");
    }

    public async Task<ScreenLayout> GetLayoutAsync() => await compositor.GetLayoutAsync().ConfigureAwait(false) ?? uiLayout();

    /// <summary>
    /// Wait the CaptureDelay, so menus and the hotkey overlay of the desktop are gone, and take the picture of all screens
    /// </summary>
    public async Task<ScreenSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        var layout = await GetLayoutAsync().ConfigureAwait(false);
        if (settings.CaptureDelay > 0)
        {
            await Task.Delay(settings.CaptureDelay, cancellationToken).ConfigureAwait(false);
        }

        var takenAt = DateTime.Now;
        var image = await grabber.GrabAsync(layout, settings.CaptureMousepointer && grabber.SupportsCursor, cancellationToken).ConfigureAwait(false);
        Log.Debug($"{grabber.Name} captured {image.Width}x{image.Height} for the layout {layout.Bounds}");
        return new ScreenSnapshot(image, layout, takenAt);
    }

    /// <summary>
    /// Cut the image pixels out of the snapshot. The title for ${title} is the one of the window the area is in, as far as known.
    /// The area is remembered for "capture last region", except for full screen captures like on Windows.
    /// </summary>
    public async Task<Capture> CreateCaptureAsync(ScreenSnapshot snapshot, SKRectI pixels, CaptureMode mode, string? title = null)
    {
        pixels.Intersect(new SKRectI(0, 0, snapshot.Image.Width, snapshot.Image.Height));
        if (pixels.IsEmpty)
        {
            throw new CaptureException("The area to capture is not on any screen");
        }

        var area = snapshot.ToLayout(pixels);
        title ??= await FindTitleAsync(area).ConfigureAwait(false);
        var details = new CaptureDetails { DateTime = snapshot.TakenAt, Title = title, Mode = mode };
        if (mode != CaptureMode.FullScreen)
        {
            settings.LastCapturedRegion = area;
        }

        return new Capture(snapshot.Crop(pixels), details);
    }

    /// <summary>
    /// The whole snapshot as capture, the image is copied so the snapshot can be disposed
    /// </summary>
    public async Task<Capture> CreateFullScreenCaptureAsync(ScreenSnapshot snapshot) =>
        await CreateCaptureAsync(snapshot, new SKRectI(0, 0, snapshot.Image.Width, snapshot.Image.Height), CaptureMode.FullScreen)
            .ConfigureAwait(false);

    private async Task<string> FindTitleAsync(LayoutRect area)
    {
        if (!compositor.KnowsWindows)
        {
            return FilenamePattern.DefaultTitle;
        }

        var centerX = area.X + area.Width / 2.0;
        var centerY = area.Y + area.Height / 2.0;
        var windows = await compositor.GetVisibleWindowsAsync().ConfigureAwait(false);
        var window = windows.FirstOrDefault(w => w.Bounds.Contains(centerX, centerY));
        return string.IsNullOrWhiteSpace(window?.Title) ? FilenamePattern.DefaultTitle : window.Title;
    }
}

/// <summary>
/// Uses the second grabber when the first one fails, e.g. grim on a compositor without wlr-screencopy
/// </summary>
public sealed class FallbackScreenGrabber(IScreenGrabber primary, IScreenGrabber fallback) : IScreenGrabber
{
    private bool _primaryFailed;

    public string Name => _primaryFailed ? fallback.Name : primary.Name;

    public bool SupportsCursor => _primaryFailed ? fallback.SupportsCursor : primary.SupportsCursor;

    public async Task<SKBitmap> GrabAsync(ScreenLayout layout, bool includeCursor, CancellationToken cancellationToken = default)
    {
        if (!_primaryFailed)
        {
            try
            {
                return await primary.GrabAsync(layout, includeCursor, cancellationToken).ConfigureAwait(false);
            }
            catch (CaptureException ex)
            {
                Log.Warn($"{primary.Name} can't capture here, using {fallback.Name} from now on: {ex.Message}");
                _primaryFailed = true;
            }
        }

        return await fallback.GrabAsync(layout, includeCursor, cancellationToken).ConfigureAwait(false);
    }
}
