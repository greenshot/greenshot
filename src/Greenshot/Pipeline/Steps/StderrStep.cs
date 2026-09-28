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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Dedicated pipeline step that emits error text directly to stderr, sets a custom process exit code,
    /// and optionally aborts the capture pipeline immediately.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Stderr, "Stderr Output", "Emits an error message directly to stderr and sets a custom process exit code.", "Diagnostics")]
    [StepParameter("text", ContractDataType.String, Required = false, Description = "Error message expression to output to stderr")]
    [StepParameter("message", ContractDataType.String, Required = false, Description = "Alias for text parameter")]
    [StepParameter("exitCode", ContractDataType.Integer, Required = false, DefaultValue = 1, Description = "Numerical process exit code (default: 1)")]
    [StepParameter("abort", ContractDataType.Boolean, Required = false, DefaultValue = true, Description = "Whether to abort recipe execution immediately")]
    public class StderrStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StderrStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public StepContract Contract =>
            StepContractRegistry.GetContract(WellKnownStepTypes.Stderr) ?? StepContractBuilder.FromType(GetType());

        public StderrStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.Stderr;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) return;

            string rawText = NodeConfig.GetParameter<string>("Text")
                ?? NodeConfig.GetParameter<string>("text")
                ?? NodeConfig.GetParameter<string>("Message")
                ?? NodeConfig.GetParameter<string>("message")
                ?? NodeConfig.GetParameter<string>("Error")
                ?? NodeConfig.GetParameter<string>("error");

            string evaluatedText = string.Empty;
            // The engine already evaluated the expressions in the parameters; evaluating the result again would expand
            // ${...} text that came from data (e.g. OCR or barcode text). See IEvaluatesOwnParameters.
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                evaluatedText = rawText;
            }

            int exitCode = NodeConfig.GetParameter<int>("ExitCode", 1);
            if (exitCode == 1 && NodeConfig.Parameters != null && NodeConfig.Parameters.TryGetValue("exitcode", out var ecObj))
            {
                if (int.TryParse(ecObj?.ToString(), out int parsedEc))
                {
                    exitCode = parsedEc;
                }
            }

            bool abort = NodeConfig.GetParameter<bool>("Abort", true);
            if (abort && NodeConfig.Parameters != null && NodeConfig.Parameters.TryGetValue("abort", out var abObj))
            {
                if (bool.TryParse(abObj?.ToString(), out bool parsedAb))
                {
                    abort = parsedAb;
                }
            }

            context.ExitCode = exitCode;

            lock (context.Properties)
            {
                context.Properties["LastStderr"] = evaluatedText;
                context.Properties["StderrEmitted"] = true;
                context.Properties["ExitCode"] = exitCode;
            }

            context.LogStep($"[Stderr] (ExitCode={exitCode}) {evaluatedText}");
            Log.WarnFormat("StderrStep '{0}' emitted (ExitCode={1}): {2}", Name, exitCode, evaluatedText);

            if (context.StderrWriter != null && !string.IsNullOrEmpty(evaluatedText))
            {
                try
                {
                    await context.StderrWriter(evaluatedText).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warn("StderrStep: Failed to write to streaming StderrWriter", ex);
                }
            }

            if (abort)
            {
                context.Abort(string.IsNullOrEmpty(evaluatedText) ? $"Aborted by StderrStep with exit code {exitCode}" : evaluatedText);
            }
        }
    }
}
