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
using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Triggers;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Ai;

/// <summary>
/// The capture rules for recipes started by AI tools (AI tool triggers): windows of excluded applications are blacked out in
/// everything taken from the screen. The Source step marks its captures (<see cref="Enter"/>), the app blacks out the
/// windows in the picture of the screen (<see cref="RedactExcludedWindowsAsync"/>) before anything is cut out of it.
/// </summary>
public static class AiToolCapture
{
    private static readonly AsyncLocal<bool> Active = new();

    /// <summary>
    /// True when the flow was started by an AI tool (only greenshot-mcp can fire an AI tool trigger)
    /// </summary>
    public static bool IsAiToolRun(CaptureFlowContext? context) =>
        string.Equals(context?.Trigger?.TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True while a capture for an AI tool is taken, on this async flow
    /// </summary>
    public static bool IsActive => Active.Value;

    /// <summary>
    /// The captures until the result is disposed are for an AI tool
    /// </summary>
    public static IDisposable Enter()
    {
        bool previous = Active.Value;
        Active.Value = true;
        return new Scope(previous);
    }

    private sealed class Scope(bool previous) : IDisposable
    {
        public void Dispose() => Active.Value = previous;
    }

    /// <summary>
    /// Blacks out the windows of excluded applications in a picture of the screen.
    /// Without window information (GNOME, KDE) nothing can be blacked out: then the capture is refused while there are exclusions.
    /// </summary>
    /// <returns>The number of windows blacked out</returns>
    /// <exception cref="CaptureException">The desktop doesn't tell where the windows are</exception>
    public static async Task<int> RedactExcludedWindowsAsync(ScreenSnapshot snapshot, ICompositor? compositor)
    {
        if (AiToolAccess.ExcludedApplications.Count == 0)
        {
            return 0;
        }

        if (compositor is not { KnowsWindows: true })
        {
            throw new CaptureException($"On this desktop ({compositor?.Name ?? "unknown"}) Greenshot can't see where windows are, so it can't keep the " +
                                       "excluded applications out of screenshots for AI tools. Empty the list of excluded applications in the " +
                                       "Greenshot settings (AI tools tab) to allow it.");
        }

        int redacted = 0;
        using var canvas = new SKCanvas(snapshot.Image);
        using var black = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        foreach (var window in await compositor.GetVisibleWindowsAsync().ConfigureAwait(false))
        {
            if (!AiToolAccess.IsWindowExcluded(window))
            {
                continue;
            }

            var pixels = snapshot.ToPixels(window.Bounds);
            if (pixels.IsEmpty)
            {
                continue;
            }

            canvas.DrawRect(pixels, black);
            redacted++;
        }

        canvas.Flush();
        return redacted;
    }

    /// <summary>
    /// PNG of the image, scaled down so the longest side is at most maxSize pixels (0: original size)
    /// </summary>
    public static byte[] EncodePng(SKBitmap image, int maxSize, out int width, out int height)
    {
        SKBitmap? scaled = null;
        try
        {
            var toEncode = image;
            if (maxSize > 0 && Math.Max(image.Width, image.Height) > maxSize)
            {
                double scale = (double)maxSize / Math.Max(image.Width, image.Height);
                var info = new SKImageInfo(Math.Max(1, (int)Math.Round(image.Width * scale)), Math.Max(1, (int)Math.Round(image.Height * scale)),
                    SKColorType.Bgra8888, SKAlphaType.Premul);
                scaled = image.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
                toEncode = scaled ?? image;
            }

            width = toEncode.Width;
            height = toEncode.Height;
            using var data = toEncode.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        finally
        {
            scaled?.Dispose();
        }
    }
}
