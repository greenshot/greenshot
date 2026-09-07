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
using System.Collections;
using System.Collections.Generic;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Configuration for an individual modular step/block within a capture recipe.
    /// Supports dynamic configuration evaluation at runtime when parameters are left unset (null).
    /// </summary>
    public class RecipeStepConfig
    {
        public string StepType { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public RecipeStepConfig()
        {
        }

        public RecipeStepConfig(string stepType, string name = null)
        {
            StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
            Name = name ?? stepType;
        }

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters != null && Parameters.TryGetValue(key, out var val) && val != null)
            {
                if (val is T typed)
                {
                    return typed;
                }

                if (val is JToken jToken)
                {
                    try
                    {
                        return jToken.ToObject<T>();
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                try
                {
                    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    if (targetType.IsEnum)
                    {
                        if (val is string str)
                        {
                            return (T)Enum.Parse(targetType, str, true);
                        }
                        return (T)Enum.ToObject(targetType, val);
                    }

                    if (typeof(T) == typeof(List<string>) && val is IEnumerable enumerable && !(val is string))
                    {
                        var list = new List<string>();
                        foreach (var item in enumerable)
                        {
                            if (item != null) list.Add(item.ToString());
                        }
                        return (T)(object)list;
                    }

                    return (T)Convert.ChangeType(val, targetType);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public RecipeStepConfig Set(string key, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Parameters[key] = value;
            return this;
        }

        /// <summary>
        /// Fluent helper to override the display name of this step. Useful when the same step type
        /// appears multiple times in a recipe (e.g. two "Processors" steps at different pipeline positions).
        /// </summary>
        public RecipeStepConfig WithName(string name)
        {
            Name = name;
            return this;
        }

        public RecipeStepConfig Clone()
        {
            var clone = new RecipeStepConfig
            {
                StepType = StepType,
                Name = Name,
                Enabled = Enabled,
                Parameters = new Dictionary<string, object>(Parameters, StringComparer.OrdinalIgnoreCase)
            };

            // Deep clone nested step lists if present (e.g. for conditional blocks)
            if (Parameters.TryGetValue("ThenSteps", out var thenObj) && thenObj is List<RecipeStepConfig> thenList)
            {
                var newThen = new List<RecipeStepConfig>(thenList.Count);
                foreach (var s in thenList) newThen.Add(s.Clone());
                clone.Parameters["ThenSteps"] = newThen;
            }

            if (Parameters.TryGetValue("ElseSteps", out var elseObj) && elseObj is List<RecipeStepConfig> elseList)
            {
                var newElse = new List<RecipeStepConfig>(elseList.Count);
                foreach (var s in elseList) newElse.Add(s.Clone());
                clone.Parameters["ElseSteps"] = newElse;
            }

            return clone;
        }

        #region Factory Helpers

        public static RecipeStepConfig CreateSource(
            CaptureSourceType sourceType = CaptureSourceType.Region,
            bool? captureMouse = null,
            int? delayMs = null,
            ScreenCaptureMode? screenMode = null,
            WindowCaptureMode? windowMode = null,
            bool? alignDpi = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Source, $"Acquire {sourceType}");
            step.Set("SourceType", sourceType);
            if (captureMouse.HasValue) step.Set("CaptureMouseCursor", captureMouse.Value);
            if (delayMs.HasValue) step.Set("DelayMs", delayMs.Value);
            if (screenMode.HasValue) step.Set("ScreenCaptureMode", screenMode.Value);
            if (windowMode.HasValue) step.Set("WindowCaptureMode", windowMode.Value);
            if (alignDpi.HasValue) step.Set("AlignDpi", alignDpi.Value);
            return step;
        }

        public static RecipeStepConfig CreateSelection(CaptureMode mode = CaptureMode.Region, bool allowWindowSnapping = true)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.InteractiveSelection, $"Select {mode}");
            step.Set("SelectionMode", mode);
            step.Set("AllowWindowSnapping", allowWindowSnapping);
            return step;
        }

        public static RecipeStepConfig CreateBorder(int width = 2, string color = "#000000")
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Border, "Add Border");
            step.Set("Width", width);
            step.Set("Color", color ?? "#000000");
            return step;
        }

        public static RecipeStepConfig CreateEffect(string effect, Dictionary<string, object> parameters = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Effect, $"Apply {effect}");
            step.Set("Effect", effect);
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    step.Set(kvp.Key, kvp.Value);
                }
            }
            return step;
        }

        public static RecipeStepConfig CreateFeedback(bool? playSound = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.ImmediateFeedback, "Capture Feedback");
            if (playSound.HasValue) step.Set("PlaySound", playSound.Value);
            return step;
        }

        public static RecipeStepConfig CreateProcessors(IEnumerable<string> processorIds = null, ProcessorTiming? timing = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Processors, "Run Processors");
            if (processorIds != null)
            {
                step.Set("ProcessorIds", new List<string>(processorIds));
            }
            if (timing.HasValue)
            {
                step.Set("Timing", timing.Value.ToString());
            }
            return step;
        }

        public static RecipeStepConfig CreateDestinations(IEnumerable<string> destinationDesignations = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Destinations, "Export Destinations");
            if (destinationDesignations != null)
            {
                step.Set("DestinationDesignations", new List<string>(destinationDesignations));
            }
            return step;
        }

        public static RecipeStepConfig CreateNotification(bool? showNotification = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Notification, "Completion Notification");
            if (showNotification.HasValue) step.Set("ShowNotification", showNotification.Value);
            return step;
        }

        public static RecipeStepConfig CreateConditional(
            IStepCondition condition,
            IEnumerable<RecipeStepConfig> thenSteps = null,
            IEnumerable<RecipeStepConfig> elseSteps = null)
        {
            var step = new RecipeStepConfig(WellKnownStepTypes.Conditional, "Conditional Block");
            step.Set("Condition", condition);
            step.Set("ThenSteps", thenSteps != null ? new List<RecipeStepConfig>(thenSteps) : new List<RecipeStepConfig>());
            step.Set("ElseSteps", elseSteps != null ? new List<RecipeStepConfig>(elseSteps) : new List<RecipeStepConfig>());
            return step;
        }

        #endregion
    }
}
