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
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Editor.Destinations;
using Greenshot.Recipes.Pipeline;
using Greenshot.Recipes.Views;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// The destination window as a step: a preview of the capture, the editor recipes to change it (like in the editor), and the
    /// destinations to choose from. It also is the error recovery UI when a prior export failed.
    /// </summary>
    [StepInfo(WellKnownStepTypes.DynamicDestination, "Dynamic Destination Flyout", "Lets the user change the capture with an editor recipe and pick a destination.", "Destination")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Optional)]
    [StepParameter("Title", ContractDataType.String, Translatable = true, Description = "Title of the flyout")]
    [StepParameter("Destinations", ContractDataType.Object, ValueType = typeof(List<string>), Description = "Destinations to offer (default: all)")]
    [StepParameter("ShowPreview", ContractDataType.Boolean, Description = "Show a preview of the capture (default: the setting of Greenshot)")]
    [StepParameter("TimeoutSeconds", ContractDataType.Integer, Description = "Close the flyout after this many seconds")]
    [StepParameter("WindowPosition", ContractDataType.Enum, DefaultValue = "Default", Description = "Where the window opens: Center is the middle of the screen, Mouse is at the mouse pointer, Default follows the setting of Greenshot",
        AllowedValues = new[] { "Default", "Center", "Mouse" })]
    [StepInputVariable("LastError", ContractDataType.String, Description = "Shown when the flyout is used to pick another destination after a failed export")]
    public class DynamicDestinationStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DynamicDestinationStep));

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public DynamicDestinationStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "DynamicDestinationStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            string title = Config.GetParameter<string>("Title") ?? "Export Capture";
            // Without a choice in the step the setting of Greenshot decides
            bool showPreview = Config.GetParameter<bool?>("ShowPreview") ?? IniConfigRegistry.GetSection<ICoreConfiguration>().DestinationWindowShowPreview;
            int timeoutSeconds = Config.GetParameter<int?>("TimeoutSeconds") ?? 0;
            // Mouse or Center; without one (or Default) the setting decides
            string windowPosition = Config.GetParameter<string>("WindowPosition");
            bool? atMouse = string.Equals(windowPosition, "Mouse", StringComparison.OrdinalIgnoreCase) ? true
                : string.Equals(windowPosition, "Center", StringComparison.OrdinalIgnoreCase) ? false
                : (bool?)null;

            string lastError = null;
            if (context.Properties.TryGetValue("LastError", out var errObj) && errObj != null)
            {
                lastError = errObj.ToString();
            }

            var payload = context.Payload;
            var surface = payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("DynamicDestination: there is no capture to export.");
                return;
            }

            // The destinations to choose from, without the destination picker and this window
            var allDests = DestinationHelper.GetChoices(payload.RawCapture?.CaptureDetails ?? surface.CaptureDetails);
            var specificDestDesignations = Config.GetParameter<List<string>>("Destinations");
            List<IDestination> targetDests;
            if (specificDestDesignations != null && specificDestDesignations.Count > 0)
            {
                targetDests = allDests.Where(d => specificDestDesignations.Contains(d.Designation, StringComparer.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                targetDests = allDests;
            }

            // The window works on a copy, which the editor recipes can replace; the changed copy becomes the capture of the flow
            var capture = await context.Ui.InvokeAsync(() => new EditableCapture(surface.Clone()), cancellationToken).ConfigureAwait(false);
            IDestination selectedDest;
            try
            {
                selectedDest = await context.Ui.InvokeAsync(
                    () => DynamicDestinationWindow.Choose(null, capture, targetDests, lastError, cancellationToken, title, timeoutSeconds, showPreview, atMouse),
                    cancellationToken).ConfigureAwait(false);
                if (capture.IsChanged)
                {
                    context.LogStep("DynamicDestination: the capture was changed with an editor recipe.");
                    await context.Ui.InvokeAsync(() => ReplaceSurface(payload, capture.Release()), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await context.Ui.InvokeAsync(capture.Dispose, CancellationToken.None).ConfigureAwait(false);
            }

            if (selectedDest == null)
            {
                context.LogStep("DynamicDestination: Flyout dismissed without destination selection.");
                return;
            }

            // The editor itself takes over the capture; an open editor chosen below it (a destination without entries of its own) only gets a copy
            if (EditorDestination.DESIGNATION.Equals(selectedDest.Designation, StringComparison.OrdinalIgnoreCase)
                && selectedDest.Descriptor?.HasDynamicDestinations == true)
            {
                context.Payload.RetainSurfaceForEditor = true;
                context.LogStep("DynamicDestination: User selected Open in Editor.");
            }
            else
            {
                context.LogStep($"DynamicDestination: User selected destination '{selectedDest.Descriptor?.DisplayName ?? selectedDest.Designation}'.");
            }

            var dispatcher = new DestinationDispatcher();
            await dispatcher.DispatchAsync(context, new[] { selectedDest }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The changed capture replaces the surface of the flow (on the UI thread, where the surfaces live)
        /// </summary>
        private static void ReplaceSurface(ICapturePayload payload, ISurface changed)
        {
            var previous = payload.Surface;
            payload.InvalidateExportSource();
            payload.Surface = changed;
            previous?.Dispose();
        }
    }
}
