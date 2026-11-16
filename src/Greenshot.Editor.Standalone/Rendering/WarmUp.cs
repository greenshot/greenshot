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
using System;
using System.IO;
using System.Threading.Tasks;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Standalone.Rendering
{
    /// <summary>
    /// Makes the drawing ready while the window is still being built: the installed fonts are listed and a small picture
    /// is read and drawn once on a thread of the pool, so the code for it is loaded and compiled by the time the first
    /// capture is shown. The settings and texts are loaded next to it. Nothing depends on it.
    /// </summary>
    public static class WarmUp
    {
        public static void Start()
        {
            // PARALLEL: runs next to the start of the window, which is the point of it
#pragma warning disable RS0030 // R10: the documented boundary to the thread pool for drawing
            Task.Run(Run).Forget("Making the drawing ready");
            // The settings and the texts: both are loaded once, whoever asks first, and the window needs them right away
            Task.Run(() => _ = EditorTexts.Text).Forget("Loading the texts");
#pragma warning restore RS0030
        }

        private static void Run()
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            using var stream = new MemoryStream();
            using (var picture = new Image<Rgba32>(16, 16, new Rgba32(255, 255, 255)))
            {
                picture.SaveAsPng(stream);
            }

            var document = DocumentFiles.Read(stream.ToArray(), "warm-up.png");
            using (DocumentRenderer.Render(document))
            {
            }

            int fonts = EditorFonts.InstalledFamilies.Count;
            _ = EditorFonts.Emoji;
            EditorLog.Debug($"Start: the drawing was made ready in {System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds:F0} ms, {fonts} font families are installed");
        }
    }
}
