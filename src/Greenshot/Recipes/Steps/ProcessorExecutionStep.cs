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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Contracts = Greenshot.Base.Pipeline.Contracts;

using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Pipeline step executing image processors (OCR, TitleFix, or plugin processors).
    /// </summary>
    [StepInfo(WellKnownStepTypes.Processors, "Processors", "Runs image processors (e.g. OCR, title fix, plugin processors).", "Processing")]
    [StepPayload(RawCapture = PayloadRequirement.Required, Surface = PayloadRequirement.Optional, ExtractedText = PayloadRequirement.Created)]
    [StepParameter("ProcessorIds", ContractDataType.Object, Description = "Only run these processors (type name, description or designation)")]
    [StepParameter("ProcessorMode", ContractDataType.String, Description = "OCR runs OCR and stores the text")]
    [StepParameter("Timing", ContractDataType.Enum, Description = "Run the processors that belong before or after the selection", DefaultValue = "Any", AllowedValues = new[] { "Any", "PreSelection", "PostSelection" })]
    [StepParameter("RunOcr", ContractDataType.Boolean, DefaultValue = true, Description = "Run OCR processors")]
    [StepParameter("RunTitleFix", ContractDataType.Boolean, DefaultValue = true, Description = "Run the title fix processor")]
    [StepParameter("RunPlugins", ContractDataType.Boolean, DefaultValue = true, Description = "Run plugin processors")]
    [StepOutputVariable("OcrText", ContractDataType.String, "Text found by OCR (also in Payload.ExtractedText)", Conditional = true)]
    [StepOutputVariable("Text", ContractDataType.String, "Same as OcrText", Conditional = true)]
    [StepOutputVariable("CommandResult", ContractDataType.String, "Same as OcrText", Conditional = true)]
    public class ProcessorExecutionStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ProcessorExecutionStep));

        public string Name { get; }
        public RecipeNodeConfig Config { get; }

        public ProcessorExecutionStep(RecipeNodeConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ProcessorExecutionStep";
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null)
            {
                context.LogStep("ProcessorExecutionStep skipped: Payload or RawCapture is null.");
                return;
            }

            context.State = CaptureFlowState.Processing;

            var processors = SimpleServiceProvider.Current.GetAllInstances<IProcessor>()
                .Where(p => p.isActive)
                .ToList();

            // Timing filter: PreSelection / PostSelection only run the processors that declare that PreferredTiming,
            // Any (the default) runs all active processors.
            var timingParam = Config.GetParameter<string>("Timing");
            if (!string.IsNullOrEmpty(timingParam) &&
                !string.Equals(timingParam, "Any", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<ProcessorTiming>(timingParam, ignoreCase: true, out var requestedTiming))
            {
                processors = processors
                    .OfType<AbstractProcessor>()
                    .Where(p => p.PreferredTiming == requestedTiming)
                    .Cast<IProcessor>()
                    .ToList();
            }

            string mode = Config.GetParameter<string>("ProcessorMode");
            if (string.Equals(mode, "OCR", StringComparison.OrdinalIgnoreCase))
            {
                processors = processors
                    .Where(p => p.GetType().Name.IndexOf("Ocr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                p.Description.IndexOf("Ocr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                (p.Designation != null && p.Designation.IndexOf("Ocr", StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }
            else if (string.Equals(mode, "Selected", StringComparison.OrdinalIgnoreCase))
            {
                bool runOcr = Config.GetParameter<bool?>("RunOcr") ?? true;
                bool runTitleFix = Config.GetParameter<bool?>("RunTitleFix") ?? true;
                bool runPlugins = Config.GetParameter<bool?>("RunPlugins") ?? true;

                processors = processors.Where(p =>
                {
                    bool isOcr = p.GetType().Name.IndexOf("Ocr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Description.IndexOf("Ocr", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isTitleFix = p.GetType().Name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      p.Description.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isOcr) return runOcr;
                    if (isTitleFix) return runTitleFix;
                    return runPlugins;
                }).ToList();
            }

            var processorIds = Config.GetParameter<List<string>>("ProcessorIds");
            if (processorIds != null && processorIds.Count > 0)
            {
                processors = processors
                    .Where(p => processorIds.Contains(p.Designation, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            // If OCR is specifically requested by the recipe step, execute OCR and populate text properties
            bool isExplicitOcr = string.Equals(mode, "OCR", StringComparison.OrdinalIgnoreCase) ||
                                 (processorIds != null && processorIds.Any(id => id.IndexOf("Ocr", StringComparison.OrdinalIgnoreCase) >= 0));

            if (isExplicitOcr)
            {
                var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
                if (ocrProvider != null)
                {
                    var surf = payload.EnsureSurface();
                    if (surf != null)
                    {
                        try
                        {
                            var ocrLines = await ocrProvider.DoOcrAsync(surf).ConfigureAwait(false);
                            if (ocrLines != null && ocrLines.Any())
                            {
                                string txt = string.Join(Environment.NewLine, ocrLines.Select(l => l.Text));
                                payload.ExtractedText = txt;
                                context.Properties["OcrText"] = txt;
                                context.Properties["Text"] = txt;
                                context.Properties["CommandResult"] = txt;
                                lock (payload.RawCapture.CaptureDetails.Features)
                                {
                                    payload.RawCapture.CaptureDetails.Features.AddRange(ocrLines);
                                }
                                context.LogStep($"OCR extracted {ocrLines.Count} line(s) of text.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to execute OCR in ProcessorExecutionStep", ex);
                        }
                    }
                }
            }

            foreach (var processor in processors)
            {
                if (context.IsAborted || cancellationToken.IsCancellationRequested) break;

                context.LogStep($"Running processor: {processor.Description}");
                Log.InfoFormat("Calling processor {0}", processor.Description);
                processor.ProcessCapture(payload.RawCapture);
            }
        }
    }
}
