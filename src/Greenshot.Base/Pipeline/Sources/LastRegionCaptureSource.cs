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

using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Re-captures the last captured rectangle saved in the configuration.
    /// </summary>
    public class LastRegionCaptureSource : ICaptureSource
    {
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name => "LastRegionCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var lastRegion = CoreConfig.LastCapturedRegion;
            if (lastRegion.IsEmpty)
            {
                context.Abort("No last captured region available.");
                return Task.FromResult<ICapturePayload>(null);
            }

            ICapture capture = new Capture();
            capture = WindowCapture.CaptureRectangle(capture, lastRegion);

            // Attempt to resolve window title from visible window at center of last region
            NativePoint centerPoint = new NativePoint(lastRegion.X + lastRegion.Width / 2, lastRegion.Y + lastRegion.Height / 2);
            foreach (WindowDetails window in WindowDetails.GetVisibleWindows())
            {
                if (window.Contains(centerPoint))
                {
                    capture.CaptureDetails.Title = window.Text;
                    context.Properties["SelectedWindow"] = window;
                    break;
                }
            }

            capture.MoveMouseLocation(capture.ScreenBounds.Location.X - capture.Location.X, capture.ScreenBounds.Location.Y - capture.Location.Y);
            capture.CaptureDetails.AddMetaData("source", "screen");

            var payload = new CapturePayload(capture);
            return Task.FromResult<ICapturePayload>(payload);
        }
    }
}
