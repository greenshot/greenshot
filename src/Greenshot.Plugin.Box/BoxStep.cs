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

namespace Greenshot.Plugin.Box
{
    /// <summary>
    /// Capture recipe step that uploads the current capture surface to Box.
    /// </summary>
    public class BoxStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BoxStep));
        private readonly BoxPlugin _plugin;

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public BoxStep(RecipeNodeConfig config, BoxPlugin plugin)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Name = config.Name ?? "BoxUploadStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("BoxStep: No surface available to upload.");
                Log.Warn("BoxStep: Surface is null in context payload.");
                return Task.CompletedTask;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            context.LogStep("Uploading capture to Box...");
            Log.Info("BoxStep: Executing Box upload.");

            string uploadUrl = _plugin.Upload(captureDetails, surface);
            if (!string.IsNullOrEmpty(uploadUrl))
            {
                context.Properties["Box.UploadUrl"] = uploadUrl;
                surface.UploadUrl = uploadUrl;
                context.LogStep($"Successfully uploaded capture to Box: {uploadUrl}");
                Log.InfoFormat("BoxStep: Capture uploaded to Box with URL '{0}'", uploadUrl);
            }
            else
            {
                context.LogStep("BoxStep: Upload to Box did not produce a URL (cancelled or failed).");
                Log.Warn("BoxStep: Upload to Box failed or was cancelled.");
            }

            return Task.CompletedTask;
        }
    }
}
