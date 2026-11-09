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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Interfaces.Ocr
{
    /// <summary>
    /// The OCR lines of a capture: the ones found while selecting (T mode, the OCR started with the capture), else OCR runs now
    /// and its lines are kept with the capture. Shared by the OCR step, the OCR destination and Smart text.
    /// </summary>
    public static class OcrLines
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OcrLines));

        /// <summary>
        /// The OCR lines of the capture, recognized now when it has none yet
        /// </summary>
        /// <param name="captureDetails">the details of the capture, which keep the lines</param>
        /// <param name="image">the image for the OCR when the capture has no lines yet; null: no OCR</param>
        /// <param name="languageTag">the OCR language (e.g. de-DE), null or empty: the OCR language setting</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>the lines, empty when there is no text or no OCR (e.g. no Windows OCR language installed)</returns>
        public static Task<List<IOcrLineFeature>> GetAsync(ICaptureDetails captureDetails, Image image, string languageTag, CancellationToken cancellationToken) =>
            GetAsync(captureDetails, image, languageTag, null, cancellationToken);

        /// <summary>
        /// The OCR lines of the capture, recognized now when it has none yet; while that takes longer than a moment a window with
        /// a progress bar and Cancel says so (when there is somebody to see it)
        /// </summary>
        /// <param name="captureDetails">the details of the capture, which keep the lines</param>
        /// <param name="image">the image for the OCR when the capture has no lines yet; null: no OCR</param>
        /// <param name="languageTag">the OCR language (e.g. de-DE), null or empty: the OCR language setting</param>
        /// <param name="userInteraction">shows the progress, null: nothing is shown</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static Task<List<IOcrLineFeature>> GetAsync(ICaptureDetails captureDetails, Image image, string languageTag, IUserInteraction userInteraction, CancellationToken cancellationToken) =>
            GetAsync(captureDetails, image == null ? null : _ => Task.FromResult<(Image, IDisposable)>((image, null)), languageTag, userInteraction, cancellationToken);

        /// <summary>
        /// The OCR lines of the exported capture, recognized on the rendered capture (as PNG, no reduced colors) when it has none yet
        /// </summary>
        /// <param name="captureDetails">the details of the capture, which keep the lines</param>
        /// <param name="source">the capture to render for the OCR; null: no OCR</param>
        /// <param name="languageTag">the OCR language (e.g. de-DE), null or empty: the OCR language setting</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="userInteraction">shows the progress while OCR takes longer than a moment, null: nothing is shown</param>
        public static Task<List<IOcrLineFeature>> GetAsync(ICaptureDetails captureDetails, IExportSource source, string languageTag, IUserInteraction userInteraction, CancellationToken cancellationToken) =>
            GetAsync(captureDetails, source == null ? null : async token =>
            {
                var lease = await source.RenderAsync(new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false) { DisableReduceColors = true }, token).ConfigureAwait(false);
                return (lease?.Image, lease);
            }, languageTag, userInteraction, cancellationToken);

        /// <param name="captureDetails">the details of the capture, which keep the lines</param>
        /// <param name="image">gives the image for the OCR and what to dispose after it (the provider reads the pixels before it returns)</param>
        /// <param name="languageTag">the OCR language</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="userInteraction">shows the progress, null: nothing is shown</param>
        private static Task<List<IOcrLineFeature>> GetAsync(ICaptureDetails captureDetails, Func<CancellationToken, Task<(Image Image, IDisposable Owner)>> image, string languageTag,
            IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            // The window only shows when it takes longer than a moment, lines the capture already has come without it
            if (userInteraction?.IsInteractive != true || Find(captureDetails).Count > 0)
            {
                return GetCoreAsync(captureDetails, image, languageTag, cancellationToken);
            }

            return userInteraction.RunWithProgressAsync(Texts.Core.OcrRunning, (_, token) => GetCoreAsync(captureDetails, image, languageTag, token), cancellationToken);
        }

        private static async Task<List<IOcrLineFeature>> GetCoreAsync(ICaptureDetails captureDetails, Func<CancellationToken, Task<(Image Image, IDisposable Owner)>> image, string languageTag,
            CancellationToken cancellationToken)
        {
            if (captureDetails?.ProcessingTask != null)
            {
                try
                {
                    await captureDetails.ProcessingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn("Error waiting for the OCR started with the capture", ex);
                }
            }

            var lines = Find(captureDetails);
            if (lines.Count > 0 || image == null)
            {
                return lines;
            }

            // OCR is optional (e.g. no Windows OCR language installed): without it there is simply no text
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null)
            {
                return lines;
            }

            try
            {
                var (source, owner) = await image(cancellationToken).ConfigureAwait(false);
                List<IOcrLineFeature> recognized;
                using (owner)
                {
                    if (source == null)
                    {
                        return lines;
                    }

                    recognized = await ocrProvider.DoOcrAsync(source, string.IsNullOrWhiteSpace(languageTag) ? null : languageTag.Trim()).ConfigureAwait(false);
                }

                if (recognized == null || recognized.Count == 0)
                {
                    return lines;
                }

                if (captureDetails != null)
                {
                    lock (captureDetails.Features)
                    {
                        captureDetails.Features.AddRange(recognized);
                    }
                }

                return recognized;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to run OCR", ex);
                return lines;
            }
        }

        /// <summary>
        /// The OCR lines the capture has now, empty when there are none
        /// </summary>
        public static List<IOcrLineFeature> Find(ICaptureDetails captureDetails)
        {
            if (captureDetails?.Features == null)
            {
                return new List<IOcrLineFeature>();
            }

            lock (captureDetails.Features)
            {
                return captureDetails.Features.OfType<IOcrLineFeature>().ToList();
            }
        }
    }
}
