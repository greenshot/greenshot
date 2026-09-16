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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.Office.Destinations;
using log4net;

namespace Greenshot.Plugin.Office
{
    /// <summary>
    /// Capture recipe step that inserts/exports the capture surface into Microsoft Office applications
    /// (Word, Excel, PowerPoint, OneNote, Outlook).
    /// </summary>
    public class OfficeStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OfficeStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public OfficeStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "OfficeExportStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var surface = context.Payload?.EnsureSurface();
            if (surface == null)
            {
                context.LogStep("OfficeStep: No surface available to export.");
                Log.Warn("OfficeStep: Surface is null in context payload.");
                return Task.CompletedTask;
            }

            var captureDetails = context.Payload?.RawCapture?.CaptureDetails ?? new CaptureDetails();

            // Determine target Office application
            string appName = NodeConfig.GetParameter<string>("Application")
                ?? NodeConfig.GetParameter<string>("application")
                ?? NodeConfig.GetParameter<string>("Target")
                ?? NodeConfig.GetParameter<string>("target")
                ?? NodeConfig.StepType;

            IDestination destination = null;
            if (string.Equals(appName, "Excel", StringComparison.OrdinalIgnoreCase))
            {
                destination = new ExcelDestination();
            }
            else if (string.Equals(appName, "PowerPoint", StringComparison.OrdinalIgnoreCase) || string.Equals(appName, "Powerpoint", StringComparison.OrdinalIgnoreCase))
            {
                destination = new PowerpointDestination();
            }
            else if (string.Equals(appName, "Word", StringComparison.OrdinalIgnoreCase))
            {
                destination = new WordDestination();
            }
            else if (string.Equals(appName, "OneNote", StringComparison.OrdinalIgnoreCase) || string.Equals(appName, "Onenote", StringComparison.OrdinalIgnoreCase))
            {
                destination = new OneNoteDestination();
            }
            else if (string.Equals(appName, "Outlook", StringComparison.OrdinalIgnoreCase))
            {
                destination = new OutlookDestination();
            }
            else
            {
                destination = new WordDestination();
            }

            context.LogStep($"Exporting capture to Office application ({appName})...");
            Log.InfoFormat("OfficeStep: Exporting capture to {0}", appName);

            try
            {
                var exportInfo = destination.ExportCapture(false, surface, captureDetails);
                if (exportInfo != null && exportInfo.ExportMade)
                {
                    context.LogStep($"Successfully exported capture to {appName}.");
                }
                else
                {
                    context.LogStep($"OfficeStep: Export to {appName} was not completed.");
                }
            }
            catch (Exception ex)
            {
                context.LogStep($"OfficeStep: Error exporting to {appName}: {ex.Message}");
                Log.Error($"OfficeStep: Failed to export to {appName}", ex);
            }

            return Task.CompletedTask;
        }
    }
}
