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

using SkiaSharp;

namespace Greenshot.Linux.Core.Capturing;

/// <summary>
/// Takes a picture of all screens. On Wayland an application can't read the screen itself, a grabber asks
/// the compositor: directly with wlr-screencopy (grim) or through the xdg-desktop-portal.
/// </summary>
public interface IScreenGrabber
{
    string Name { get; }

    /// <summary>
    /// Can the grabber capture the mouse pointer?
    /// </summary>
    bool SupportsCursor { get; }

    /// <summary>
    /// A picture of the bounding box of all screens of the layout
    /// </summary>
    Task<SKBitmap> GrabAsync(ScreenLayout layout, bool includeCursor, CancellationToken cancellationToken = default);
}

public sealed class CaptureException(string message, Exception? inner = null) : Exception(message, inner);
