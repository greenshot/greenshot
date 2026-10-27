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
using Greenshot.Linux.Recipes.Expressions;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes.Steps
{
    [StepInfo(WellKnownStepTypes.ImmediateFeedback, "Immediate Feedback", "Plays the camera sound after a capture (not for a capture handed to the flow).", "Feedback")]
    [StepParameter("PlaySound", ContractDataType.Boolean, Description = "Play the camera sound (default: settings)")]
    [StepParameter("SoundFilePath", ContractDataType.FilePath, Description = "Sound file to play instead of the camera sound")]
    [StepInputVariable("PlayShutterSound", ContractDataType.Boolean, Description = "Overrides PlaySound")]
    [StepInputVariable("SoundFilePath", ContractDataType.FilePath, Description = "Overrides SoundFilePath")]
    public class ImmediateFeedbackStep : ICaptureStep
    {
        public string Name { get; }

        public RecipeNodeConfig Config { get; }

        public ImmediateFeedbackStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ImmediateFeedbackStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            // Nothing was captured when the image was handed over, so no shutter sound
            if (context.IsPayloadPreSupplied || !ResolvePlaySound(context))
            {
                return Task.CompletedTask;
            }

            RecipeServices.Host?.PlaySound(ResolveSoundFilePath(context));
            return Task.CompletedTask;
        }

        private bool ResolvePlaySound(CaptureFlowContext context)
        {
            if (context.Properties.TryGetValue("PlayShutterSound", out var contextValue) && contextValue is bool contextBool)
            {
                return contextBool;
            }

            if (Config.Parameters.TryGetValue("PlaySound", out var stepValue) && stepValue != null)
            {
                if (stepValue is bool stepBool) return stepBool;
                if (bool.TryParse(stepValue.ToString(), out bool parsed)) return parsed;
            }

            return RecipeServices.Settings?.PlayCameraSound ?? false;
        }

        /// <summary>
        /// The sound file, null for the camera sound
        /// </summary>
        private string ResolveSoundFilePath(CaptureFlowContext context)
        {
            if (context.Properties.TryGetValue("SoundFilePath", out var contextValue) && contextValue is string contextPath && !string.IsNullOrWhiteSpace(contextPath))
            {
                return contextPath;
            }

            if (Config.Parameters.TryGetValue("SoundFilePath", out var stepValue) && !string.IsNullOrWhiteSpace(stepValue?.ToString()))
            {
                return ExpressionEvaluator.Instance.Evaluate<string>(stepValue.ToString(), context);
            }

            var configured = RecipeServices.Settings?.GetValue("Core", "NotificationSound");
            return string.IsNullOrWhiteSpace(configured) ? null : configured;
        }
    }
}
