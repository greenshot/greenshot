/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Captures the currently active desktop window with heuristic fallbacks (DWM/GDI/Screen).
    /// </summary>
    public class ActiveWindowCaptureSource : ICaptureSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ActiveWindowCaptureSource));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name => "ActiveWindowCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            WindowDetails window = null;
            if (context.Properties.TryGetValue("TargetWindow", out var twObj))
            {
                window = twObj as WindowDetails;
            }

            bool presupplied = window != null;
            if (!presupplied)
            {
                window = WindowDetails.GetActiveWindow();
            }

            ICapture capture = new Capture();
            bool captured = false;

            if (window != null)
            {
                if (!presupplied && window.Iconic)
                {
                    window.Restore();
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
                Log.Warn("No active window to capture or capture failed, falling back to screen capture.");
                capture = WindowCapture.CaptureScreen(capture);
                capture.CaptureDetails.AddMetaData("source", "Screen");
                capture.CaptureDetails.Title = "Screen";
            }

            var payload = new CapturePayload(capture);
            return Task.FromResult<ICapturePayload>(payload);
        }
    }
}
