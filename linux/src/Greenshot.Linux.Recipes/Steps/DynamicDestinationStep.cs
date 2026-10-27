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

using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.DynamicDestination, "Dynamic Destination Flyout", "Lets the user pick a destination, open the editor, or forward the capture to another recipe.", "Destination")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Optional)]
    [StepParameter("Title", ContractDataType.String, Description = "Title of the flyout")]
    [StepParameter("Destinations", ContractDataType.Object, ValueType = typeof(List<string>), Description = "Destinations to offer (default: all)")]
    [StepParameter("AllowRecipeForwarding", ContractDataType.Boolean, DefaultValue = true, Description = "Offer to forward the capture to another recipe")]
    [StepParameter("ShowPreview", ContractDataType.Boolean, Description = "Show a preview of the capture")]
    [StepParameter("TimeoutSeconds", ContractDataType.Integer, Description = "Close the flyout after this many seconds")]
    [StepInputVariable("LastError", ContractDataType.String, Description = "Shown when the flyout is used to pick another destination after a failed export")]
    public class DynamicDestinationStep : ICaptureStep
    {
        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public DynamicDestinationStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "DynamicDestinationStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var host = RecipeServices.Host;
            if (host == null || !context.UserInteraction.IsInteractive)
            {
                throw new InteractionRequiredException("Destination flyout");
            }

            string title = Config.GetParameter<string>("Title") ?? "Export Capture";
            bool showPreview = Config.GetParameter<bool?>("ShowPreview") ?? true;
            bool allowForwarding = Config.GetParameter<bool?>("AllowRecipeForwarding") ?? true;
            int timeoutSeconds = Config.GetParameter<int?>("TimeoutSeconds") ?? 0;
            string lastError = context.Properties.TryGetValue("LastError", out var error) ? error?.ToString() : null;

            var all = host.Destinations.Where(d => d.IsAvailable && !string.Equals(d.Designation, WellKnownDestinations.Picker, StringComparison.OrdinalIgnoreCase)).ToList();
            var wanted = Config.GetParameter<List<string>>("Destinations");
            var offered = wanted is { Count: > 0 } ? all.Where(d => wanted.Any(w => DestinationExportStep.Matches(d, w))).ToList() : all;
            var recipes = allowForwarding
                ? RecipeServices.Manager?.GetAllRecipes().Where(r => r != null && r.IsEnabled && !string.Equals(r.Id, context.Recipe?.Id, StringComparison.OrdinalIgnoreCase)).ToList()
                : null;

            var choice = await host.ShowDestinationFlyoutAsync(title, showPreview ? context.Payload?.Image : null, offered, recipes ?? new List<CaptureRecipe>(), lastError,
                timeoutSeconds, cancellationToken).ConfigureAwait(false);
            if (choice == null)
            {
                context.LogStep("DynamicDestination: Flyout dismissed without destination selection.");
                return;
            }

            if (choice.OpenEditor || choice.Destination?.Designation == WellKnownDestinations.Editor)
            {
                context.LogStep("DynamicDestination: User selected Open in Editor.");
                var editor = all.FirstOrDefault(d => d.Designation == WellKnownDestinations.Editor) ?? choice.Destination;
                if (editor != null)
                {
                    await new DestinationDispatcher().DispatchAsync(context, [editor], cancellationToken).ConfigureAwait(false);
                }
            }
            else if (choice.Destination != null)
            {
                context.LogStep($"DynamicDestination: User selected destination '{choice.Destination.DisplayName}'.");
                await new DestinationDispatcher().DispatchAsync(context, [choice.Destination], cancellationToken).ConfigureAwait(false);
            }
            else if (choice.Recipe != null && RecipeServices.Pipeline is { } pipeline)
            {
                context.LogStep($"DynamicDestination: Forwarding capture to recipe '{choice.Recipe.Name}'.");
                var payload = context.Payload;
                // The capture is handed over: the target recipe disposes it
                context.Payload = null;
                await pipeline.ExecuteAsync(choice.Recipe, null, forwarded =>
                {
                    forwarded.Payload = payload;
                    forwarded.IsPayloadPreSupplied = true;
                    foreach (var property in context.Properties)
                    {
                        forwarded.Properties[property.Key] = property.Value;
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
