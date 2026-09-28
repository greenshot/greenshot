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
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Pipeline step that executes child steps based on condition evaluation.
    /// </summary>
    public class ConditionalCaptureStep : ICaptureStep
    {
        public string Name { get; }
        public IStepCondition Condition { get; }
        public List<ICaptureStep> ThenSteps { get; } = new List<ICaptureStep>();
        public List<ICaptureStep> ElseSteps { get; } = new List<ICaptureStep>();

        public ConditionalCaptureStep(string name, IStepCondition condition, ICaptureStep innerStep)
        {
            Name = name ?? innerStep?.Name ?? "ConditionalStep";
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            if (innerStep != null) ThenSteps.Add(innerStep);
        }

        public ConditionalCaptureStep(string name, IStepCondition condition, IEnumerable<ICaptureStep> thenSteps, IEnumerable<ICaptureStep> elseSteps = null)
        {
            Name = name ?? "ConditionalStep";
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            if (thenSteps != null) ThenSteps.AddRange(thenSteps);
            if (elseSteps != null) ElseSteps.AddRange(elseSteps);
        }

        public async Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (Condition.Evaluate(context))
            {
                context.LogStep($"Condition [{Condition.Description}] MET. Executing {ThenSteps.Count} then-step(s).");
                foreach (var step in ThenSteps)
                {
                    if (context.IsAborted || cancellationToken.IsCancellationRequested) break;
                    await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                context.LogStep($"Condition [{Condition.Description}] NOT MET. Executing {ElseSteps.Count} else-step(s).");
                foreach (var step in ElseSteps)
                {
                    if (context.IsAborted || cancellationToken.IsCancellationRequested) break;
                    await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
