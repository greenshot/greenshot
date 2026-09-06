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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
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

            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>();

            if (ocrProvider == null)
            {
                return false;
            }

            if (capture.Image == null)
            {
                return false;
            }

            Image clonedImage;
            try
            {
                clonedImage = (Image)capture.Image.Clone();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to clone capture image for OCR background processing", ex);
                return false;
            }

            var captureDetails = capture.CaptureDetails;
            var initialCropOffset = captureDetails.CropOffset;

            var task = Task.Run(() =>
            {
                using (clonedImage)
                {
                    try
                    {
                        var ocrLines = Task.Run(async () => await ocrProvider.DoOcrAsync(clonedImage).ConfigureAwait(false)).Result;
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
            });

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
    }
}