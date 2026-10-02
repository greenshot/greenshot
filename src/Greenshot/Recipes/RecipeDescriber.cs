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
using System.Globalization;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using log4net;
using Newtonsoft.Json.Linq;

namespace Greenshot.Recipes
{
    /// <summary>
    /// A line of the description of a recipe
    /// </summary>
    public sealed class RecipeDescriptionLine
    {
        public RecipeDescriptionLine(string text, RecipeGateType? risk = null, bool isDetail = false)
        {
            Text = text;
            Risk = risk;
            IsDetail = isDetail;
        }

        public string Text { get; }

        /// <summary>
        /// The kind of gated action this line is about, null when it is harmless
        /// </summary>
        public RecipeGateType? Risk { get; }

        /// <summary>
        /// A detail of the line before it (e.g. what a step uploads)
        /// </summary>
        public bool IsDetail { get; }

        public override string ToString() => Text;
    }

    /// <summary>
    /// The description of a trigger: what starts the recipe and why that matters
    /// </summary>
    public sealed class RecipeTriggerDescription
    {
        /// <summary>
        /// The key of the approval, see <see cref="RecipeApprovalPolicy.GetTriggerKey"/>
        /// </summary>
        public string Key { get; set; }

        public string Label { get; set; }

        /// <summary>
        /// Why this trigger needs attention, null when it only adds something the user starts (a hotkey, a menu entry)
        /// </summary>
        public string Risk { get; set; }

        /// <summary>
        /// The trigger is disabled in the recipe itself
        /// </summary>
        public bool IsDisabled { get; set; }

        public override string ToString() => Risk == null ? Label : $"{Label}: {Risk}";
    }

    /// <summary>
    /// Describes what a recipe does in plain words, from the recipe itself: shown when the user approves a recipe, so what an AI
    /// tool or the recipe's author says about it can't hide what it does.
    /// </summary>
    public static class RecipeDescriber
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeDescriber));

        /// <summary>
        /// The triggers, including the browser invocation of a Commandline trigger as its own entry
        /// </summary>
        public static IReadOnlyList<RecipeTriggerDescription> DescribeTriggers(CaptureRecipe recipe)
        {
            var result = new List<RecipeTriggerDescription>();
            if (recipe?.Triggers == null)
            {
                return result;
            }
            for (int i = 0; i < recipe.Triggers.Count; i++)
            {
                var trigger = recipe.Triggers[i];
                if (trigger == null)
                {
                    continue;
                }
                var description = DescribeTrigger(recipe, trigger);
                description.Key = RecipeApprovalPolicy.GetTriggerKey(i, trigger);
                description.IsDisabled = !trigger.Enabled;
                result.Add(description);
                if (RecipeApprovalPolicy.AllowsBrowserInvocation(trigger))
                {
                    result.Add(new RecipeTriggerDescription
                    {
                        Key = RecipeApprovalPolicy.GetBrowserInvocationKey(i, trigger),
                        Label = $"Web pages and the browser extension can start \"{trigger.GetParameter<string>("Command") ?? recipe.Id}\"",
                        Risk = "Any web page you open can start this recipe (greenshot:// links), and the browser extension can too.",
                        IsDisabled = !trigger.Enabled
                    });
                }
            }
            return result;
        }

        private static RecipeTriggerDescription DescribeTrigger(CaptureRecipe recipe, TriggerConfig trigger)
        {
            string type = trigger.TriggerType ?? string.Empty;
            string menuText = trigger.GetParameter<string>("MenuItemText") ?? recipe.Name;
            if (Is(type, TriggerConfig.TypeHotkey))
            {
                return new RecipeTriggerDescription { Label = $"Hotkey {trigger.GetParameter("Hotkey", "(none)")}" };
            }
            if (Is(type, TriggerConfig.TypeContextMenu) || Is(type, TriggerConfig.TypeSystray))
            {
                return new RecipeTriggerDescription { Label = $"Entry \"{menuText}\" in the Greenshot menu" };
            }
            if (Is(type, TriggerConfig.TypeEditor))
            {
                return new RecipeTriggerDescription { Label = $"Entry \"{menuText}\" in the editor" };
            }
            if (Is(type, TriggerConfig.TypeManual))
            {
                return new RecipeTriggerDescription { Label = "Started from the recipe list" };
            }
            if (Is(type, TriggerConfig.TypeClipboard))
            {
                return new RecipeTriggerDescription
                {
                    Label = "When an image is copied to the clipboard",
                    Risk = "Runs on its own every time you copy an image, without you starting it."
                };
            }
            if (Is(type, TriggerConfig.TypeSchedule))
            {
                return new RecipeTriggerDescription
                {
                    Label = "On a schedule",
                    Risk = "Runs on its own at the scheduled times, also when you are not looking at the screen."
                };
            }
            if (Is(type, TriggerConfig.TypeCommandline))
            {
                return new RecipeTriggerDescription
                {
                    Label = $"Command line: greenshot.com run {trigger.GetParameter<string>("Command") ?? recipe.Id}",
                    Risk = "Programs and scripts on this PC can start it."
                };
            }
            if (Is(type, TriggerConfig.TypeOpenFile))
            {
                string filter = trigger.GetParameter<string>("Filter");
                return new RecipeTriggerDescription
                {
                    Label = string.IsNullOrWhiteSpace(filter) ? "When a file is opened with Greenshot" : $"When a file ({filter}) is opened with Greenshot",
                    Risk = "Runs for files opened with Greenshot, for example with \"Open with\" in Explorer."
                };
            }
            if (Is(type, TriggerConfig.TypeExtension))
            {
                string browser = trigger.GetParameter<string>("Browser");
                return new RecipeTriggerDescription
                {
                    Label = string.IsNullOrWhiteSpace(browser) ? "Captures from the browser extension" : $"Captures from the browser extension ({browser})",
                    Risk = "Handles the captures the Greenshot browser extension sends, instead of the default recipe."
                };
            }
            if (Is(type, TriggerConfig.TypeAiTool))
            {
                return new RecipeTriggerDescription
                {
                    Label = $"AI tool \"{trigger.GetParameter<string>("ToolName") ?? trigger.Name}\"",
                    Risk = "AI tools you allowed (e.g. Claude, Copilot, Gemini) can run this recipe and get its result, the image included."
                };
            }
            return new RecipeTriggerDescription { Label = $"{type} trigger", Risk = "An unknown kind of trigger." };
        }

        /// <summary>
        /// The steps in plain words, each followed by the gated actions it does
        /// </summary>
        public static IReadOnlyList<RecipeDescriptionLine> DescribeSteps(CaptureRecipe recipe, IStepRegistry registry = null)
        {
            var lines = new List<RecipeDescriptionLine>();
            if (recipe?.Nodes == null)
            {
                return lines;
            }
            registry ??= StepRegistry.Instance;
            foreach (var node in recipe.Nodes.Where(n => n != null))
            {
                var gatedActions = GetGatedActions(node, registry);
                var mainRisk = gatedActions.Count > 0 ? gatedActions[0].GateType : (RecipeGateType?)null;
                string text = DescribeStep(node, registry);
                if (!node.Enabled)
                {
                    text += " (disabled)";
                }
                lines.Add(new RecipeDescriptionLine(text, mainRisk));
                foreach (var action in gatedActions)
                {
                    lines.Add(new RecipeDescriptionLine(DescribeGatedAction(action), action.GateType, true));
                }
            }
            return lines;
        }

        /// <summary>
        /// One line per gated action, for messages
        /// </summary>
        public static string DescribeGatedAction(RecipeGatedAction action)
        {
            string target = string.IsNullOrWhiteSpace(action?.Target) ? string.Empty : action.Target;
            return action?.GateType switch
            {
                RecipeGateType.NetworkAccess => $"Uploads to {target}",
                RecipeGateType.ExternalCommand => $"Runs the program {target}",
                RecipeGateType.FileSystemAccess => target,
                _ => target
            };
        }

        /// <summary>
        /// The differences between two versions of a recipe in plain words: the description lines and triggers that were added or removed.
        /// </summary>
        public static IReadOnlyList<string> DescribeChanges(CaptureRecipe oldRecipe, CaptureRecipe newRecipe, IStepRegistry registry = null)
        {
            var oldLines = Summarize(oldRecipe, registry);
            var newLines = Summarize(newRecipe, registry);
            var changes = new List<string>();
            changes.AddRange(newLines.Where(l => !oldLines.Contains(l, StringComparer.Ordinal)).Select(l => $"Adds: {l}"));
            changes.AddRange(oldLines.Where(l => !newLines.Contains(l, StringComparer.Ordinal)).Select(l => $"Removes: {l}"));
            return changes;
        }

        private static List<string> Summarize(CaptureRecipe recipe, IStepRegistry registry)
        {
            var lines = new List<string>();
            if (recipe == null)
            {
                return lines;
            }
            lines.AddRange(DescribeTriggers(recipe).Select(t => $"trigger {t.Label}"));
            lines.AddRange(DescribeSteps(recipe, registry).Select(l => l.Text));
            return lines.Distinct(StringComparer.Ordinal).ToList();
        }

        private static List<RecipeGatedAction> GetGatedActions(RecipeNodeConfig node, IStepRegistry registry)
        {
            var actions = new List<RecipeGatedAction>();
            try
            {
                if (registry.CreateStep(node) is IRequiresRecipeAuthorization authorization)
                {
                    foreach (var action in authorization.GetGatedActions() ?? Enumerable.Empty<RecipeGatedAction>())
                    {
                        if (action != null && !actions.Contains(action))
                        {
                            actions.Add(action);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not check the step '{node.Id}' ({node.StepType}) for gated actions.", ex);
            }
            return actions;
        }

        private static string DescribeStep(RecipeNodeConfig node, IStepRegistry registry)
        {
            string type = node.StepType ?? string.Empty;
            if (Is(type, WellKnownStepTypes.Source))
            {
                return DescribeSource(node);
            }
            if (Is(type, WellKnownStepTypes.Destinations) || Is(type, WellKnownStepTypes.CustomDestination))
            {
                var designations = GetStrings(node, "DestinationDesignations");
                string custom = node.GetParameter<string>("CustomDestinationId");
                if (!string.IsNullOrWhiteSpace(custom))
                {
                    designations.Add(custom);
                }
                return designations.Count == 0
                    ? "Sends the result to the destinations from the settings"
                    : $"Sends the result to {string.Join(", ", designations.Select(GetDestinationName))}";
            }
            if (Is(type, WellKnownStepTypes.Processors))
            {
                var processors = GetStrings(node, "ProcessorIds");
                return processors.Count == 0 ? "Runs the processors from the settings" : $"Runs {string.Join(", ", processors.Select(GetProcessorName))}";
            }
            if (Is(type, WellKnownStepTypes.Conditional))
            {
                return "Decides how to continue";
            }
            if (Is(type, WellKnownStepTypes.SetVariable))
            {
                return $"Sets the variable {node.GetParameter("Variable", "?")}";
            }
            if (Is(type, WellKnownStepTypes.Clipboard))
            {
                return "Copies the result to the clipboard";
            }
            if (Is(type, WellKnownStepTypes.Editor))
            {
                return "Opens the result in the editor";
            }
            if (Is(type, WellKnownStepTypes.Printer))
            {
                return "Prints the result";
            }
            if (Is(type, WellKnownStepTypes.Email))
            {
                return "Creates an email with the result";
            }
            if (Is(type, WellKnownStepTypes.SaveFile))
            {
                string directory = node.GetParameter<string>("SaveDirectory");
                return string.IsNullOrWhiteSpace(directory) ? "Saves the result to the output folder from the settings" : $"Saves the result to {directory}";
            }
            if (Is(type, WellKnownStepTypes.InteractiveSelection))
            {
                return "Lets you select a region";
            }
            if (Is(type, WellKnownStepTypes.UserPrompt))
            {
                return "Asks you a question";
            }
            if (Is(type, WellKnownStepTypes.Stdout) || Is(type, WellKnownStepTypes.Stderr))
            {
                return "Writes text for the caller (command line or AI tool)";
            }
            if (Is(type, WellKnownStepTypes.DynamicDestination))
            {
                return "Lets you choose where the result goes";
            }

            var contract = registry.GetContract(type);
            string name = string.IsNullOrWhiteSpace(contract?.DisplayName) ? type : contract.DisplayName;
            string parameters = SummarizeParameters(node, contract);
            return string.IsNullOrEmpty(parameters) ? name : $"{name} ({parameters})";
        }

        private static string DescribeSource(RecipeNodeConfig node)
        {
            string sourceType = node.GetParameter("SourceType", "Region");
            string what;
            switch (sourceType.ToLowerInvariant())
            {
                case "region":
                    what = node.HasParameter("PreSuppliedRegion") ? "Captures a given part of the screen" : "Captures a region you select";
                    break;
                case "window":
                    string title = node.GetParameter<string>("WindowTitle") ?? node.GetParameter<string>("WindowTitlePattern");
                    string process = node.GetParameter<string>("ProcessName");
                    what = node.HasParameter("WindowHandle") ? "Captures the window passed to the recipe"
                        : !string.IsNullOrWhiteSpace(title) ? $"Captures the window \"{title}\""
                        : !string.IsNullOrWhiteSpace(process) ? $"Captures the window of {process}"
                        : "Captures a window you choose";
                    break;
                case "activewindow":
                    what = "Captures the active window";
                    break;
                case "fullscreen":
                    what = "Captures the screen";
                    break;
                case "clipboard":
                    what = "Takes the image from the clipboard";
                    break;
                case "file":
                    string filename = node.GetParameter<string>("Filename");
                    what = string.IsNullOrWhiteSpace(filename) ? "Loads an image file" : $"Loads the image file {filename}";
                    break;
                case "currenteditor":
                    what = "Takes the image from the open editor";
                    break;
                case "lastregion":
                    what = "Captures the last region again";
                    break;
                default:
                    what = $"Captures ({sourceType})";
                    break;
            }
            int delay = node.GetParameter("DelayMs", 0);
            return delay > 0 ? $"{what}, after {delay.ToString(CultureInfo.InvariantCulture)} ms" : what;
        }

        private static string SummarizeParameters(RecipeNodeConfig node, Base.Pipeline.Contracts.StepContract contract)
        {
            if (node.Parameters == null || node.Parameters.Count == 0)
            {
                return string.Empty;
            }
            var parts = new List<string>();
            foreach (var pair in node.Parameters)
            {
                string value = FormatValue(pair.Value);
                if (value == null || value.Length > 60)
                {
                    continue;
                }
                var parameter = contract?.FindParameter(pair.Key);
                if (parameter?.DefaultValue != null && string.Equals(FormatValue(parameter.DefaultValue), value, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                parts.Add($"{pair.Key}: {value}");
                if (parts.Count == 4)
                {
                    break;
                }
            }
            return string.Join(", ", parts);
        }

        private static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string text:
                    return text;
                case bool flag:
                    return flag ? "yes" : "no";
                case JValue jValue:
                    return FormatValue(jValue.Value);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return null;
            }
        }

        private static List<string> GetStrings(RecipeNodeConfig node, string key)
        {
            var result = new List<string>();
            if (node.Parameters == null || !node.Parameters.TryGetValue(key, out var raw) || raw == null)
            {
                return result;
            }
            if (raw is string single)
            {
                result.AddRange(single.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            else if (raw is IEnumerable values)
            {
                foreach (var value in values)
                {
                    string text = FormatValue(value);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add(text.Trim());
                    }
                }
            }
            return result;
        }

        private static string GetDestinationName(string designation)
        {
            try
            {
                string name = DestinationHelper.GetDestination(designation)?.Descriptor?.DisplayName;
                if (!string.IsNullOrWhiteSpace(name) && !string.Equals(name, designation, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{name} ({designation})";
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not look up the destination '{designation}'.", ex);
            }
            return designation;
        }

        private static string GetProcessorName(string processorId)
        {
            try
            {
                var processor = SimpleServiceProvider.Current?.GetAllInstances<IProcessor>()
                    .FirstOrDefault(p => string.Equals(p.Designation, processorId, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(p.GetType().Name, processorId, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(processor?.Description))
                {
                    return processor.Description;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not look up the processor '{processorId}'.", ex);
            }
            return processorId.EndsWith("Processor", StringComparison.Ordinal) ? processorId.Substring(0, processorId.Length - "Processor".Length) : processorId;
        }

        private static bool Is(string value, string expected) => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}
