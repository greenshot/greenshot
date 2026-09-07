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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step presenting interactive selection overlay (region, window snapping, or OCR text).
    /// </summary>
    public class InteractiveSelectionStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InteractiveSelectionStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly IInteractiveCaptureSelector _selector;

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public InteractiveSelectionStep(RecipeStepConfig config, IInteractiveCaptureSelector selector = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "InteractiveSelectionStep";
            _selector = selector ?? new InteractiveCaptureSelector();
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null)
            {
                context.Abort("No raw capture available for interactive selection.");
                return;
            }

            // Skip interaction if region was already pre-supplied
            if (context.Properties.TryGetValue("PreSuppliedRegion", out var regionObj) &&
                regionObj is NativeRect preRect && !preRect.IsEmpty)
            {
                payload.RawCapture.Crop(preRect);
                return;
            }

            // Skip interaction if capture was already acquired directly from a window (e.g. targeted window capture)
            if (payload.RawCapture.CaptureDetails?.MetaData?.TryGetValue("source", out var src) == true && src == "Window")
            {
                return;
            }

            context.State = CaptureFlowState.Selecting;

            bool allowSnapping = Config.GetParameter("AllowWindowSnapping", true);
            List<WindowDetails> snapWindows = new List<WindowDetails>();

            if (allowSnapping)
            {
                snapWindows = await Task.Run(() =>
                {
                    var list = new List<WindowDetails>();
                    foreach (var window in WindowDetails.GetVisibleWindows())
                    {
                        window.FreezeDetails();
                        int depth = CoreConfig.WindowCaptureAllChildLocations ? 20 : 3;
                        window.GetChildren(depth);
                        list.Add(window);
                    }
                    return list;
                }, cancellationToken).ConfigureAwait(false);
            }

            CaptureMode initialMode = Config.GetParameter("SelectionMode", CaptureMode.Region);

            var selection = await _selector.SelectAsync(payload.RawCapture, snapWindows, initialMode, cancellationToken).ConfigureAwait(false);

            if (selection.IsCancelled)
            {
                context.Abort("User cancelled interactive selection.");
                return;
            }

            if (selection.SelectedWindow != null)
            {
                payload.RawCapture.CaptureDetails.Title = selection.SelectedWindow.Text;
                context.Properties["SelectedWindow"] = selection.SelectedWindow;
            }

            if (selection.SelectedRegion.Width > 0 && selection.SelectedRegion.Height > 0)
            {
                payload.RawCapture.Crop(selection.SelectedRegion);

                // Offset back to screen coordinates
                NativeRect screenOffsetRect = selection.SelectedRegion.Offset(
                    payload.RawCapture.ScreenBounds.Location.X,
                    payload.RawCapture.ScreenBounds.Location.Y);
                CoreConfig.LastCapturedRegion = screenOffsetRect;
            }

            if (selection.FinalMode == CaptureMode.Text)
            {
                ExtractOcrText(context);
            }
        }

        private static void ExtractOcrText(CaptureFlowContext context)
        {
            var rawCapture = context.Payload?.RawCapture;
            var captureDetails = rawCapture?.CaptureDetails;
            if (captureDetails == null) return;

            if (captureDetails.ProcessingTask != null)
            {
                try
                {
                    captureDetails.ProcessingTask.Wait();
                }
                catch (Exception ex)
                {
                    Log.Warn("Error waiting for background OCR processing in InteractiveSelectionStep", ex);
                }
            }

            List<IOcrLineFeature> ocrLines;
            lock (captureDetails.Features)
            {
                ocrLines = captureDetails.Features.OfType<IOcrLineFeature>().ToList();
            }

            if (!ocrLines.Any())
            {
                var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>();
                if (ocrProvider != null && rawCapture.Image != null)
                {
                    try
                    {
                        var lines = Task.Run(async () => await ocrProvider.DoOcrAsync(rawCapture.Image).ConfigureAwait(false)).Result;
                        if (lines != null && lines.Any())
                        {
                            lock (captureDetails.Features)
                            {
                                captureDetails.Features.AddRange(lines);
                            }
                            ocrLines = lines;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Failed to run OCR in InteractiveSelectionStep", ex);
                    }
                }
            }

            if (ocrLines == null || !ocrLines.Any()) return;

            var bounds = rawCapture.Image != null
                ? new NativeRect(0, 0, rawCapture.Image.Width, rawCapture.Image.Height)
                : NativeRect.Empty;

            var textResult = new StringBuilder();

            foreach (var line in ocrLines)
            {
                if (!bounds.IsEmpty && (line.Bounds.IsEmpty || !line.Bounds.IntersectsWith(bounds))) continue;

                if (line.Words != null && line.Words.Count > 0)
                {
                    bool lineHasWords = false;
                    for (var i = 0; i < line.Words.Count; i++)
                    {
                        var word = line.Words[i];
                        if (!bounds.IsEmpty && !word.Bounds.IntersectsWith(bounds)) continue;
                        if (lineHasWords && word.Text.Length > 0)
                        {
                            textResult.Append(' ');
                        }
                        textResult.Append(word.Text);
                        lineHasWords = true;
                    }
                    if (lineHasWords)
                    {
                        textResult.AppendLine();
                    }
                }
                else if (!string.IsNullOrEmpty(line.Text))
                {
                    textResult.AppendLine(line.Text);
                }
            }

            string extracted = textResult.ToString().TrimEnd();
            context.Payload.ExtractedText = extracted;
            if (!string.IsNullOrEmpty(extracted))
            {
                var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>();
                if (uiContext != null)
                {
                    uiContext.Post(_ =>
                    {
                        try
                        {
                            Clipboard.SetText(extracted);
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("Failed to set clipboard text", ex);
                        }
                    }, null);
                }
                else
                {
                    try
                    {
                        Clipboard.SetText(extracted);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Failed to set clipboard text", ex);
                    }
                }
            }
        }
    }
}
