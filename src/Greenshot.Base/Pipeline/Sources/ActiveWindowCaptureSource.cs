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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Captures the currently active desktop window or a targeted window matching title/pattern/process criteria,
    /// with heuristic fallbacks (DWM/GDI/Screen).
    /// </summary>
    public class ActiveWindowCaptureSource : ICaptureSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ActiveWindowCaptureSource));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly RecipeStepConfig _config;

        public string Name => "ActiveWindowCaptureSource";

        public ActiveWindowCaptureSource(RecipeStepConfig config = null)
        {
            _config = config;
        }

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            WindowDetails window = null;
            if (context.Properties.TryGetValue("TargetWindow", out var twObj))
            {
                window = twObj as WindowDetails;
            }

            // Check config and context properties for targeted window specifications
            string title = _config?.GetParameter<string>("WindowTitle")
                ?? _config?.GetParameter<string>("windowTitle")
                ?? (context.Properties.TryGetValue("WindowTitle", out var tObj) ? tObj as string : null);

            string titlePattern = _config?.GetParameter<string>("WindowTitlePattern")
                ?? _config?.GetParameter<string>("windowTitlePattern")
                ?? (context.Properties.TryGetValue("WindowTitlePattern", out var tpObj) ? tpObj as string : null);

            string processName = _config?.GetParameter<string>("ProcessName")
                ?? _config?.GetParameter<string>("processName")
                ?? (context.Properties.TryGetValue("ProcessName", out var pnObj) ? pnObj as string : null);

            bool matchCase = _config?.GetParameter("MatchCase", false)
                ?? _config?.GetParameter("matchCase", false)
                ?? (context.Properties.TryGetValue("MatchCase", out var mcObj) && mcObj is bool mc && mc);

            bool isTargeted = !string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(titlePattern) || !string.IsNullOrEmpty(processName);

            if (window == null && isTargeted)
            {
                window = FindMatchingWindow(title, titlePattern, processName, matchCase);
                if (window != null)
                {
                    Log.InfoFormat("Found targeted window '{0}' (Handle: {1})", window.Text, window.Handle);
                }
                else
                {
                    Log.WarnFormat("Could not find window matching title='{0}', pattern='{1}', process='{2}'", title, titlePattern, processName);
                }
            }

            bool presupplied = window != null;
            if (!presupplied)
            {
                window = WindowDetails.GetActiveWindow();
            }

            ICapture capture = new Greenshot.Base.Core.Capture();
            bool captured = false;

            if (window != null)
            {
                if (window.Iconic)
                {
                    window.Restore();
                    Thread.Sleep(100);
                }

                if (isTargeted)
                {
                    window.ToForeground();
                    Thread.Sleep(100);
                }

                window = WindowCaptureHelper.SelectCaptureWindow(window);
                if (window != null)
                {
                    CoreConfig.LastCapturedRegion = window.WindowRectangle;
                    var windowCaptureMode = context.Properties.TryGetValue("WindowCaptureMode", out var wcmObj) && wcmObj is WindowCaptureMode wcm
                        ? wcm
                        : CoreConfig.WindowCaptureMode;
                    capture = WindowCaptureHelper.CaptureWindow(window, capture, windowCaptureMode);
                    if (capture != null)
                    {
                        capture.MoveMouseLocation(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);
                        capture.CaptureDetails.AddMetaData("source", "Window");
                        captured = true;
                    }
                }
            }

            if (!captured)
            {
                Log.Warn("No active or targeted window to capture or capture failed, falling back to screen capture.");
                capture = WindowCapture.CaptureScreen(capture);
                capture.CaptureDetails.AddMetaData("source", "Screen");
                capture.CaptureDetails.Title = "Screen";
            }

            var payload = new CapturePayload(capture);
            return Task.FromResult<ICapturePayload>(payload);
        }

        private static WindowDetails FindMatchingWindow(string title, string titlePattern, string processName, bool matchCase)
        {
            Regex regex = null;
            if (!string.IsNullOrEmpty(titlePattern))
            {
                var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                try
                {
                    regex = new Regex(titlePattern, options);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Invalid windowTitlePattern regex '{titlePattern}'", ex);
                }
            }

            StringComparison comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // 1. Check if the currently active foreground window matches criteria
            var activeWin = WindowDetails.GetActiveWindow();
            if (activeWin != null && Matches(activeWin, title, regex, processName, comparison))
            {
                return activeWin;
            }

            // 2. Search top-level application windows in desktop Z-order
            foreach (var win in WindowDetails.GetTopLevelWindows())
            {
                if (win == null || win.Handle == IntPtr.Zero || win.HasParent) continue;

                if (Matches(win, title, regex, processName, comparison))
                {
                    return win;
                }
            }

            return null;
        }

        private static bool Matches(WindowDetails win, string title, Regex regex, string processName, StringComparison comparison)
        {
            if (win == null || win.Handle == IntPtr.Zero) return false;

            // Process name check
            if (!string.IsNullOrEmpty(processName))
            {
                string proc = null;
                try
                {
                    if (!string.IsNullOrEmpty(win.ProcessPath))
                    {
                        proc = Path.GetFileNameWithoutExtension(win.ProcessPath);
                    }
                }
                catch { }

                string expectedProc = Path.GetFileNameWithoutExtension(processName);
                if (proc == null || (!proc.Equals(expectedProc, comparison) && !proc.Equals(processName, comparison)))
                {
                    return false;
                }
            }

            // Title regex check
            if (regex != null)
            {
                if (string.IsNullOrEmpty(win.Text) || !regex.IsMatch(win.Text))
                {
                    return false;
                }
            }
            // Title exact or substring check
            else if (!string.IsNullOrEmpty(title))
            {
                if (string.IsNullOrEmpty(win.Text) || win.Text.IndexOf(title, comparison) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
