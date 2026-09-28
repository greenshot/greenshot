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
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Base.Pipeline.Contracts
{
    /// <summary>
    /// Contract describing a parameter in RecipeNodeConfig.Parameters.
    /// </summary>
    public class ParameterContract
    {
        public string Name { get; set; }
        public ContractDataType DataType { get; set; } = ContractDataType.String;
        public bool Required { get; set; }
        public object DefaultValue { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<string> AllowedValues { get; set; } = Array.Empty<string>();
        public bool SupportsExpressions { get; set; } = true;

        /// <summary>True when this is the parameter with the given name (parameter names are case-insensitive).</summary>
        public bool Matches(string parameterName) => string.Equals(Name, parameterName, StringComparison.OrdinalIgnoreCase);

        public ParameterContract() { }

        public ParameterContract(
            string name,
            ContractDataType dataType = ContractDataType.String,
            bool required = false,
            object defaultValue = null,
            string description = null,
            IReadOnlyList<string> allowedValues = null,
            bool supportsExpressions = true)
        {
            Name = name;
            DataType = dataType;
            Required = required;
            DefaultValue = defaultValue;
            Description = description;
            AllowedValues = allowedValues ?? Array.Empty<string>();
            SupportsExpressions = supportsExpressions;
        }
    }

    /// <summary>
    /// Contract describing an input or output variable in CaptureFlowContext.Properties.
    /// </summary>
    /// <remarks>
    /// A name can contain placeholders that depend on the node, see <see cref="ResolveNames"/>:
    /// <c>{NodeId}</c> is the node's id, <c>{Parameter:A}</c> the value of parameter A,
    /// and a name that is only <c>{ParameterKeys:X}</c> stands for every key of the dictionary parameter X.
    /// </remarks>
    public class VariableContract
    {
        public string Name { get; set; }
        public ContractDataType DataType { get; set; } = ContractDataType.String;
        public bool Required { get; set; }
        public string Description { get; set; }
        public string ExampleValue { get; set; }

        /// <summary>
        /// Outputs only: the step does not always set it (e.g. only when a barcode was found), so a missing value is not a contract violation.
        /// </summary>
        public bool Conditional { get; set; }

        /// <summary>
        /// Outputs only: a conditional output that is always set when the node has this parameter (e.g. Destination.Filename with SaveDirectory).
        /// </summary>
        public string WhenParameter { get; set; }

        /// <summary>True when the output is always set by this node: not conditional, or its <see cref="WhenParameter"/> is set.</summary>
        public bool IsGuaranteedFor(Recipes.RecipeNodeConfig node)
        {
            if (!Conditional) return true;
            if (string.IsNullOrEmpty(WhenParameter) || node == null) return false;
            object value = node.GetParameter<object>(WhenParameter);
            return value != null && !(value is string text && string.IsNullOrWhiteSpace(text));
        }

        /// <summary>
        /// The variable name(s) for a concrete node, with the placeholders replaced.
        /// Empty when a parameter placeholder cannot be resolved (the parameter is not set).
        /// </summary>
        public IReadOnlyList<string> ResolveNames(Recipes.RecipeNodeConfig node)
        {
            if (string.IsNullOrEmpty(Name)) return Array.Empty<string>();
            if (Name.IndexOf('{') < 0) return new[] { Name };

            string result = Name.Replace("{NodeId}", node?.Id ?? string.Empty);

            const string keysPrefix = "{ParameterKeys:";
            if (result.StartsWith(keysPrefix, StringComparison.Ordinal) && result.EndsWith("}", StringComparison.Ordinal))
            {
                object value = node?.GetParameter<object>(result.Substring(keysPrefix.Length, result.Length - keysPrefix.Length - 1));
                var keys = new List<string>();
                if (value is Newtonsoft.Json.Linq.JObject jObject)
                {
                    keys.AddRange(jObject.Properties().Select(p => p.Name));
                }
                else if (value is System.Collections.IDictionary dictionary)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        if (key != null) keys.Add(key.ToString());
                    }
                }
                return keys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
            }

            const string parameterPrefix = "{Parameter:";
            int start;
            while ((start = result.IndexOf(parameterPrefix, StringComparison.Ordinal)) >= 0)
            {
                int end = result.IndexOf('}', start);
                if (end < 0) return Array.Empty<string>();
                string parameterName = result.Substring(start + parameterPrefix.Length, end - start - parameterPrefix.Length);
                string value = node?.GetParameter<object>(parameterName)?.ToString();
                if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
                result = result.Substring(0, start) + value + result.Substring(end + 1);
            }
            return new[] { result };
        }

        /// <summary>True when the name depends on the node (contains placeholders).</summary>
        public bool IsNodeSpecific => Name != null && Name.IndexOf('{') >= 0;

        public VariableContract() { }

        public VariableContract(
            string name,
            ContractDataType dataType = ContractDataType.String,
            bool required = false,
            string description = null,
            string exampleValue = null)
        {
            Name = name;
            DataType = dataType;
            Required = required;
            Description = description;
            ExampleValue = exampleValue;
        }
    }

    /// <summary>
    /// Contract describing a step's requirements and modifications to the visual capture payload.
    /// </summary>
    public class PayloadContract
    {
        public PayloadRequirement RawCapture { get; set; } = PayloadRequirement.None;
        public PayloadRequirement Surface { get; set; } = PayloadRequirement.None;
        public PayloadRequirement ExtractedText { get; set; } = PayloadRequirement.None;
        public PayloadEffect VisualMutation { get; set; } = PayloadEffect.None;
        public IReadOnlyList<string> ProducedMetadataKeys { get; set; } = Array.Empty<string>();

        public PayloadContract() { }

        public PayloadContract(
            PayloadRequirement rawCapture = PayloadRequirement.None,
            PayloadRequirement surface = PayloadRequirement.None,
            PayloadRequirement extractedText = PayloadRequirement.None,
            PayloadEffect visualMutation = PayloadEffect.None,
            IReadOnlyList<string> producedMetadataKeys = null)
        {
            RawCapture = rawCapture;
            Surface = surface;
            ExtractedText = extractedText;
            VisualMutation = visualMutation;
            ProducedMetadataKeys = producedMetadataKeys ?? Array.Empty<string>();
        }
    }
}
