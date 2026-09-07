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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Configuration;
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

            if (selection.FinalMode == CaptureMode.Text && payload.RawCapture.CaptureDetails.OcrInformation != null)
            {
                ExtractOcrText(context, selection.SelectedRegion);
            }
        }

        private static void ExtractOcrText(CaptureFlowContext context, NativeRect selectionRect)
        {
            var ocrInfo = context.Payload?.RawCapture?.CaptureDetails?.OcrInformation;
            if (ocrInfo == null) return;

            var textResult = new StringBuilder();
            var bounds = selectionRect.IsEmpty ? new NativeRect(NativePoint.Empty, context.Payload.RawCapture.Image.Size) : selectionRect;

            foreach (var line in ocrInfo.Lines)
            {
                if (line.CalculatedBounds.IsEmpty || !line.CalculatedBounds.IntersectsWith(bounds)) continue;

                for (var i = 0; i < line.Words.Length; i++)
                {
                    var word = line.Words[i];
                    if (!word.Bounds.IntersectsWith(bounds)) continue;
                    textResult.Append(word.Text);
                    if (i + 1 < line.Words.Length && word.Text.Length > 1) textResult.Append(' ');
                }
                textResult.AppendLine();
            }

            string extracted = textResult.ToString();
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
