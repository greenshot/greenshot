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
using System.Reflection;

namespace Greenshot.Base.Pipeline.Contracts
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class StepInfoAttribute : Attribute
    {
        public string StepType { get; }
        public string DisplayName { get; }
        public string Description { get; set; }
        public string Category { get; set; }

        /// <summary>
        /// The step also reads parameters that are not declared (e.g. the properties of an annotation), so undeclared
        /// parameters are not reported.
        /// </summary>
        public bool AcceptsUndeclaredParameters { get; set; }

        public StepInfoAttribute(string stepType, string displayName = null, string description = null, string category = null)
        {
            StepType = stepType ?? throw new ArgumentNullException(nameof(stepType));
            DisplayName = displayName ?? stepType;
            Description = description;
            Category = category;
        }
    }

    /// <summary>
    /// Supplies the allowed values of a step parameter at runtime, see <see cref="StepParameterAttribute.AllowedValuesProvider"/>.
    /// The values are requested every time they are needed, so they reflect what is registered at that moment
    /// (e.g. file formats registered by a plugin after the step contract was built).
    /// </summary>
    public interface IAllowedValuesProvider
    {
        /// <summary>The allowed values; an empty list means every value is allowed.</summary>
        IReadOnlyList<string> GetAllowedValues();
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class StepParameterAttribute : Attribute
    {
        public string Name { get; }
        public ContractDataType DataType { get; }
        public bool Required { get; set; }
        public object DefaultValue { get; set; }
        public string Description { get; set; }
        public string[] AllowedValues { get; set; }

        /// <summary>
        /// A type implementing <see cref="IAllowedValuesProvider"/> (with a public parameterless constructor) that supplies the
        /// allowed values at runtime, for values that are not known at compile time (e.g. the registered file formats).
        /// Takes precedence over <see cref="AllowedValues"/>.
        /// </summary>
        public Type AllowedValuesProvider { get; set; }

        public bool SupportsExpressions { get; set; } = true;

        public StepParameterAttribute(string name, ContractDataType dataType = ContractDataType.String)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class StepInputVariableAttribute : Attribute
    {
        public string Name { get; }
        public ContractDataType DataType { get; }
        public bool Required { get; set; }
        public string Description { get; set; }
        public string ExampleValue { get; set; }

        public StepInputVariableAttribute(string name, ContractDataType dataType = ContractDataType.String)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class StepOutputVariableAttribute : Attribute
    {
        public string Name { get; }
        public ContractDataType DataType { get; }
        public string Description { get; set; }
        public string ExampleValue { get; set; }

        /// <summary>The step does not always set this output (e.g. only when something was found).</summary>
        public bool Conditional { get; set; }

        /// <summary>With Conditional: the output is always set when the node has this parameter.</summary>
        public string WhenParameter { get; set; }

        public StepOutputVariableAttribute(string name, ContractDataType dataType = ContractDataType.String, string description = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType;
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class StepPayloadAttribute : Attribute
    {
        public PayloadRequirement RawCapture { get; set; } = PayloadRequirement.None;
        public PayloadRequirement Surface { get; set; } = PayloadRequirement.None;
        public PayloadRequirement ExtractedText { get; set; } = PayloadRequirement.None;
        public PayloadEffect VisualMutation { get; set; } = PayloadEffect.None;
        public string[] ProducedMetadataKeys { get; set; }
    }

    /// <summary>
    /// Builds a StepContract by reflecting on a decorated ICaptureStep type.
    /// </summary>
    public static class StepContractBuilder
    {
        /// <param name="stepType">The step class.</param>
        /// <param name="stepTypeName">Overrides the step type of [StepInfo], for a class registered under several step types with different meaning (e.g. Border and Effect).</param>
        /// <param name="displayName">Overrides the display name.</param>
        /// <param name="description">Overrides the description.</param>
        public static StepContract FromType(Type stepType, string stepTypeName = null, string displayName = null, string description = null)
        {
            if (stepType == null) return null;

            var infoAttr = stepType.GetCustomAttribute<StepInfoAttribute>(true);
            stepTypeName ??= infoAttr?.StepType ?? stepType.Name;
            displayName ??= stepTypeName == infoAttr?.StepType ? infoAttr?.DisplayName ?? stepTypeName : stepTypeName;
            description ??= infoAttr?.Description ?? string.Empty;
            string category = infoAttr?.Category ?? "General";

            var paramAttrs = stepType.GetCustomAttributes<StepParameterAttribute>(true);
            var paramsList = new List<ParameterContract>();
            foreach (var p in paramAttrs)
            {
                var parameter = new ParameterContract(
                    p.Name,
                    p.DataType,
                    p.Required,
                    p.DefaultValue,
                    p.Description,
                    p.AllowedValues,
                    p.SupportsExpressions);
                if (p.AllowedValuesProvider != null)
                {
                    if (!typeof(IAllowedValuesProvider).IsAssignableFrom(p.AllowedValuesProvider))
                    {
                        throw new ArgumentException($"{stepType.Name}: AllowedValuesProvider {p.AllowedValuesProvider.Name} of parameter '{p.Name}' does not implement {nameof(IAllowedValuesProvider)}.");
                    }
                    parameter.AllowedValuesProvider = (IAllowedValuesProvider)Activator.CreateInstance(p.AllowedValuesProvider);
                }
                paramsList.Add(parameter);
            }

            var inVarAttrs = stepType.GetCustomAttributes<StepInputVariableAttribute>(true);
            var inVarsList = new List<VariableContract>();
            foreach (var v in inVarAttrs)
            {
                inVarsList.Add(new VariableContract(
                    v.Name,
                    v.DataType,
                    v.Required,
                    v.Description,
                    v.ExampleValue));
            }

            var outVarAttrs = stepType.GetCustomAttributes<StepOutputVariableAttribute>(true);
            var outVarsList = new List<VariableContract>();
            foreach (var v in outVarAttrs)
            {
                outVarsList.Add(new VariableContract(
                    v.Name,
                    v.DataType,
                    false,
                    v.Description,
                    v.ExampleValue)
                {
                    Conditional = v.Conditional,
                    WhenParameter = v.WhenParameter
                });
            }

            var payloadAttr = stepType.GetCustomAttribute<StepPayloadAttribute>(true);
            var payloadContract = payloadAttr != null
                ? new PayloadContract(
                    payloadAttr.RawCapture,
                    payloadAttr.Surface,
                    payloadAttr.ExtractedText,
                    payloadAttr.VisualMutation,
                    payloadAttr.ProducedMetadataKeys)
                : new PayloadContract();

            return new StepContract(
                stepTypeName,
                displayName,
                description,
                category,
                paramsList,
                inVarsList,
                outVarsList,
                payloadContract)
            {
                ImplementationType = stepType,
                AcceptsUndeclaredParameters = infoAttr?.AcceptsUndeclaredParameters ?? false
            };
        }
    }
}
