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
using Contracts = Greenshot.Base.Recipes.Contracts;

using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Dedicated pipeline step that emits text directly and immediately to stdout.
    /// If executed in an IPC context (e.g. CLI proxy), sends a streaming response packet
    /// back through the IPC stream immediately without waiting for the full pipeline to finish.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Stdout, "Stdout Output", "Writes text to standard output of the caller (e.g. greenshot-cli.exe).", "Diagnostics")]
    [StepPayload(ExtractedText = PayloadRequirement.Optional)]
    [StepParameter("Text", ContractDataType.String, Description = "Text to write (default: the extracted text)")]
    [StepOutputVariable("LastStdout", ContractDataType.String, "The text that was written")]
    [StepOutputVariable("StdoutEmitted", ContractDataType.Boolean, "True once text was written")]
    public class StdoutStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StdoutStep));

        public string Name { get; }
        public RecipeNodeConfig NodeConfig { get; }

        public StdoutStep(RecipeNodeConfig config)
        {
            NodeConfig = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.Stdout;
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (context == null) return;

            string rawText = NodeConfig.GetParameter<string>("Text");

            string evaluatedText;
            // The engine already evaluated the expressions in the parameters; evaluating the result again would expand
            // ${...} text that came from data (e.g. OCR or barcode text). See IEvaluatesOwnParameters.
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                evaluatedText = rawText;
            }
            else
            {
                // Fallback to extracted text payload if available
                evaluatedText = context.Payload?.ExtractedText ?? string.Empty;
            }

            // Record into context properties for downstream inspectability
            lock (context.Properties)
            {
                context.Properties["LastStdout"] = evaluatedText;
                context.Properties["StdoutEmitted"] = true;
            }

            context.LogStep($"[Stdout] {evaluatedText}");
            Log.InfoFormat("StdoutStep '{0}' emitted: {1}", Name, evaluatedText);

            // Stream immediately over IPC if a writer is attached
            if (context.StdoutWriter != null)
            {
                try
                {
                    await context.StdoutWriter(evaluatedText).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warn("StdoutStep: Failed to write to streaming StdoutWriter", ex);
                }
            }
        }
    }
}
