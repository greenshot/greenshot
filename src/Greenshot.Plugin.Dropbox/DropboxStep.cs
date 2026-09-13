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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Plugin.Dropbox
{
    /// <summary>
    /// Capture recipe step that uploads the current capture surface to Dropbox.
    /// </summary>
    public class DropboxStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DropboxStep));
        private readonly DropboxPlugin _plugin;

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public DropboxStep(RecipeNodeConfig config, DropboxPlugin plugin)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Name = config.Name ?? "DropboxUploadStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("DropboxStep: No surface available to upload.");
                Log.Warn("DropboxStep: Surface is null in context payload.");
                return Task.CompletedTask;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            context.LogStep("Uploading capture to Dropbox...");
            Log.Info("DropboxStep: Executing Dropbox upload.");

            bool success = _plugin.Upload(captureDetails, surface, out string uploadUrl);
            if (success)
            {
                if (!string.IsNullOrEmpty(uploadUrl))
                {
                    context.Properties["Dropbox.UploadUrl"] = uploadUrl;
                    surface.UploadUrl = uploadUrl;
                    context.LogStep($"Successfully uploaded capture to Dropbox: {uploadUrl}");
                    Log.InfoFormat("DropboxStep: Capture uploaded to Dropbox with URL '{0}'", uploadUrl);
                }
                else
                {
                    context.LogStep("DropboxStep: Upload to Dropbox succeeded.");
                }
            }
            else
            {
                context.LogStep("DropboxStep: Upload to Dropbox failed or was cancelled.");
                Log.Warn("DropboxStep: Upload to Dropbox failed.");
            }

            return Task.CompletedTask;
        }
    }
}
