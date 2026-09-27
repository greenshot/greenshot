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
    /// Complete contract of a pipeline step disclosing its configuration parameters,
    /// expected input variables, produced output variables, and payload interactions.
    /// </summary>
    public class StepContract
    {
        public string StepType { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public IReadOnlyList<ParameterContract> Parameters { get; set; } = Array.Empty<ParameterContract>();
        public IReadOnlyList<VariableContract> InputVariables { get; set; } = Array.Empty<VariableContract>();
        public IReadOnlyList<VariableContract> OutputVariables { get; set; } = Array.Empty<VariableContract>();
        public PayloadContract PayloadContract { get; set; } = new PayloadContract();

        public StepContract() { }

        public StepContract(
            string stepType,
            string displayName = null,
            string description = null,
            string category = null,
            IReadOnlyList<ParameterContract> parameters = null,
            IReadOnlyList<VariableContract> inputVariables = null,
            IReadOnlyList<VariableContract> outputVariables = null,
            PayloadContract payloadContract = null)
        {
            StepType = stepType;
            DisplayName = displayName ?? stepType;
            Description = description ?? string.Empty;
            Category = category ?? "General";
            Parameters = parameters ?? Array.Empty<ParameterContract>();
            InputVariables = inputVariables ?? Array.Empty<VariableContract>();
            OutputVariables = outputVariables ?? Array.Empty<VariableContract>();
            PayloadContract = payloadContract ?? new PayloadContract();
        }
    }
}
