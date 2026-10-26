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
/// The capture modes of Greenshot.Base CaptureMode which exist on Linux
/// </summary>
public enum CaptureMode
{
    Region,
    FullScreen,
    ActiveWindow,
    Window,
    LastRegion,
}

/// <summary>
/// What is known about a capture, used for the filename pattern
/// </summary>
public sealed class CaptureDetails
{
    public DateTime DateTime { get; init; } = DateTime.Now;

    public string? Title { get; init; }

    public CaptureMode Mode { get; init; }

    public Dictionary<string, string> MetaData { get; } = new();
}

/// <summary>
/// A finished capture: the image and its details. Disposing the capture disposes the image.
/// </summary>
public sealed class Capture(SKBitmap image, CaptureDetails details) : IDisposable
{
    public SKBitmap Image { get; } = image;

    public CaptureDetails Details { get; } = details;

    public void Dispose() => Image.Dispose();
}
