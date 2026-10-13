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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Greenshot.Capturing.Views;
using log4net;

namespace Greenshot.Capturing
{
    /// <summary>
    /// The first interactive capture after Greenshot started was slower than the following ones: the capture window, its tools and
    /// the capture code were JIT-compiled on the UI thread while the user waited (about 100 ms more to create the window, and the UI
    /// thread stalled). This compiles that code in the background, some seconds after Greenshot started, like EditorPrewarm does for the editor.
    /// </summary>
    public static class CapturePrewarm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CapturePrewarm));
        private static int _started;

        /// <summary>
        /// The namespaces with the code which runs from the hotkey to the shown capture window
        /// </summary>
        internal static readonly string[] CaptureNamespaces =
        {
            "Greenshot.Capturing",
            "Greenshot.Capturing.Controls",
            "Greenshot.Capturing.Overlays",
            "Greenshot.Capturing.Tools",
            "Greenshot.Capturing.ViewModels",
            "Greenshot.Capturing.Views",
            "Greenshot.Recipes.Pipeline",
            "Greenshot.Recipes.Steps",
            "Greenshot.Base.Recipes.Pipeline",
            "Greenshot.Base.Recipes.Sources",
            "Greenshot.Base.Capturing",
            "Greenshot.Base.Interfaces.Capture",
            "Greenshot.Base.Native"
        };

        /// <summary>
        /// Prepare the interactive capture in the background, only the first call does something
        /// </summary>
        /// <param name="delay">TimeSpan to wait before starting, so Greenshot's own startup isn't slowed down</param>
        /// <param name="cancellationToken">CancellationToken</param>
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

#pragma warning disable RS0030 // R10: CPU-bound preparation on the thread pool
            int preparedMethods = await Task.Run(() =>
#pragma warning restore RS0030
            {
                // The WPF theme assembly is loaded when the first window with controls opens
                try
                {
                    Assembly.Load("PresentationFramework.Aero2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                }
                catch (Exception ex)
                {
                    Log.Debug("Couldn't load PresentationFramework.Aero2", ex);
                }
                return JitPrewarm.PrepareMethods(new[] { typeof(CaptureWindow).Assembly, typeof(ScreenCapture).Assembly }, CaptureNamespaces, cancellationToken);
            }, cancellationToken).ConfigureAwait(false);
            Log.DebugFormat("Prepared the interactive capture, {0} methods were JIT-compiled.", preparedMethods);
        }
    }
}
