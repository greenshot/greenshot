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

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Output;

namespace Greenshot.Linux.Core.Destinations;

/// <summary>
/// Copies the capture as PNG to the clipboard
/// </summary>
public sealed class ClipboardDestination : IDestination
{
    public string Designation => WellKnownDestinations.Clipboard;

    public string DisplayName => "Copy to clipboard";

    public int Priority => 2;

    public bool IsAvailable => SystemClipboard.IsAvailable;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        try
        {
            var png = ImageFormats.Encode(capture.Image, "png");
            await SystemClipboard.SetImageAsync(png, "image/png").ConfigureAwait(false);
            return ExportResult.Done(message: "The capture was copied to the clipboard");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or OperationCanceledException)
        {
            Log.Error("Copying to the clipboard failed", ex);
            return ExportResult.Failed(ex.Message);
        }
    }
}
