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

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Recipes.Sources
{
    /// <summary>
    /// Acquires an image payload from the Windows clipboard.
    /// </summary>
    public class ClipboardCaptureSource : ICaptureSource
    {
        public string Name => "ClipboardCaptureSource";

        public async Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            // The clipboard is read on the UI thread by the clipboard service
            Image clipboardImage = await ClipboardService.For(context.Ui).GetImageAsync(cancellationToken).ConfigureAwait(false);
            if (clipboardImage == null)
            {
                context.Abort("Clipboard does not contain a valid image.");
                return null;
            }

            ICapture capture = new Capture(clipboardImage);
            capture.CaptureDetails.Title = "Clipboard";
            capture.CaptureDetails.AddMetaData("source", "Clipboard");

            return new CapturePayload(capture);
        }
    }
}
