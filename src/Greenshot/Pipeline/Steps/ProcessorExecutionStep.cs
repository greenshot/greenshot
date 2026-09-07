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
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// Pipeline step executing image processors (OCR, TitleFix, or plugin processors).
    /// </summary>
    public class ProcessorExecutionStep : ICaptureStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ProcessorExecutionStep));

        public string Name { get; }
        public RecipeStepConfig Config { get; }

        public ProcessorExecutionStep(RecipeStepConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? "ProcessorExecutionStep";
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            var payload = context.Payload;
            if (payload?.RawCapture == null) return Task.CompletedTask;

            context.State = CaptureFlowState.Processing;

            var processors = SimpleServiceProvider.Current.GetAllInstances<IProcessor>()
                .Where(p => p.isActive)
                .ToList();

            // Optional explicit timing filter: when set, only run processors that declare
            // the matching PreferredTiming. When absent, run all active processors (default,
            // backward-compatible behaviour for recipes that have a single Processors step).
            var timingParam = Config.GetParameter<string>("Timing");
            if (!string.IsNullOrEmpty(timingParam) &&
                Enum.TryParse<ProcessorTiming>(timingParam, ignoreCase: true, out var requestedTiming))
            {
                processors = processors
                    .OfType<AbstractProcessor>()
                    .Where(p => p.PreferredTiming == requestedTiming)
                    .Cast<IProcessor>()
                    .ToList();
            }

            var processorIds = Config.GetParameter<List<string>>("ProcessorIds");
            if (processorIds != null && processorIds.Count > 0)
            {
                processors = processors
                    .Where(p => processorIds.Contains(p.GetType().Name, StringComparer.OrdinalIgnoreCase) ||
                                processorIds.Contains(p.Description, StringComparer.OrdinalIgnoreCase) ||
                                processorIds.Contains(p.Designation, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var processor in processors)
            {
                if (context.IsAborted || cancellationToken.IsCancellationRequested) break;

                context.LogStep($"Running processor: {processor.Description}");
                Log.InfoFormat("Calling processor {0}", processor.Description);
                processor.ProcessCapture(payload.RawCapture);
            }

            return Task.CompletedTask;
        }
    }
}
