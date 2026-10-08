/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Forms;
using log4net;

namespace Greenshot.Editor
{
    /// <summary>
    /// Opening the first editor took seconds: ImageSharp and the Twemoji font were loaded for the emoji button and
    /// all the editor code was JIT-compiled on the UI thread. This does that work in the background, some seconds after
    /// Greenshot started, so the first editor opens (almost) as fast as the following ones.
    /// </summary>
    public static class EditorPrewarm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EditorPrewarm));
        private static int _started;

        /// <summary>
        /// The namespaces with the code which runs when an editor opens
        /// </summary>
        private static readonly string[] EditorStartupNamespaces =
        {
            "Greenshot.Editor.Forms",
            "Greenshot.Editor.Controls",
            "Greenshot.Editor.Drawing",
            "Greenshot.Editor.Drawing.Fields",
            "Greenshot.Editor.Drawing.Fields.Binding",
            "Greenshot.Editor.Configuration",
            "Greenshot.Editor.Helpers",
            "Greenshot.Base.Controls"
        };

        /// <summary>
        /// Prepare the editor in the background, only the first call does something.
        /// </summary>
        /// <param name="delay">TimeSpan to wait before starting, so Greenshot's own startup isn't slowed down</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task which completes when the editor is prepared</returns>
        public static async Task PrewarmAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
            {
                return;
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            var stopwatch = Stopwatch.StartNew();
            // Renders on the thread pool, this loads ImageSharp and the Twemoji font
            var emojiTask = EmojiRenderer.GetSharedBitmapAsync(EmojiRenderer.EmojiButtonEmoji, EmojiRenderer.EmojiButtonSize);

            // PARALLEL: JIT-compiling the editor code next to the emoji rendering, both are only prepared for the first editor
#pragma warning disable RS0030 // R10: documented parallel branch
            int preparedMethods = await Task.Run(() =>
#pragma warning restore RS0030
            {
                // The installed fonts for the font family combobox
                _ = Controls.FontFamilyComboBox.InstalledFamilies.Length;
                // Reading the embedded resources (the images of the buttons) the first time
                foreach (string name in EmbeddedResources.GetNames(typeof(ImageEditorForm)))
                {
                    try
                    {
                        EmbeddedResources.GetImage(typeof(ImageEditorForm), name)?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Only a preparation, the editor reports it if it really needs this image
                        Log.Debug($"Couldn't load the embedded image {name}", ex);
                    }
                }
                return JitPrewarm.PrepareMethods(new[] { typeof(ImageEditorForm).Assembly, typeof(GreenshotForm).Assembly }, EditorStartupNamespaces, cancellationToken);
            }, cancellationToken).ConfigureAwait(false);

            await emojiTask.ConfigureAwait(false);
            Log.DebugFormat("Prepared the editor in {0} ms, {1} methods were JIT-compiled.", stopwatch.ElapsedMilliseconds, preparedMethods);
        }
    }
}
