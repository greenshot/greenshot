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
using System.Threading;
using System.Threading.Tasks;

using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step that evaluates expressions and assigns new or updated variable values
    /// into the flow context (context.Properties) for use by subsequent downstream nodes.
    /// </summary>
    [StepInfo(WellKnownStepTypes.SetVariable, "Set Variable", "Stores values in variables for the following steps.", "Logic")]
    [StepParameter("Variables", ContractDataType.Object, ValueType = typeof(Dictionary<string, object>), Required = true, Description = "The variables to set: name to value")]
    [StepOutputVariable("{ParameterKeys:Variables}", ContractDataType.String, "The variables of the Variables parameter")]
    public class SetVariableStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SetVariableStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public SetVariableStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.SetVariable;
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null || NodeConfig.Parameters == null) return Task.CompletedTask;

            // { "Variables": { "var1": "${payload.width}", "var2": "${user.name}" } }
            // The engine already evaluated the expressions in the values (exactly once, see IEvaluatesOwnParameters);
            // a value is used as it is, so ${...} text inside a variable's value stays text.
            if (NodeConfig.Parameters.TryGetValue("Variables", out var varsObj) && varsObj != null)
            {
                if (varsObj is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        object evaluated = kvp.Value;
                        lock (context.Properties)
                        {
                            context.Properties[kvp.Key] = evaluated;
                        }
                        context.LogStep($"Set variable '{kvp.Key}' = '{evaluated}'");
                        Log.InfoFormat("SetVariableStep '{0}': context.Properties['{1}'] = '{2}'", Name, kvp.Key, evaluated);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
