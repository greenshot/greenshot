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
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Captures the mouse cursor icon, hotspot, and screen position.
    /// Can be used as an atomic source or composed into a CompositeCaptureSource.
    /// </summary>
    public class CursorCaptureSource : ICaptureSource
    {
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public string Name => "CursorCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload ?? new CapturePayload();
            var capture = payload.RawCapture ?? new Capture();

            bool shouldCapture = context.Properties.TryGetValue("CaptureMouseCursor", out var cmcObj) && cmcObj is bool cmc
                ? cmc
                : CoreConfig.CaptureMousepointer;
            capture = WindowCapture.CaptureCursor(capture);
            capture.CursorVisible = shouldCapture;

            payload.RawCapture = capture;
            return Task.FromResult<ICapturePayload>(payload);
        }
    }
}
