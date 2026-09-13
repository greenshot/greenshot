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
using Dapplo.HttpExtensions;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    /// Capture recipe step that attaches the screenshot to a Jira issue.
    /// </summary>
    public class JiraStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(JiraStep));
        private static IJiraConfiguration Config => IniConfigRegistry.GetSection<IJiraConfiguration>();

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public JiraStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "JiraUploadStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("JiraStep: No surface available to upload.");
                Log.Warn("JiraStep: Surface is null in context payload.");
                return;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();
            var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>(isOptional: true);
            if (jiraConnector == null)
            {
                context.LogStep("JiraStep: JiraConnector service is not available.");
                Log.Warn("JiraStep: JiraConnector not registered.");
                return;
            }

            string issueKey = NodeConfig.GetParameter<string>("IssueKey")
                ?? NodeConfig.GetParameter<string>("issueKey")
                ?? NodeConfig.GetParameter<string>("Issue")
                ?? NodeConfig.GetParameter<string>("issue")
                ?? (context.Properties.TryGetValue("Jira.IssueKey", out var ik) && ik is string iks ? iks : null);

            if (!string.IsNullOrEmpty(issueKey))
            {
                issueKey = FilenameHelper.FillVariables(issueKey, false);
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
            var surfaceContainer = new SurfaceContainer(surface, outputSettings, filename);

            if (!string.IsNullOrEmpty(issueKey))
            {
                context.LogStep($"Attaching capture to Jira issue '{issueKey}'...");
                Log.InfoFormat("JiraStep: Attaching capture to Jira issue '{0}'", issueKey);

                try
                {
                    await jiraConnector.AttachAsync(issueKey, surfaceContainer).ConfigureAwait(false);
                    string uploadUrl = jiraConnector.JiraBaseUri.AppendSegments("browse", issueKey).AbsoluteUri;
                    surface.UploadUrl = uploadUrl;
                    context.Properties["Jira.UploadUrl"] = uploadUrl;
                    context.Properties["Jira.IssueKey"] = issueKey;
                    context.LogStep($"Successfully attached capture to Jira issue '{issueKey}': {uploadUrl}");
                }
                catch (Exception ex)
                {
                    context.LogStep($"JiraStep: Failed to attach capture to '{issueKey}': {ex.Message}");
                    Log.Error($"JiraStep: Error attaching capture to {issueKey}", ex);
                }
            }
            else
            {
                // Dispatch via JiraDestination (interactive dialog or default)
                context.LogStep("JiraStep: No issue key specified; delegating to Jira destination.");
                var destination = new JiraDestination();
                destination.ExportCapture(false, surface, captureDetails);
                if (!string.IsNullOrEmpty(surface.UploadUrl))
                {
                    context.Properties["Jira.UploadUrl"] = surface.UploadUrl;
                }
            }
        }
    }
}
