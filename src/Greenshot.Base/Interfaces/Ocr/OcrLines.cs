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
using Dapplo.Windows.Common.Structs;
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
    /// and its lines are kept with the capture. Shared by the OCR step, the OCR destination and plugins.
    /// </summary>
    public static class OcrLines
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OcrLines));

        /// <summary>
        /// In <see cref="ICaptureDetails.StartedProcessors"/> when the OCR of the capture was started; the OCR adds its lines once
        /// </summary>
        public const string OcrProcessor = "OCR";

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
            // Text read from the application (UI Automation) doesn't cover all of the capture: OCR adds what is outside it
            if (image == null || (captureDetails != null && !TryStartOcr(captureDetails)) || (captureDetails == null && lines.Count > 0))
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

                if (captureDetails == null)
                {
                    return recognized;
                }

                AddRecognized(captureDetails, recognized, captureDetails.CropArea);
                return Find(captureDetails);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelled: the next one asking runs the OCR
                if (captureDetails != null)
                {
                    lock (captureDetails.StartedProcessors)
                    {
                        captureDetails.StartedProcessors.Remove(OcrProcessor);
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to run OCR", ex);
                return lines;
            }
        }

        /// <summary>
        /// Note that the OCR of the capture starts, false when it ran already: its lines are there (also those of a capture which was
        /// loaded or copied, which don't carry the note)
        /// </summary>
        public static bool TryStartOcr(ICaptureDetails captureDetails)
        {
            if (Find(captureDetails).Any(line => line.Source == TextSource.Ocr))
            {
                return false;
            }
            lock (captureDetails.StartedProcessors)
            {
                return captureDetails.StartedProcessors.Add(OcrProcessor);
            }
        }

        /// <summary>
        /// Add the lines the OCR found, except where the capture has text read from the application: that text is exact, the OCR only
        /// adds the rest (text in images, a remote desktop, an application without UI Automation text)
        /// </summary>
        /// <param name="captureDetails">the details of the capture, which keep the lines</param>
        /// <param name="recognized">the lines of the OCR</param>
        /// <param name="cropAreaWhenFound">the crop area of the capture when the OCR read it, see <see cref="ICaptureDetails.AddFeatures"/></param>
        public static void AddRecognized(ICaptureDetails captureDetails, IEnumerable<IOcrLineFeature> recognized, NativeRect cropAreaWhenFound)
        {
            var applicationText = Find(captureDetails).Where(line => line.Source == TextSource.UiAutomation).Select(line => line.Bounds).ToList();
            // The lines are moved to the crop area of now when they are added
            int dx = captureDetails.CropArea.X - cropAreaWhenFound.X, dy = captureDetails.CropArea.Y - cropAreaWhenFound.Y;
            var outside = recognized
                .Where(line => !applicationText.Any(text => line.Box.Offset(-dx, -dy).IntersectsWith(text)))
                .Cast<IDetectedFeature>()
                .ToList();
            if (applicationText.Count > 0)
            {
                Log.Info($"OCR found {outside.Count} lines outside the {applicationText.Count} lines of application text");
            }
            captureDetails.AddFeatures(outside, cropAreaWhenFound);
        }

        /// <summary>
        /// Add the text read from an application; OCR lines which are already there under it give way (the application was slower than the OCR)
        /// </summary>
        /// <param name="captureDetails">the details of the capture, which keep the lines</param>
        /// <param name="lines">the lines of the application</param>
        /// <param name="cropAreaWhenFound">the crop area of the capture when the screen was captured, see <see cref="ICaptureDetails.AddFeatures"/></param>
        public static void AddApplicationText(ICaptureDetails captureDetails, IReadOnlyList<IOcrLineFeature> lines, NativeRect cropAreaWhenFound)
        {
            lock (captureDetails.Features)
            {
                // The lines are moved to the crop area of now, those outside it aren't added
                captureDetails.AddFeatures(lines, cropAreaWhenFound);
                var added = lines.Where(line => captureDetails.Features.Contains(line)).Select(line => line.Bounds).ToList();
                int replaced = captureDetails.Features.RemoveAll(feature =>
                    feature is IOcrLineFeature { Source: TextSource.Ocr } ocrLine && added.Any(text => ocrLine.Box.IntersectsWith(text)));
                if (replaced > 0)
                {
                    Log.Info($"Application text replaced {replaced} OCR lines");
                }
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
