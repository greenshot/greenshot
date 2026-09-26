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
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Destinations;
using Greenshot.Editor.Destinations;
using Greenshot.UI;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Modern WPF-styled interactive export flyout step that presents a capture thumbnail preview,
    /// allows quick forwarding to destinations, supports forwarding to other recipes, and acts
    /// as a rich error recovery UI when a prior export fails.
    /// </summary>
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
                if (context.Payload?.SharedRenderedBitmap != null)
                {
                    previewImg = context.Payload.SharedRenderedBitmap;
                }
                else if (context.Payload?.RawCapture?.Image != null)
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
            var allDests = DestinationHelper.GetAllDestinations()?.Where(d => d.IsActive && !string.Equals(d.Designation, "Picker", StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<IDestination>();
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

            var tcs = new TaskCompletionSource<(IDestination Dest, CaptureRecipe Recipe, bool OpenEditor)>();
            var uiContext = SimpleServiceProvider.Current.GetInstance<SynchronizationContext>(isOptional: true) ?? SynchronizationContext.Current;

            void ShowDialogOnUi()
            {
                try
                {
                    var window = new DynamicDestinationWindow(
                        title,
                        previewImg,
                        targetDests,
                        availableRecipes,
                        lastError,
                        timeoutSeconds);

                    window.ShowDialog();

                    tcs.SetResult((window.SelectedDestination, window.SelectedRecipeToForward, window.OpenInEditorRequested));
                }
                catch (Exception ex)
                {
                    Log.Error("Error displaying DynamicDestinationWindow", ex);
                    tcs.SetException(ex);
                }
                finally
                {
                    if (disposePreview)
                    {
                        previewImg?.Dispose();
                    }
                }
            }

            if (Application.Current?.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    ShowDialogOnUi();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(ShowDialogOnUi);
                }
            }
            else if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                ShowDialogOnUi();
            }
            else
            {
                var staThread = new Thread(() =>
                {
                    try
                    {
                        ShowDialogOnUi();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error on STA thread displaying DynamicDestinationWindow", ex);
                        tcs.TrySetException(ex);
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }

            var (selectedDest, selectedRecipe, openEditor) = await tcs.Task.ConfigureAwait(false);

            var surface = context.Payload?.EnsureSurface();
            var captureDetails = context.Payload?.RawCapture?.CaptureDetails;

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
                else if (surface != null && captureDetails != null)
                {
                    DestinationDispatcher.InvokeOnSta(uiContext, () =>
                    {
                        DestinationHelper.ExportCapture(false, EditorDestination.DESIGNATION, surface, captureDetails);
                    });
                }
            }
            else if (selectedDest != null)
            {
                context.LogStep($"DynamicDestination: User selected destination '{selectedDest.Description ?? selectedDest.Designation}'.");
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
