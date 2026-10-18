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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.CustomUploader.Destinations;
using Greenshot.Plugin.CustomUploader.Sxcu;
using log4net;

namespace Greenshot.Plugin.CustomUploader.Recipes
{
    /// <summary>
    /// The names of the imported uploaders, for the Uploader parameter
    /// </summary>
    public sealed class UploaderNames : IAllowedValuesProvider
    {
        public IReadOnlyList<string> GetAllowedValues() => CustomUploaderPlugin.GetUploaders().Select(definition => definition.Name).ToList();
    }

    /// <summary>
    /// Capture recipe step that uploads the capture with an imported uploader (beta, registered only for beta testers).
    /// </summary>
    [StepInfo("CustomUploader", "Upload with a custom uploader", "Uploads the capture with an uploader imported from an .sxcu file.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Uploader", ContractDataType.Enum, Description = "Name of the imported uploader, empty when there is only one", AllowedValuesProvider = typeof(UploaderNames))]
    [StepOutputVariable("CustomUploader.Url", ContractDataType.String, "Link to the upload", Conditional = true)]
    public class CustomUploaderStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CustomUploaderStep));
        private readonly CustomUploaderPlugin _plugin;

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public CustomUploaderStep(RecipeNodeConfig config, CustomUploaderPlugin plugin)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Name = config.Name ?? "CustomUploaderStep";
        }

        /// <summary>
        /// The user approves the uploader and its server; an uploader imported again with another server needs a new approval
        /// </summary>
        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            var definition = FindUploader();
            yield return definition != null
                ? CustomUploaderPlugin.CreateGatedAction(definition)
                : new RecipeGatedAction(RecipeGateType.NetworkAccess, NodeConfig.GetParameter<string>("Uploader") ?? CustomUploaderDestination.DesignationName);
        }

        /// <summary>
        /// The uploader with the name of the parameter, or the only one when the parameter is empty
        /// </summary>
        private UploaderDefinition FindUploader()
        {
            string name = NodeConfig.GetParameter<string>("Uploader");
            var uploaders = CustomUploaderPlugin.GetUploaders();
            if (string.IsNullOrWhiteSpace(name))
            {
                return uploaders.Length == 1 ? uploaders[0] : null;
            }

            return uploaders.FirstOrDefault(definition => string.Equals(definition.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Payload?.EnsureSurface() == null)
            {
                context.LogStep("CustomUploaderStep: No surface available to upload.");
                Log.Warn("CustomUploaderStep: Surface is null in context payload.");
                return;
            }

            var definition = FindUploader();
            if (definition == null)
            {
                context.LogStep($"CustomUploaderStep: No uploader '{NodeConfig.GetParameter<string>("Uploader")}', import it in the settings.");
                Log.Warn("CustomUploaderStep: The uploader of the step isn't imported.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();
            var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            var destination = new UploaderDestination(_plugin, definition);
            ExportResult result;
            try
            {
                context.LogStep($"Uploading capture with {definition.Name}...");
                result = await _plugin.UploadAsync(source, captureDetails, definition, context.UserInteraction, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Same behaviour as the other upload steps: the flow continues, the failure is logged and shown in the notification
                Log.Error("CustomUploaderStep: Upload failed.", ex);
                result = ExportResult.Failed(ex.Message, ex);
            }

            if (result.IsSucceeded && result.Uri != null)
            {
                context.Properties["CustomUploader.Url"] = result.Uri.AbsoluteUri;
                context.LogStep($"CustomUploaderStep: Uploaded with {definition.Name}.");
            }
            else
            {
                context.LogStep($"CustomUploaderStep: Upload with {definition.Name} failed: {result.Error}");
            }

            // The same result as the destination, the Notification step shows it
            await ExportResultHandler.ApplyAsync(destination, result, source, cancellationToken).ConfigureAwait(false);
            context.RecordExport(destination, result, captureDetails, source);
        }
    }
}
