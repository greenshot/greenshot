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
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Captures the screen (all monitors, the monitor under the cursor, or a fixed monitor).
    /// </summary>
    public class ScreenCaptureSource : ICaptureSource
    {
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name => "ScreenCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            ScreenCaptureMode mode = context.Properties.TryGetValue("ScreenCaptureMode", out var scmObj) && scmObj is ScreenCaptureMode scm
                ? scm
                : CoreConfig.ScreenCaptureMode;
            ICapture capture = new Capture();
            bool captureTaken = false;

            switch (mode)
            {
                case ScreenCaptureMode.Auto:
                    NativePoint mouseLocation = User32Api.GetCursorLocation();
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        if (screen.Bounds.Contains(mouseLocation))
                        {
                            capture = WindowCapture.CaptureRectangle(capture, screen.Bounds);
                            captureTaken = true;
                            capture.CursorLocation = capture.CursorLocation.Offset(-screen.Bounds.Location.X, -screen.Bounds.Location.Y);
                            break;
                        }
                    }
                    break;

                case ScreenCaptureMode.Fixed:
                    int screenIndex = CoreConfig.ScreenToCapture - 1; // 1-based in config
                    if (screenIndex >= 0 && screenIndex < Screen.AllScreens.Length)
                    {
                        capture = WindowCapture.CaptureRectangle(capture, Screen.AllScreens[screenIndex].Bounds);
                        captureTaken = true;
                    }
                    break;

                case ScreenCaptureMode.FullScreen:
                    break;
            }

            if (!captureTaken)
            {
                capture = WindowCapture.CaptureScreen(capture);
            }

            capture.CaptureDetails.Title = "Screen";
            capture.CaptureDetails.AddMetaData("source", "Screen");

            var payload = new CapturePayload(capture);
            return Task.FromResult<ICapturePayload>(payload);
        }
    }
}
