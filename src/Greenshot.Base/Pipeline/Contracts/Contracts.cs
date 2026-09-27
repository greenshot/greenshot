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
    public class VariableContract
    {
        public string Name { get; set; }
        public ContractDataType DataType { get; set; } = ContractDataType.String;
        public bool Required { get; set; }
        public string Description { get; set; }
        public string ExampleValue { get; set; }

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
