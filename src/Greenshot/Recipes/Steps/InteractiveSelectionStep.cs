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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Capturing;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step presenting interactive selection overlay (region, window snapping, or OCR text).
    /// </summary>
    [StepInfo(WellKnownStepTypes.InteractiveSelection, "Interactive Selection", "Lets the user select a region, a window or text on a capture of the screen, or a region or window on the live screen when freezing is turned off in the settings. A capture that was handed to the flow, or that is not a screen capture, is used as a whole.", "Interaction")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Optional, VisualMutation = PayloadEffect.MutatesPixels)]
    [StepParameter("SelectionMode", ContractDataType.Enum, DefaultValue = "Region", Description = "Initial selection mode; Text also extracts the text of the selection", AllowedValues = new[] { "Region", "Window", "Text" })]
    [StepParameter("SelectionTool", ContractDataType.String, Description = "Id of the capture tool to start with, e.g. one of a plugin; when it is not there, the tool of the SelectionMode")]
    [StepParameter("AllowWindowSnapping", ContractDataType.Boolean, DefaultValue = true, Description = "Snap the selection to windows")]
    [StepInputVariable("PreSuppliedRegion", ContractDataType.Object, Description = "When set, the selection is skipped")]
    [StepOutputVariable("SelectedWindow", ContractDataType.Object, "The window that was selected", Conditional = true)]
    public class InteractiveSelectionStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(InteractiveSelectionStep));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

        private readonly IInteractiveCaptureSelector _selector;
        private readonly Func<NativeRect, CancellationToken, Task<ScreenCaptureResult>> _captureArea;

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public InteractiveSelectionStep(RecipeNodeConfig config, IInteractiveCaptureSelector selector = null) : this(config, selector, null)
        {
        }

        /// <param name="config">RecipeNodeConfig</param>
        /// <param name="selector">IInteractiveCaptureSelector, default the capture window</param>
        /// <param name="captureArea">Captures an area of the screen for a selection on the live screen, default ScreenCapture</param>
        internal InteractiveSelectionStep(RecipeNodeConfig config, IInteractiveCaptureSelector selector, Func<NativeRect, CancellationToken, Task<ScreenCaptureResult>> captureArea)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "InteractiveSelectionStep";
            _selector = selector ?? new InteractiveCaptureSelector();
            _captureArea = captureArea ?? ScreenCapture.CaptureRectangleAsync;
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
                    await OcrStep.ExtractTextAsync(context, cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            context.State = CaptureFlowState.Selecting;

            bool allowSnapping = Config.GetParameter("AllowWindowSnapping", true);
            List<IInteropWindow> snapWindows = new List<IInteropWindow>();

            // Started by the acquire step next to the capture, see GetSnapWindowsAsync
            Task<List<IInteropWindow>> snapWindowsTask = null;
            bool started = _selector is ICaptureWindowPreparer preparer && preparer.TryTakeSnapWindows(out snapWindowsTask);
            if (allowSnapping)
            {
                if (started)
                {
                    try
                    {
                        snapWindows = await snapWindowsTask.ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log.Warn("Getting the windows to snap to next to the capture failed, getting them now", ex);
                        started = false;
                    }
                }
                if (!started)
                {
                    // Win32 enumeration, runs inline on the pool thread the step is already on
                    snapWindows = EnumerateSnapWindows(cancellationToken);
                }
            }

            CaptureMode initialMode = Config.GetParameter("SelectionMode", CaptureMode.Region);

            string initialTool = Config.GetParameter<string>("SelectionTool");

            var selection = await _selector.SelectAsync(payload.RawCapture, snapWindows, initialMode, initialTool, cancellationToken).ConfigureAwait(false);

            if (selection == null)
            {
                context.Abort("User cancelled interactive selection.");
                return;
            }

            if (selection.SelectedWindow != null)
            {
                payload.RawCapture.CaptureDetails.Title = selection.SelectedWindow.GetCaption();
                context.Properties["SelectedWindow"] = selection.SelectedWindow;
            }
            // The selection gives the capture the title of the window
            TitleFix.Apply(payload.RawCapture.CaptureDetails);

            // Selected on the live screen: nothing was captured yet
            bool live = payload.RawCapture.Image == null;
            if (selection.SelectedRegion.Width > 0 && selection.SelectedRegion.Height > 0)
            {
                // Offset back to screen coordinates
                NativeRect screenOffsetRect = selection.SelectedRegion.Offset(
                    payload.RawCapture.ScreenBounds.Location.X,
                    payload.RawCapture.ScreenBounds.Location.Y);

                if (!live)
                {
                    payload.RawCapture.Crop(selection.SelectedRegion);
                }
                else if (selection.Tool is ISelectionCaptureTool)
                {
                    // The tool takes the image, the capture is placed like a cropped one
                    payload.RawCapture.Location = selection.SelectedRegion.Location;
                    payload.RawCapture.MoveMouseLocation(-selection.SelectedRegion.X, -selection.SelectedRegion.Y);
                }
                else if (!await CaptureLiveAsync(context, selection.SelectedRegion, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                // Configuration is written on the UI thread (single writer, its change events have UI subscribers)
                context.Ui.InvokeAsync(() => CoreConfig.LastCapturedRegion = screenOffsetRect, CancellationToken.None).FireAndLog("Store the last captured region", Log);

                // A tool which takes its own image of the selection, e.g. of a plugin
                if (selection.Tool is ISelectionCaptureTool selectionCaptureTool)
                {
                    context.State = CaptureFlowState.Acquiring;
                    var image = await selectionCaptureTool.CaptureSelectionAsync(selection, screenOffsetRect, context.Ui, cancellationToken).ConfigureAwait(false);
                    if (image == null)
                    {
                        context.Abort($"User cancelled the capture of tool {selectionCaptureTool.Id}.");
                        return;
                    }
                    // The cursor was somewhere on the screen, not in the new image
                    payload.RawCapture.CursorVisible = false;
                    payload.RawCapture.Image = image;
                }
            }
            else if (live)
            {
                // Nothing selected (e.g. a window outside of the screen): the whole screen, as from a frozen capture
                if (!await CaptureLiveAsync(context, NativeRect.Empty, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }

            if (selection.FinalMode == CaptureMode.Text)
            {
                await OcrStep.ExtractTextAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Capture the selection of the live screen, the capture looks like a cropped capture of the whole screen
        /// </summary>
        /// <param name="context">CaptureFlowContext</param>
        /// <param name="area">The selection in coordinates of the capture, empty for the whole screen</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>false when nothing could be captured, the flow is aborted</returns>
        private async Task<bool> CaptureLiveAsync(CaptureFlowContext context, NativeRect area, CancellationToken cancellationToken)
        {
            context.State = CaptureFlowState.Acquiring;
            var capture = context.Payload.RawCapture;
            var screenBounds = capture.ScreenBounds;
            var screenArea = area.IsEmpty ? screenBounds : area.Offset(screenBounds.X, screenBounds.Y);
            var result = await _captureArea(screenArea, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                context.Abort($"Capturing the selection {screenArea} failed.");
                return false;
            }
            if (capture.CaptureDetails.DpiX > 0 && capture.CaptureDetails.DpiY > 0 && result.Image is System.Drawing.Bitmap bitmap)
            {
                // Set by the acquire step for the screen
                bitmap.SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
            }
            capture.Image = result.Image;
            if (area.IsEmpty)
            {
                capture.Location = result.Location;
            }
            else
            {
                // Like Crop
                capture.Location = area.Location;
                capture.MoveMouseLocation(-area.X, -area.Y);
            }
            capture.CaptureDetails.AddMetaData(ScreenCapture.CaptureMethodKey, result.Backend.Name);
            // Switched from the frozen screen (L): what the processors found on that image doesn't belong to this one
            await ImageFeatures.RemoveWhenProcessedAsync(capture.CaptureDetails).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Get the windows to snap to on the thread pool, while the screen is captured: the acquire step hands the task to the selector,
        /// the selection takes it from there. They are the windows of the moment of the capture as before.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        internal static Task<List<IInteropWindow>> GetSnapWindowsAsync(CancellationToken cancellationToken)
        {
#pragma warning disable RS0030 // R10: the Win32 window enumeration runs next to the capture
            var task = Task.Run(() => EnumerateSnapWindows(cancellationToken), cancellationToken);
#pragma warning restore RS0030
            // Observed, also when no selection takes it
            _ = task.ContinueWith(t => Log.Debug("Getting the windows to snap to failed", t.Exception), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            return task;
        }

        private static List<IInteropWindow> EnumerateSnapWindows(CancellationToken cancellationToken)
        {
            var snapWindows = new List<IInteropWindow>();
            foreach (var window in WindowHelper.GetVisibleWindows())
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Dapplo.Windows caches the values, so the windows describe the screen of the capture, also the child windows
                window.GetChildren(allLevels: true);
                snapWindows.Add(window);
            }
            return snapWindows;
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
    }
}
