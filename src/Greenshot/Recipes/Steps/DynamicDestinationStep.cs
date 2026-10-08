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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Contracts = Greenshot.Base.Recipes.Contracts;

using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using Greenshot.Recipes.Pipeline;
using Greenshot.Recipes.Views;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Modern WPF-styled interactive export flyout step that presents a capture thumbnail preview,
    /// allows quick forwarding to destinations, supports forwarding to other recipes, and acts
    /// as a rich error recovery UI when a prior export fails.
    /// </summary>
    [StepInfo(WellKnownStepTypes.DynamicDestination, "Dynamic Destination Flyout", "Lets the user pick a destination, open the editor, or forward the capture to another recipe.", "Destination")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Optional)]
    [StepParameter("Title", ContractDataType.String, Description = "Title of the flyout")]
    [StepParameter("Destinations", ContractDataType.Object, Description = "Destinations to offer (default: all)")]
    [StepParameter("AllowRecipeForwarding", ContractDataType.Boolean, DefaultValue = true, Description = "Offer to forward the capture to another recipe")]
    [StepParameter("ShowPreview", ContractDataType.Boolean, Description = "Show a preview of the capture")]
    [StepParameter("TimeoutSeconds", ContractDataType.Integer, Description = "Close the flyout after this many seconds")]
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
            bool showPreview = Config.GetParameter<bool?>("ShowPreview") ?? true;
            bool allowRecipeForwarding = Config.GetParameter<bool?>("AllowRecipeForwarding") ?? true;
            int timeoutSeconds = Config.GetParameter<int?>("TimeoutSeconds") ?? 0;

            string lastError = null;
            if (context.Properties.TryGetValue("LastError", out var errObj) && errObj != null)
            {
                lastError = errObj.ToString();
            }

            Image previewImg = null;
            bool disposePreview = false;
            if (showPreview)
            {
                if (context.Payload?.RawCapture?.Image != null)
                {
                    previewImg = context.Payload.RawCapture.Image;
                }
                else
                {
                    var surf = context.Payload?.EnsureSurface();
                    if (surf != null)
                    {
                        previewImg = surf.GetImageForExport();
                        disposePreview = true;
                    }
                }
            }

            // Resolve destinations (excluding legacy WinForms destination picker)
            var allDests = DestinationHelper.GetAllDestinations()?.Where(d => d.IsAvailableFor(context.Payload?.RawCapture?.CaptureDetails) && !string.Equals(d.Designation, "Picker", StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<IDestination>();
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

            // Resolve other recipes if forwarding is enabled
            List<CaptureRecipe> availableRecipes = null;
            if (allowRecipeForwarding)
            {
                var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);
                if (recipeManager != null)
                {
                    availableRecipes = recipeManager.GetAllRecipes()
                        .Where(r => r != null && r.IsEnabled && !string.Equals(r.Id, context.Recipe?.Id, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            (IDestination Dest, CaptureRecipe Recipe, bool OpenEditor) choice;
            try
            {
                // The flyout is UI: shown on the UI thread, the flow waits without blocking
                choice = await context.Ui.InvokeAsync(() =>
                {
                    var window = new DynamicDestinationWindow(
                        title,
                        previewImg,
                        targetDests,
                        availableRecipes,
                        lastError,
                        timeoutSeconds);

                    // A cancelled flow closes the flyout, the close is posted to the UI thread
                    using (cancellationToken.Register(() => context.Ui.InvokeAsync(() =>
                           {
                               if (window.IsVisible)
                               {
                                   window.Close();
                               }
                           }, CancellationToken.None).FireAndLog("Close the destination flyout", Log)))
                    {
                        window.ShowDialog();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return (window.SelectedDestination, window.SelectedRecipeToForward, window.OpenInEditorRequested);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (disposePreview)
                {
                    previewImg?.Dispose();
                }
            }

            var (selectedDest, selectedRecipe, openEditor) = choice;

            if (openEditor || (selectedDest != null && EditorDestination.DESIGNATION.Equals(selectedDest.Designation, StringComparison.OrdinalIgnoreCase)))
            {
                context.Payload.RetainSurfaceForEditor = true;
                context.LogStep("DynamicDestination: User selected Open in Editor.");
                var editorDest = DestinationHelper.GetDestination(EditorDestination.DESIGNATION) ?? selectedDest;
                if (editorDest != null)
                {
                    var dispatcher = new DestinationDispatcher();
                    await dispatcher.DispatchAsync(context, new[] { editorDest }, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (selectedDest != null)
            {
                context.LogStep($"DynamicDestination: User selected destination '{selectedDest.Descriptor?.DisplayName ?? selectedDest.Designation}'.");
                var dispatcher = new DestinationDispatcher();
                await dispatcher.DispatchAsync(context, new[] { selectedDest }, cancellationToken).ConfigureAwait(false);
            }
            else if (selectedRecipe != null)
            {
                context.LogStep($"DynamicDestination: Forwarding capture to recipe '{selectedRecipe.Name}'.");
                var pipeline = SimpleServiceProvider.Current.GetInstance<ICapturePipeline>(isOptional: true);
                if (pipeline != null)
                {
                    await pipeline.ExecuteAsync(selectedRecipe, null, ctx =>
                    {
                        ctx.Payload = context.Payload;
                        // The capture is handed over: the target recipe must not capture or select again
                        ctx.IsPayloadPreSupplied = true;
                        foreach (var kvp in context.Properties)
                        {
                            ctx.Properties[kvp.Key] = kvp.Value;
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                context.LogStep("DynamicDestination: Flyout dismissed without destination selection.");
            }
        }
    }
}
