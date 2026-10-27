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

#nullable disable

using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
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

        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public InteractiveSelectionStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "InteractiveSelectionStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null)
            {
                context.Abort("No raw capture available for interactive selection.");
                return;
            }

            if (context.Properties.ContainsKey("PreSuppliedRegion"))
            {
                return;
            }

            var mode = Config.GetParameter("SelectionMode", CaptureMode.Region);
            // The capture screen shows the capture where it was on the screen, a capture handed over or of a window, file or the clipboard is used as a whole
            bool isScreen = !payload.Metadata.TryGetValue("source", out var source) || string.Equals(source as string, "Screen", StringComparison.OrdinalIgnoreCase);
            if (context.IsPayloadPreSupplied || !isScreen || RecipeServices.Host == null)
            {
                if (mode == CaptureMode.Text)
                {
                    await OcrStep.ExtractTextAsync(context, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            context.State = CaptureFlowState.Selecting;
            var outcome = await RecipeServices.Host.SelectAsync(payload, mode, Config.GetParameter("AllowWindowSnapping", true), cancellationToken).ConfigureAwait(false);
            if (outcome == null)
            {
                context.Abort("User cancelled interactive selection.");
                return;
            }

            if (!string.IsNullOrEmpty(outcome.WindowTitle))
            {
                context.Properties["SelectedWindow"] = outcome.WindowTitle;
            }

            payload.InvalidateExportSource();
            Log.DebugFormat("Selected {0}x{1} ({2})", payload.RawCapture.Image.Width, payload.RawCapture.Image.Height, outcome.Mode);
            if (outcome.Mode == CaptureMode.Text)
            {
                await OcrStep.ExtractTextAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
