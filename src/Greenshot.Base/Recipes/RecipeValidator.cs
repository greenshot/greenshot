/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Result of validating a recipe against the formal written contract and schema.
    /// </summary>
    public class RecipeValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool HasExternalCommands { get; set; }
        public List<string> ExternalCommands { get; } = new List<string>();

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);

        public override string ToString()
        {
            if (IsValid)
            {
                return Warnings.Count > 0
                    ? $"Valid with {Warnings.Count} warning(s): {string.Join("; ", Warnings)}"
                    : "Valid";
            }
            return $"Invalid ({Errors.Count} error(s)): {string.Join("; ", Errors)}";
        }
    }

    /// <summary>
    /// Validates CaptureRecipe instances against the written JSON contract (recipe.schema.json).
    /// Prevents malformed, incomplete, or invalid recipes from being registered into the pipeline.
    /// </summary>
    public static class RecipeValidator
    {
        private static readonly HashSet<string> KnownStepTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            WellKnownStepTypes.Source,
            WellKnownStepTypes.InteractiveSelection,
            WellKnownStepTypes.Border,
            WellKnownStepTypes.Effect,
            WellKnownStepTypes.ImmediateFeedback,
            WellKnownStepTypes.Processors,
            WellKnownStepTypes.Destinations,
            WellKnownStepTypes.Notification,
            WellKnownStepTypes.Conditional,
            WellKnownStepTypes.TextEffect,
            "ObfuscateText",
            "ExternalCommand"
        };

        private static readonly HashSet<string> KnownTriggerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Greenshot.Base.Triggers.TriggerConfig.TypeHotkey,
            Greenshot.Base.Triggers.TriggerConfig.TypeContextMenu,
            Greenshot.Base.Triggers.TriggerConfig.TypeSystray,
            Greenshot.Base.Triggers.TriggerConfig.TypeClipboard,
            Greenshot.Base.Triggers.TriggerConfig.TypeManual,
            Greenshot.Base.Triggers.TriggerConfig.TypeSchedule
        };

        /// <summary>
        /// Validates a CaptureRecipe against the formal schema contract.
        /// </summary>
        public static RecipeValidationResult Validate(CaptureRecipe recipe)
        {
            var result = new RecipeValidationResult();

            if (recipe == null)
            {
                result.AddError("Recipe cannot be null.");
                return result;
            }

            // Validate Version
            if (string.IsNullOrWhiteSpace(recipe.Version))
            {
                result.AddWarning("Recipe 'version' is missing; defaulting to '1.0'.");
            }
            else
            {
                string major = recipe.Version.Split('.')[0].Trim();
                if (major != "1")
                {
                    result.AddError($"Unsupported recipe version '{recipe.Version}'. This version of Greenshot supports version 1.x recipes.");
                }
            }

            if (string.IsNullOrWhiteSpace(recipe.Id))
            {
                result.AddError("Recipe 'id' is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(recipe.Name))
            {
                result.AddError("Recipe 'name' is required and cannot be empty.");
            }

            // Validate Triggers (optional)
            if (recipe.Triggers != null)
            {
                for (int i = 0; i < recipe.Triggers.Count; i++)
                {
                    var trigger = recipe.Triggers[i];
                    ValidateTrigger(trigger, i, result);
                }
            }

            if (recipe.Steps == null || recipe.Steps.Count == 0)
            {
                result.AddError("Recipe must contain at least one step in 'steps'.");
                return result;
            }

            for (int i = 0; i < recipe.Steps.Count; i++)
            {
                var step = recipe.Steps[i];
                ValidateStep(step, i, result);
            }

            return result;
        }

        private static void ValidateTrigger(Greenshot.Base.Triggers.TriggerConfig trigger, int index, RecipeValidationResult result)
        {
            if (trigger == null)
            {
                result.AddError($"Trigger at index {index} cannot be null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(trigger.TriggerType))
            {
                result.AddError($"Trigger at index {index} is missing required 'triggerType'.");
                return;
            }

            if (!KnownTriggerTypes.Contains(trigger.TriggerType))
            {
                result.AddWarning($"Trigger at index {index} has unrecognized triggerType '{trigger.TriggerType}'.");
            }

            if (string.Equals(trigger.TriggerType, Greenshot.Base.Triggers.TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
            {
                string hotkey = trigger.GetParameter<string>("Hotkey");
                if (string.IsNullOrWhiteSpace(hotkey))
                {
                    result.AddError($"Hotkey trigger '{trigger.Name}' at index {index} is missing required 'Hotkey' parameter.");
                }
            }
        }

        private static void ValidateStep(RecipeStepConfig step, int index, RecipeValidationResult result)
        {
            if (step == null)
            {
                result.AddError($"Step at index {index} cannot be null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(step.StepType))
            {
                result.AddError($"Step at index {index} is missing required 'stepType'.");
                return;
            }

            if (!KnownStepTypes.Contains(step.StepType))
            {
                // Non-standard step types might be provided by plugins, treat as warning rather than hard rejection
                result.AddWarning($"Step at index {index} has unrecognized stepType '{step.StepType}'. Ensure a matching plugin step factory is registered.");
            }

            // Step-specific parameter validations
            if (string.Equals(step.StepType, WellKnownStepTypes.Border, StringComparison.OrdinalIgnoreCase))
            {
                if (step.Parameters.TryGetValue("Width", out var w) && w != null)
                {
                    try
                    {
                        int width = Convert.ToInt32(w);
                        if (width < 1)
                        {
                            result.AddError($"Step '{step.Name}' [Border]: 'Width' must be greater than or equal to 1 (got {width}).");
                        }
                    }
                    catch
                    {
                        result.AddError($"Step '{step.Name}' [Border]: 'Width' must be a valid integer.");
                    }
                }
            }
            else if (string.Equals(step.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase))
            {
                if (step.Parameters.TryGetValue("SourceType", out var st) && st is string stStr)
                {
                    if (!Enum.TryParse<CaptureSourceType>(stStr, true, out _))
                    {
                        result.AddError($"Step '{step.Name}' [Source]: Unknown SourceType '{stStr}'.");
                    }
                }

                if (step.Parameters.TryGetValue("DelayMs", out var delay) && delay != null)
                {
                    try
                    {
                        int d = Convert.ToInt32(delay);
                        if (d < 0)
                        {
                            result.AddError($"Step '{step.Name}' [Source]: 'DelayMs' cannot be negative.");
                        }
                    }
                    catch
                    {
                        result.AddError($"Step '{step.Name}' [Source]: 'DelayMs' must be a valid integer.");
                    }
                }
            }
            else if (string.Equals(step.StepType, WellKnownStepTypes.InteractiveSelection, StringComparison.OrdinalIgnoreCase))
            {
                if (step.Parameters.TryGetValue("SelectionMode", out var sm) && sm is string smStr)
                {
                    if (!Enum.TryParse<CaptureMode>(smStr, true, out _))
                    {
                        result.AddError($"Step '{step.Name}' [InteractiveSelection]: Unknown SelectionMode '{smStr}'.");
                    }
                }
            }
            else if (string.Equals(step.StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase))
            {
                if (!step.Parameters.ContainsKey("Condition") && !step.Parameters.ContainsKey("condition"))
                {
                    result.AddError($"Step '{step.Name}' [Conditional]: A 'Condition' parameter must be specified.");
                }
            }
            else if (string.Equals(step.StepType, "ExternalCommand", StringComparison.OrdinalIgnoreCase) ||
                     step.Parameters.ContainsKey("Command") ||
                     step.Parameters.ContainsKey("Executable"))
            {
                result.HasExternalCommands = true;
                string cmd = step.GetParameter<string>("Command") ?? step.GetParameter<string>("Executable") ?? step.Name;
                result.ExternalCommands.Add(cmd);
            }
        }
    }
}
