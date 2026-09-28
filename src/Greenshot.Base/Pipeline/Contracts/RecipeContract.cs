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
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;

namespace Greenshot.Base.Pipeline.Contracts
{
    public class StepSummaryContract
    {
        public string NodeId { get; set; }
        public string StepType { get; set; }
        public string DisplayName { get; set; }
        public IReadOnlyList<string> RequiredInputs { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ProducedOutputs { get; set; } = Array.Empty<string>();
        public PayloadRequirement RequiresRawCapture { get; set; }
        public PayloadRequirement ProducesRawCapture { get; set; }
    }

    /// <summary>
    /// Composite contract of an entire CaptureRecipe computed by analyzing its triggers and DAG steps.
    /// </summary>
    public class RecipeContract
    {
        public string RecipeId { get; set; }
        public string RecipeName { get; set; }
        public string Description { get; set; }

        public IReadOnlyList<VariableContract> Inputs { get; set; } = Array.Empty<VariableContract>();
        public IReadOnlyList<VariableContract> Outputs { get; set; } = Array.Empty<VariableContract>();
        public IReadOnlyList<StepSummaryContract> Steps { get; set; } = Array.Empty<StepSummaryContract>();
        public IReadOnlyList<string> ValidationWarnings { get; set; } = Array.Empty<string>();

        public bool AcquiresImage { get; set; }
        public bool MutatesPixels { get; set; }
        public bool ExtractsText { get; set; }

        public static RecipeContract Analyze(CaptureRecipe recipe)
        {
            if (recipe == null) return null;

            var result = new RecipeContract
            {
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                Description = recipe.Description ?? string.Empty
            };

            var inputsMap = new Dictionary<string, VariableContract>(StringComparer.OrdinalIgnoreCase);
            var outputsMap = new Dictionary<string, VariableContract>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            var stepSummaries = new List<StepSummaryContract>();

            // 1. Incorporate CommandlineTrigger arguments as explicit inputs
            if (recipe.Triggers != null)
            {
                foreach (var trig in recipe.Triggers.Where(t => t.Enabled && string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase)))
                {
                    var declaredArgs = trig.GetParameter<List<CommandlineArgument>>("Arguments");
                    if (declaredArgs != null)
                    {
                        foreach (var arg in declaredArgs)
                        {
                            if (string.IsNullOrWhiteSpace(arg.Name)) continue;
                            string varName = !string.IsNullOrWhiteSpace(arg.Variable) ? arg.Variable : arg.Name;
                            inputsMap[varName] = new VariableContract(
                                varName,
                                arg.Type,
                                arg.Required,
                                arg.Description ?? $"Command-line argument --{arg.Name}",
                                arg.DefaultValue);
                        }
                    }
                }
            }

            bool hasRawCapture = false;

            // 2. Traverse recipe nodes in DAG execution order (or sequence if linear)
            if (recipe.Nodes != null && recipe.Nodes.Count > 0)
            {
                foreach (var node in recipe.Nodes)
                {
                    var contract = StepContractRegistry.GetContract(node.StepType);
                    var summary = new StepSummaryContract
                    {
                        NodeId = node.Id,
                        StepType = node.StepType,
                        DisplayName = contract?.DisplayName ?? node.StepType
                    };

                    if (contract != null)
                    {
                        summary.RequiresRawCapture = contract.PayloadContract.RawCapture;
                        summary.ProducesRawCapture = contract.PayloadContract.RawCapture == PayloadRequirement.Created ? PayloadRequirement.Created : PayloadRequirement.None;

                        if (contract.PayloadContract.RawCapture == PayloadRequirement.Created)
                        {
                            hasRawCapture = true;
                            result.AcquiresImage = true;
                        }

                        if (contract.PayloadContract.RawCapture == PayloadRequirement.Required && !hasRawCapture)
                        {
                            warnings.Add($"Step '{node.Id}' ({node.StepType}) requires an image, but no preceding node acquires one.");
                        }

                        if (contract.PayloadContract.VisualMutation == PayloadEffect.MutatesPixels)
                        {
                            result.MutatesPixels = true;
                        }

                        if (contract.PayloadContract.ExtractedText == PayloadRequirement.Created)
                        {
                            result.ExtractsText = true;
                        }

                        var reqInputs = new List<string>();
                        foreach (var iv in contract.InputVariables)
                        {
                            reqInputs.Add(iv.Name);
                            // If input is required and not yet produced by an earlier step, it becomes a recipe-level input
                            if (iv.Required && !outputsMap.ContainsKey(iv.Name) && !inputsMap.ContainsKey(iv.Name))
                            {
                                inputsMap[iv.Name] = iv;
                            }
                        }
                        summary.RequiredInputs = reqInputs;

                        var prodOutputs = new List<string>();
                        foreach (var ov in contract.OutputVariables)
                        {
                            prodOutputs.Add(ov.Name);
                            outputsMap[ov.Name] = ov;
                        }
                        summary.ProducedOutputs = prodOutputs;
                    }

                    stepSummaries.Add(summary);
                }
            }

            // Standard payload output properties available if an image is acquired
            if (result.AcquiresImage || hasRawCapture)
            {
                outputsMap["Payload.Width"] = new VariableContract("Payload.Width", ContractDataType.Integer, false, "Image width in pixels");
                outputsMap["Payload.Height"] = new VariableContract("Payload.Height", ContractDataType.Integer, false, "Image height in pixels");
            }

            if (result.ExtractsText)
            {
                outputsMap["Payload.ExtractedText"] = new VariableContract("Payload.ExtractedText", ContractDataType.String, false, "Text extracted via OCR or text recognition");
            }

            result.Inputs = inputsMap.Values.ToList().AsReadOnly();
            result.Outputs = outputsMap.Values.ToList().AsReadOnly();
            result.Steps = stepSummaries.AsReadOnly();
            result.ValidationWarnings = warnings.AsReadOnly();

            return result;
        }
    }
}
