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

using Greenshot.Linux.Recipes.Contracts;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.Notification, "Notification", "Shows the notification for the exports before it.", "Feedback")]
    [StepParameter("ShowNotification", ContractDataType.Boolean, Description = "Show the completion notification (default: settings)")]
    [StepInputVariable("ShowNotification", ContractDataType.Boolean, Description = "Overrides the ShowNotification parameter")]
    [StepOutputVariable("EnableCompletionNotification", ContractDataType.Boolean, "Whether the completion notification is shown")]
    public class NotificationStep : ICaptureStep
    {
        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public NotificationStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "NotificationStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            bool showNotification = ResolveShowNotification(context);
            context.Properties["EnableCompletionNotification"] = showNotification;
            if (!showNotification)
            {
                // The exports before this step are done with, a later Notification step doesn't show them
                context.GetExportLog().TakeUnnotified();
                return;
            }

            // The exports since the previous Notification step (of all destination and plugin steps, also of parallel branches)
            await ExportNotification.ShowAsync(context, null, cancellationToken).ConfigureAwait(false);
        }

        private bool ResolveShowNotification(CaptureFlowContext context)
        {
            if (context.Properties.TryGetValue("ShowNotification", out var contextValue) && contextValue is bool contextBool)
            {
                return contextBool;
            }

            if (Config.Parameters.TryGetValue("ShowNotification", out var stepValue) && stepValue != null)
            {
                if (stepValue is bool stepBool) return stepBool;
                if (bool.TryParse(stepValue.ToString(), out bool parsed)) return parsed;
            }

            return RecipeServices.Settings?.ShowTrayNotification ?? true;
        }
    }
}
