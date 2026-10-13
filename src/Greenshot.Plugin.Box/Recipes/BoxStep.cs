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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Plugin.Box.Recipes
{
    /// <summary>
    /// Capture recipe step that uploads the current capture surface to Box.
    /// </summary>
    [StepInfo("Box", "Upload to Box", "Uploads the capture to Box (configured account).", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepOutputVariable("Box.UploadUrl", ContractDataType.String, "Link to the uploaded file", Conditional = true)]
    public class BoxStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BoxStep));
        private readonly BoxPlugin _plugin;

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        /// <summary>
        /// Uploads the capture: the user has to allow network access when approving a recipe with this step
        /// </summary>
        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Box (box.com)");
        }

        public BoxStep(RecipeNodeConfig config, BoxPlugin plugin)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Name = config.Name ?? "BoxUploadStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Payload?.EnsureSurface() == null)
            {
                context.LogStep("BoxStep: No surface available to upload.");
                Log.Warn("BoxStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            context.LogStep("Uploading capture to Box...");
            Log.Info("BoxStep: Executing Box upload.");

            var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            string uploadUrl;
            try
            {
                uploadUrl = await _plugin.UploadAsync(source, captureDetails, context.UserInteraction, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Same behaviour as the other upload steps: the flow continues, the failure is logged
                context.LogStep($"BoxStep: Upload to Box failed: {ex.Message}");
                Log.Error("BoxStep: Upload to Box failed.", ex);
                return;
            }

            if (!string.IsNullOrEmpty(uploadUrl))
            {
                context.Properties["Box.UploadUrl"] = uploadUrl;
                await source.UseSurfaceAsync(surface => surface.UploadUrl = uploadUrl, cancellationToken).ConfigureAwait(false);
                context.LogStep($"Successfully uploaded capture to Box: {uploadUrl}");
                Log.InfoFormat("BoxStep: Capture uploaded to Box with URL '{0}'", uploadUrl);
            }
            else
            {
                context.LogStep("BoxStep: Upload to Box did not produce a URL (declined).");
                Log.Warn("BoxStep: Upload to Box was declined.");
            }
        }
    }
}
