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
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Capturing
{
    /// <summary>
    /// What the processors found on the image of a capture (text lines, codes) belongs to that image, the windows belong to the screen.
    /// When the selection switches to the live screen (L), the image is gone and so is what was found on it.
    /// </summary>
    internal static class ImageFeatures
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageFeatures));

        /// <summary>
        /// Remove what was found on the image, only the windows stay
        /// </summary>
        /// <returns>true when something was removed</returns>
        public static bool Remove(ICaptureDetails captureDetails)
        {
            lock (captureDetails.Features)
            {
                return captureDetails.Features.RemoveAll(feature => feature is not WindowFeature) > 0;
            }
        }

        /// <summary>
        /// Wait for the processors which still look at the old image, then remove what they found
        /// </summary>
        /// <returns>true when something was removed</returns>
        public static async Task<bool> RemoveWhenProcessedAsync(ICaptureDetails captureDetails)
        {
            var processingTask = captureDetails.ProcessingTask;
            if (processingTask != null)
            {
                try
                {
                    await processingTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // The processors log their own errors, the features are removed anyway
                    Log.Debug("A processor of the old image failed", ex);
                }
            }
            return Remove(captureDetails);
        }
    }
}
