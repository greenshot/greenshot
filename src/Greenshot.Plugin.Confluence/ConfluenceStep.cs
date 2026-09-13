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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.Confluence.Entities;
using log4net;

namespace Greenshot.Plugin.Confluence
{
    /// <summary>
    /// Capture recipe step that uploads/attaches the screenshot to a Confluence page.
    /// </summary>
    public class ConfluenceStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfluenceStep));
        private static IConfluenceConfiguration Config => IniConfigRegistry.GetSection<IConfluenceConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public ConfluenceStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ConfluenceUploadStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("ConfluenceStep: No surface available to upload.");
                Log.Warn("ConfluenceStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            string pageId = NodeConfig.GetParameter<string>("PageId")
                ?? NodeConfig.GetParameter<string>("pageId")
                ?? NodeConfig.GetParameter<string>("Page")
                ?? NodeConfig.GetParameter<string>("page");

            if (!string.IsNullOrEmpty(pageId))
            {
                pageId = FilenameHelper.FillVariables(pageId, false);
            }

            string formatStr = NodeConfig.GetParameter<string>("Format") ?? NodeConfig.GetParameter<string>("UploadFormat");
            OutputFormat uploadFormat = Config?.UploadFormat ?? OutputFormat.png;
            if (!string.IsNullOrWhiteSpace(formatStr) && Enum.TryParse<OutputFormat>(formatStr, true, out var parsedFormat))
            {
                uploadFormat = parsedFormat;
            }

            int jpegQuality = NodeConfig.GetParameter<int?>("JpegQuality") ?? (Config?.UploadJpegQuality ?? 80);
            bool reduceColors = NodeConfig.GetParameter<bool?>("ReduceColors") ?? (Config?.UploadReduceColors ?? false);
            var outputSettings = new SurfaceOutputSettings(uploadFormat, jpegQuality, reduceColors);

            string filename = Path.GetFileName(FilenameHelper.GetFilename(uploadFormat, captureDetails));
            string extension = "." + uploadFormat.ToString().ToLower();
            if (!filename.ToLower().EndsWith(extension))
            {
                filename += extension;
            }

            if (!string.IsNullOrEmpty(pageId) && long.TryParse(pageId, out long parsedPageId))
            {
                context.LogStep($"Uploading capture to Confluence page '{parsedPageId}'...");
                Log.InfoFormat("ConfluenceStep: Uploading capture to Confluence page '{0}'", parsedPageId);

                await Task.Run(() =>
                {
                    try
                    {
                        var connector = ConfluencePlugin.ConfluenceConnector;
                        if (connector != null)
                        {
                            var surfaceContainer = new SurfaceContainer(surface, outputSettings, filename);
                            connector.AddAttachment(parsedPageId, "image/" + uploadFormat.ToString().ToLower(), null, filename, surfaceContainer);
                            context.Properties["Confluence.PageId"] = parsedPageId.ToString();
                            context.LogStep($"Successfully uploaded capture to Confluence page '{parsedPageId}'.");
                        }
                        else
                        {
                            context.LogStep("ConfluenceStep: Confluence connector could not connect.");
                        }
                    }
                    catch (Exception ex)
                    {
                        context.LogStep($"ConfluenceStep: Failed to upload to '{parsedPageId}': {ex.Message}");
                        Log.Error($"ConfluenceStep: Error uploading to page {parsedPageId}", ex);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Fall back to destination dialog
                context.LogStep("ConfluenceStep: Delegating to Confluence destination.");
                var destination = new ConfluenceDestination();
                destination.ExportCapture(false, surface, captureDetails);
                if (!string.IsNullOrEmpty(surface.UploadUrl))
                {
                    context.Properties["Confluence.UploadUrl"] = surface.UploadUrl;
                }
            }
        }
    }
}
