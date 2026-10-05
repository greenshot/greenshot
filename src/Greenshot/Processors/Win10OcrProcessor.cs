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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Dapplo.Windows.Common.Structs;
using Greenshot.Configuration;

namespace Greenshot.Processors
{
    /// <summary>
    /// This processor processes a capture to see if there is text on it
    /// </summary>
    public class Win10OcrProcessor : AbstractProcessor
    {
        private static readonly IWin10Configuration Win10Configuration = IniConfigRegistry.GetSection<IWin10Configuration>();
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10OcrProcessor));

        public override string Designation => "Windows10OcrProcessor";

        public override string Description => "Windows OCR";

        public override bool isActive => Win10Configuration.AlwaysRunOCROnCapture;

        /// <summary>
        /// Runs before interactive selection so detected OCR text lines are visible
        /// as hotspots in the CaptureWindow while the user selects a region.
        /// </summary>
        public override ProcessorTiming PreferredTiming => ProcessorTiming.PreSelection;

        public override bool ProcessCapture(ICapture capture)
        {
            if (!Win10Configuration.AlwaysRunOCROnCapture)
            {
                return false;
            }

            if (capture == null || capture.CaptureDetails == null)
            {
                return false;
            }

            lock (capture.CaptureDetails.StartedProcessors)
            {
                if (capture.CaptureDetails.StartedProcessors.Contains(Designation))
                {
                    return false;
                }
                capture.CaptureDetails.StartedProcessors.Add(Designation);
            }

            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);

            if (ocrProvider == null)
            {
                return false;
            }

            if (capture.Image == null)
            {
                return false;
            }

            var captureDetails = capture.CaptureDetails;
            var initialCropOffset = captureDetails.CropOffset;

            Task<List<IOcrLineFeature>> ocrTask;
            try
            {
                // The provider copies the pixels before it returns, so the image needs no clone
                ocrTask = ocrProvider.DoOcrAsync(capture.Image);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start the OCR of the capture", ex);
                return false;
            }

            // PARALLEL: the OCR runs next to the interactive selection, its lines show up as hotspots while the user selects.
            // (Background work tracked on the capture details, replaced by AnalysisResults with imaging roadmap step 2.)
            var task = AddOcrLinesAsync(ocrTask, captureDetails, initialCropOffset);

            if (captureDetails.ProcessingTask != null)
            {
                captureDetails.ProcessingTask = Task.WhenAll(captureDetails.ProcessingTask, task);
            }
            else
            {
                captureDetails.ProcessingTask = task;
            }

            return true;
        }

        /// <summary>
        /// Add the detected lines to the capture details when the OCR is done, corrected for a crop which happened in the meantime
        /// </summary>
        private static async Task AddOcrLinesAsync(Task<List<IOcrLineFeature>> ocrTask, ICaptureDetails captureDetails, NativePoint initialCropOffset)
        {
            try
            {
                var ocrLines = await ocrTask.ConfigureAwait(false);
                if (ocrLines != null && ocrLines.Any())
                {
                    lock (captureDetails.Features)
                    {
                        var currentCropOffset = captureDetails.CropOffset;
                        var dx = currentCropOffset.X - initialCropOffset.X;
                        var dy = currentCropOffset.Y - initialCropOffset.Y;
                        if (dx != 0 || dy != 0)
                        {
                            foreach (var line in ocrLines)
                            {
                                line.Offset(-dx, -dy);
                            }
                        }
                        captureDetails.Features.AddRange(ocrLines);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error performing Windows OCR in background task", ex);
            }
            finally
            {
                if (captureDetails is CaptureDetails concreteDetails)
                {
                    concreteDetails.NotifyFeaturesChanged();
                }
            }
        }
    }
}