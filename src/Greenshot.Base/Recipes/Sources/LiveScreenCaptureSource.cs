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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Recipes.Sources
{
    /// <summary>
    /// The screen without its pixels, for a selection on the live screen: the capture has the bounds of the screen but no image,
    /// the interactive selection captures only what was selected, when it was selected.
    /// </summary>
    public class LiveScreenCaptureSource : ICaptureSource
    {
        // The selection window is left out of every capture of the screen with WDA_EXCLUDEFROMCAPTURE, Windows 10 2004 and newer
        private const int ExcludeFromCaptureBuild = 19041;

        /// <summary>
        /// True when Windows can leave the selection window out of the capture of the screen
        /// </summary>
        public static bool IsSupported
        {
            get
            {
                var version = Environment.OSVersion.Version;
                return version.Major > 10 || (version.Major == 10 && version.Build >= ExcludeFromCaptureBuild);
            }
        }

        /// <summary>
        /// Metadata of a capture which the selection must not take again, e.g. one where windows were blacked out for an AI tool:
        /// the selection can't switch to the live screen
        /// </summary>
        public const string FrozenOnlyKey = "frozen_only";

        public string Name => "LiveScreenCaptureSource";

        public Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var capture = new Capture();
            capture.Location = capture.ScreenBounds.Location;
            capture.CaptureDetails.Title = "Screen";
            capture.CaptureDetails.AddMetaData("source", "Screen");
            return Task.FromResult<ICapturePayload>(new CapturePayload(capture));
        }
    }
}
