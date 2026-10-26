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

using System.Text;

namespace Greenshot.Linux.Core.Output;

/// <summary>
/// The clipboard of the session. Greenshot runs through XWayland, whose clipboard only reaches Wayland applications
/// while Greenshot has the focus; wl-copy owns the Wayland clipboard directly and keeps serving it in the background.
/// Under X11 xclip does the same.
/// </summary>
public static class SystemClipboard
{
    private static bool IsWayland => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    public static bool IsAvailable => IsWayland ? ProcessRunner.IsOnPath("wl-copy") : ProcessRunner.IsOnPath("xclip");

    public static Task SetImageAsync(byte[] data, string mimeType) => SetAsync(data, mimeType);

    public static Task SetTextAsync(string text) => SetAsync(Encoding.UTF8.GetBytes(text), "text/plain;charset=utf-8");

    /// <summary>
    /// The image on the clipboard (PNG, JPEG, BMP, ...), null when there is none
    /// </summary>
    public static async Task<byte[]?> GetImageAsync()
    {
        bool canRead = IsWayland ? ProcessRunner.IsOnPath("wl-paste") : ProcessRunner.IsOnPath("xclip");
        if (!canRead)
        {
            return null;
        }

        var types = IsWayland
            ? await ProcessRunner.RunAsync("wl-paste", ["--list-types"], timeout: TimeSpan.FromSeconds(5)).ConfigureAwait(false)
            : await ProcessRunner.RunAsync("xclip", ["-selection", "clipboard", "-t", "TARGETS", "-o"], timeout: TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        if (types.ExitCode != 0)
        {
            return null;
        }

        var available = types.OutputText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var type = available.FirstOrDefault(t => t == "image/png") ?? available.FirstOrDefault(t => t.StartsWith("image/", StringComparison.Ordinal));
        if (type == null)
        {
            return null;
        }

        var result = IsWayland
            ? await ProcessRunner.RunAsync("wl-paste", ["--no-newline", "--type", type], timeout: TimeSpan.FromSeconds(10)).ConfigureAwait(false)
            : await ProcessRunner.RunAsync("xclip", ["-selection", "clipboard", "-t", type, "-o"], timeout: TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        return result.ExitCode == 0 && result.Output.Length > 0 ? result.Output : null;
    }

    private static async Task SetAsync(byte[] data, string mimeType)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(IsWayland
                ? "Copying needs wl-copy, install the package wl-clipboard"
                : "Copying needs xclip, install the package xclip");
        }

        var result = IsWayland
            ? await ProcessRunner.RunAsync("wl-copy", ["--type", mimeType], data, TimeSpan.FromSeconds(10), captureOutput: false).ConfigureAwait(false)
            : await ProcessRunner.RunAsync("xclip", ["-selection", "clipboard", "-t", mimeType, "-i"], data, TimeSpan.FromSeconds(10), captureOutput: false)
                .ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Copying to the clipboard failed with exit code {result.ExitCode}");
        }
    }
}
