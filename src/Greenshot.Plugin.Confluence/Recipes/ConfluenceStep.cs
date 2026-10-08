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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.Confluence.Api;
using Greenshot.Plugin.Confluence.Api.Entities;
using Greenshot.Plugin.Confluence.Destinations;
using log4net;
using Greenshot.Base.Core.Export;

namespace Greenshot.Plugin.Confluence.Recipes
{
    /// <summary>
    /// Capture recipe step that uploads/attaches the screenshot to a Confluence page.
    /// </summary>
    [StepInfo("Confluence", "Upload to Confluence", "Uploads the capture as attachment to a Confluence page.", "Export")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Required)]
    [StepParameter("Format", ContractDataType.Enum, Description = "Image format of the upload", AllowedValuesProvider = typeof(SaveableFileFormatIds))]
    [StepParameter("JpegQuality", ContractDataType.Integer, Description = "JPEG quality (1-100) when uploading as JPEG")]
    [StepParameter("ReduceColors", ContractDataType.Boolean, Description = "Reduce the image to 256 colors")]
    [StepParameter("PageId", ContractDataType.String, Description = "Page to attach to")]
    [StepOutputVariable("Confluence.PageId", ContractDataType.String, "The page the capture was attached to", Conditional = true)]
    [StepOutputVariable("Confluence.UploadUrl", ContractDataType.String, "Link to the attachment", Conditional = true)]
    public class ConfluenceStep : ICaptureStep, IRequiresRecipeAuthorization
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfluenceStep));
        private static IConfluenceConfiguration Config => IniConfigRegistry.GetSection<IConfluenceConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        /// <summary>
        /// Uploads the capture: the user has to allow network access when approving a recipe with this step
        /// </summary>
        public IEnumerable<RecipeGatedAction> GetGatedActions()
        {
            yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Confluence");
        }

        public ConfluenceStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ConfluenceUploadStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Payload?.EnsureSurface() == null)
            {
                context.LogStep("ConfluenceStep: No surface available to upload.");
                Log.Warn("ConfluenceStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            string pageId = NodeConfig.GetParameter<string>("PageId");

            if (!string.IsNullOrEmpty(pageId))
            {
                pageId = FilenameHelper.FillVariables(pageId, false);
            }

            string formatStr = NodeConfig.GetParameter<string>("Format");
            var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            string uploadFormat = formatRegistry.ResolveFormatId(formatStr, Config?.UploadFormat ?? WellKnownFileFormats.Png);

            int jpegQuality = NodeConfig.GetParameter<int?>("JpegQuality") ?? (Config?.UploadJpegQuality ?? 80);
            bool reduceColors = NodeConfig.GetParameter<bool?>("ReduceColors") ?? (Config?.UploadReduceColors ?? false);
            var outputSettings = new SurfaceOutputSettings(uploadFormat, jpegQuality, reduceColors);

            string filename = Path.GetFileName(FilenameHelper.GetFilename(uploadFormat, captureDetails));

            var source = await context.Payload.GetExportSourceAsync(context.Ui, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(pageId) && long.TryParse(pageId, out long parsedPageId))
            {
                context.LogStep($"Uploading capture to Confluence page '{parsedPageId}'...");
                Log.InfoFormat("ConfluenceStep: Uploading capture to Confluence page '{0}'", parsedPageId);

                try
                {
                    var connector = ConfluencePlugin.ConfluenceConnector;
                    if (connector != null)
                    {
                        var image = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
                        await connector.AddAttachmentAsync(parsedPageId, image, filename, null, cancellationToken).ConfigureAwait(false);
                        context.Properties["Confluence.PageId"] = parsedPageId.ToString();
                        context.LogStep($"Successfully uploaded capture to Confluence page '{parsedPageId}'.");
                    }
                    else
                    {
                        context.LogStep("ConfluenceStep: Confluence connector could not connect.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.LogStep($"ConfluenceStep: Failed to upload to '{parsedPageId}': {ex.Message}");
                    Log.Error($"ConfluenceStep: Error uploading to page {parsedPageId}", ex);
                }
            }
            else
            {
                // Fall back to destination dialog
                context.LogStep("ConfluenceStep: Delegating to Confluence destination.");
                var destination = new ConfluenceDestination();
                var result = await DestinationExporter.ExportAsync(destination, source, captureDetails, false, context.UserInteraction, cancellationToken).ConfigureAwait(false);
                await ExportResultHandler.ApplyAsync(destination, result, source, cancellationToken).ConfigureAwait(false);
                if (result.Uri != null)
                {
                    context.Properties["Confluence.UploadUrl"] = result.Uri.AbsoluteUri;
                }
            }
        }
    }
}
