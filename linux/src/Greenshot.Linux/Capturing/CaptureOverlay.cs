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

using Avalonia;
using Avalonia.Platform;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Capturing.Interactive;
using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Helpers;
using SkiaSharp;

namespace Greenshot.Linux.Capturing;

/// <summary>
/// Shows the capture screen on all monitors and returns what was chosen, null when cancelled
/// </summary>
internal static class CaptureOverlay
{
    public static Task<SelectionResult?> SelectAsync(ScreenSnapshot snapshot, CaptureMode mode, IReadOnlyList<WindowInfo> windows,
        ICompositor compositor, IReadOnlyList<Screen> platformScreens, CoreSettings settings)
    {
        var bitmap = BitmapConverter.ToAvalonia(snapshot.Image);
        var selectable = windows.Select(w => new SelectableWindow(w.Title, snapshot.ToPixels(w.Bounds))).ToList();
        var startScreen = snapshot.Layout.Screens.FirstOrDefault(s => s.IsPrimary) ?? snapshot.Layout.Screens[0];
        var startPixels = snapshot.ToPixels(startScreen.Bounds);
        var model = new SelectionModel(new SKRectI(0, 0, snapshot.Image.Width, snapshot.Image.Height), selectable, mode,
            new SKPointI(startPixels.MidX, startPixels.MidY));
        model.SetZoomer(settings.ZoomerEnabled);

        var moveWithSway = compositor is SwayCompositor && snapshot.Layout.Screens.Count > 1;
        var overlayWindows = snapshot.Layout.Screens
            .Select(screen => new CaptureOverlayWindow(bitmap, snapshot.ToPixels(screen.Bounds), model, screen,
                FindPlatformBounds(screen, snapshot.Layout.Screens, platformScreens), moveWithSway))
            .ToList();

        var result = new TaskCompletionSource<SelectionResult?>();
        model.Completed += selection =>
        {
            settings.ZoomerEnabled = model.ShowZoomer;
            foreach (var window in overlayWindows)
            {
                window.Close();
            }

            bitmap.Dispose();
            result.TrySetResult(selection);
        };

        foreach (var window in overlayWindows)
        {
            window.Show();
        }

        return result.Task;
    }

    /// <summary>
    /// Where the toolkit sees the monitor: under XWayland the X11 coordinates match the compositor layout,
    /// otherwise the monitor at the same place in the order is used
    /// </summary>
    private static PixelRect FindPlatformBounds(ScreenInfo screen, IReadOnlyList<ScreenInfo> layoutScreens, IReadOnlyList<Screen> platformScreens)
    {
        var bounds = screen.Bounds;
        var exact = platformScreens.FirstOrDefault(s => s.Bounds.X == bounds.X && s.Bounds.Y == bounds.Y);
        if (exact != null)
        {
            return exact.Bounds;
        }

        var index = layoutScreens.OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y).ToList().IndexOf(screen);
        var ordered = platformScreens.OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y).ToList();
        return index >= 0 && index < ordered.Count
            ? ordered[index].Bounds
            : new PixelRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
}
