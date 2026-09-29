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
using Greenshot.Base.Pipeline.Contracts;
using Contracts = Greenshot.Base.Pipeline.Contracts;

using Greenshot.Base.Recipes;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step presenting interactive selection overlay (region, window snapping, or OCR text).
    /// </summary>
    [StepInfo(WellKnownStepTypes.InteractiveSelection, "Interactive Selection", "Lets the user select a region, a window or text on a capture of the screen. A capture that was handed to the flow, or that is not a screen capture, is used as a whole.", "Interaction")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Optional, VisualMutation = PayloadEffect.MutatesPixels)]
    [StepParameter("SelectionMode", ContractDataType.Enum, DefaultValue = "Region", Description = "Initial selection mode; Text also extracts the text of the selection", AllowedValues = new[] { "Region", "Window", "Text" })]
    [StepParameter("AllowWindowSnapping", ContractDataType.Boolean, DefaultValue = true, Description = "Snap the selection to windows")]
    [StepInputVariable("PreSuppliedRegion", ContractDataType.Object, Description = "When set, the selection is skipped")]
    [StepOutputVariable("SelectedWindow", ContractDataType.Object, "The window that was selected", Conditional = true)]
    public class InteractiveSelectionStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InteractiveSelectionStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly IInteractiveCaptureSelector _selector;

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public InteractiveSelectionStep(RecipeNodeConfig config, IInteractiveCaptureSelector selector = null)
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
                return;
            }

            // The overlay shows the capture at its screen position, so selecting only makes sense on a capture of the screen.
            // A forwarded or imported image, or one from a window, file or the clipboard, is used as a whole.
            if (context.IsPayloadPreSupplied || !IsScreenCapture(payload.RawCapture))
            {
                if (Config.GetParameter("SelectionMode", CaptureMode.Region) == CaptureMode.Text)
                {
                    await ExtractOcrTextAsync(context, cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            context.State = CaptureFlowState.Selecting;

            bool allowSnapping = Config.GetParameter("AllowWindowSnapping", true);
            List<WindowDetails> snapWindows = new List<WindowDetails>();

            if (allowSnapping)
            {
                // Win32 enumeration, runs inline on the pool thread the step is already on
                int depth = CoreConfig.WindowCaptureAllChildLocations ? 20 : 3;
                foreach (var window in WindowDetails.GetVisibleWindows())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    window.FreezeDetails();
                    window.GetChildren(depth);
                    snapWindows.Add(window);
                }
            }

            CaptureMode initialMode = Config.GetParameter("SelectionMode", CaptureMode.Region);

            var selection = await _selector.SelectAsync(payload.RawCapture, snapWindows, initialMode, cancellationToken).ConfigureAwait(false);

            if (selection == null)
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
                // Configuration is written on the UI thread (single writer, its change events have UI subscribers)
                context.Ui.InvokeAsync(() => CoreConfig.LastCapturedRegion = screenOffsetRect).FireAndLog("Store the last captured region", Log);
            }

            if (selection.FinalMode == CaptureMode.Text)
            {
                await ExtractOcrTextAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Screen sources mark their captures with source "Screen"; captures without that metadata are treated as screen captures.
        /// </summary>
        private static bool IsScreenCapture(ICapture capture)
        {
            if (capture.CaptureDetails?.MetaData == null || !capture.CaptureDetails.MetaData.TryGetValue("source", out var source))
            {
                return true;
            }
            return string.Equals(source, "Screen", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Puts the OCR text of the (selected part of the) capture into Payload.ExtractedText.
        /// Where the text goes (clipboard, stdout, ...) is up to the following steps.
        /// </summary>
        private static async Task ExtractOcrTextAsync(CaptureFlowContext context, CancellationToken cancellationToken)
        {
            var rawCapture = context.Payload?.RawCapture;
            var captureDetails = rawCapture?.CaptureDetails;
            if (captureDetails == null) return;

            if (captureDetails.ProcessingTask != null)
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
                // OCR is optional (e.g. no Windows OCR language installed): without it there is simply no text
                var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
                if (ocrProvider != null && rawCapture.Image != null)
                {
                    try
                    {
                        var lines = await ocrProvider.DoOcrAsync(rawCapture.Image).ConfigureAwait(false);
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
        }
    }
}
