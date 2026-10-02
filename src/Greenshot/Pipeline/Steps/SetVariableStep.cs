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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Contracts = Greenshot.Base.Pipeline.Contracts;

using Greenshot.Base.Recipes;
using log4net;
using Newtonsoft.Json.Linq;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step that evaluates expressions and assigns new or updated variable values
    /// into the flow context (context.Properties) for use by subsequent downstream nodes.
    /// </summary>
    [StepInfo(WellKnownStepTypes.SetVariable, "Set Variable", "Stores values in variables for the following steps.", "Logic")]
    [StepParameter("Variable", ContractDataType.String, Description = "Name of the variable to set")]
    [StepParameter("Value", ContractDataType.String, Description = "Value of the variable")]
    [StepParameter("Variables", ContractDataType.Object, Description = "Several variables: name to value")]
    [StepOutputVariable("{Parameter:Variable}", ContractDataType.String, "The variable named by the Variable parameter")]
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

            // Single variable setting: { "Variable": "MyVar", "Value": "${user.name}" }
            string singleVarName = NodeConfig.GetParameter<string>("Variable");

            if (!string.IsNullOrWhiteSpace(singleVarName))
            {
                object rawValue = NodeConfig.Parameters.TryGetValue("Value", out var v) ? v : null;

                object evaluated = EvaluateValue(rawValue, context);
                lock (context.Properties)
                {
                    context.Properties[singleVarName] = evaluated;
                }
                context.LogStep($"Set variable '{singleVarName}' = '{evaluated}'");
                Log.InfoFormat("SetVariableStep '{0}': context.Properties['{1}'] = '{2}'", Name, singleVarName, evaluated);
            }

            // Multiple variable settings: { "Variables": { "var1": "${payload.width}", "var2": "${user.name}" } }
            if (NodeConfig.Parameters.TryGetValue("Variables", out var varsObj) && varsObj != null)
            {
                Dictionary<string, object> dict = null;
                if (varsObj is Dictionary<string, object> d) dict = d;
                else if (varsObj is JObject jObj) dict = jObj.ToObject<Dictionary<string, object>>();

                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        object evaluated = EvaluateValue(kvp.Value, context);
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

        /// <summary>
        /// The engine already evaluated the expressions in the parameters (exactly once, see IEvaluatesOwnParameters);
        /// the value is used as it is, so ${...} text inside a variable's value stays text.
        /// </summary>
        private static object EvaluateValue(object rawValue, CaptureFlowContext context)
        {
            if (rawValue is JValue jValue)
            {
                return jValue.Value;
            }
            return rawValue;
        }
    }
}
