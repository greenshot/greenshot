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


using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Expressions;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Recipes.Expressions
{
    /// <summary>
    /// Everything a recipe can use in ${...}: the options and variables of this recipe (from its options, triggers and the
    /// contracts of its steps), the values of the engine and the functions. One list for the help of the recipe editor.
    /// </summary>
    public static class RecipeExpressionHelp
    {
        private static readonly Dictionary<string, string> ErrorDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LastError"] = "The message of the error, after an error transition",
            ["LastErrorType"] = "The type of the error, after an error transition",
            ["FailedNodeId"] = "The node which failed, after an error transition"
        };

        public static IReadOnlyList<ExpressionHelpEntry> For(CaptureRecipe recipe, IStepRegistry registry = null)
        {
            var entries = new List<ExpressionHelpEntry>();
            if (recipe != null)
            {
                foreach (var option in (recipe.Options ?? new List<RecipeOption>()).Where(o => !string.IsNullOrWhiteSpace(o?.Key)).OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
                {
                    string label = string.IsNullOrWhiteSpace(option.Label) ? option.Description : option.Label;
                    entries.Add(new ExpressionHelpEntry(ExpressionHelpGroup.RecipeOption, "${option." + option.Key + "}", $"{label} ({option.Type})".Trim()));
                }

                var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var input in RecipeContract.GetTriggerInputs(recipe, out _))
                {
                    variables[input.Name] = input.Description;
                }
                var contract = RecipeContract.Analyze(recipe, registry);
                foreach (var output in contract?.Outputs ?? Array.Empty<VariableContract>())
                {
                    if (!variables.ContainsKey(output.Name)) variables[output.Name] = output.Description;
                }
                // A node whose step has no contract (a plugin which isn't loaded) still sets what SetVariable names
                foreach (var node in recipe.Nodes ?? new List<RecipeNodeConfig>())
                {
                    var set = node?.Parameters != null && node.Parameters.ContainsKey("Variables") ? node.GetParameter<Dictionary<string, object>>("Variables") : null;
                    if (set != null)
                    {
                        foreach (var key in set.Keys.Where(k => !variables.ContainsKey(k)))
                        {
                            variables[key] = $"Set by node '{node.Id}'";
                        }
                    }
                }
                if (recipe.Flow?.Transitions?.Any(t => t?.IsError == true) == true)
                {
                    foreach (var error in RecipeContract.ErrorVariables.Where(e => !variables.ContainsKey(e)))
                    {
                        variables[error] = ErrorDescriptions.TryGetValue(error, out string text) ? text : string.Empty;
                    }
                }
                foreach (var pair in variables.Where(p => p.Key != RecipeContract.PayloadVariable).OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    entries.Add(new ExpressionHelpEntry(ExpressionHelpGroup.RecipeVariable, "${var." + pair.Key + "}", pair.Value));
                }
            }

            entries.AddRange(ExpressionHelp.For(TemplatePurpose.Value));
            return entries;
        }
    }
}
