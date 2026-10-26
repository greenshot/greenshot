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
/// Captures with grim, which uses the wlr-screencopy protocol of wlroots compositors (Sway, Hyprland, river, Wayfire, ...)
/// and of newer KWin and others which support ext-image-copy-capture. It is fast, captures all outputs in one picture
/// and can include the mouse pointer.
/// </summary>
public sealed class GrimScreenGrabber : IScreenGrabber
{
    public string Name => "grim";

    public bool SupportsCursor => true;

    public static bool IsAvailable => ProcessRunner.IsOnPath("grim");

    public async Task<SKBitmap> GrabAsync(ScreenLayout layout, bool includeCursor, CancellationToken cancellationToken = default)
    {
        // Compression level 0: the PNG is only passed through a pipe, speed matters more than size
        var arguments = new List<string> { "-t", "png", "-l", "0" };
        if (includeCursor)
        {
            arguments.Add("-c");
        }

        arguments.Add("-");
        var result = await ProcessRunner.RunAsync("grim", arguments, timeout: TimeSpan.FromSeconds(10), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new CaptureException($"grim failed: {result.Error.Trim()}");
        }

        return SKBitmap.Decode(result.Output) ?? throw new CaptureException("grim returned no image");
    }
}
