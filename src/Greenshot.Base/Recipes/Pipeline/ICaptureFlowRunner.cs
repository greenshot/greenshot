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

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// The single entry point which starts capture flows (roadmap section 4.4): hands the flow from the caller (usually the UI thread)
    /// to the thread pool, tracks it, observes its outcome, applies the recipe's concurrency policy and cancels running flows on shutdown.
    /// </summary>
    public interface ICaptureFlowRunner
    {
        /// <summary>
        /// Start a flow for the recipe. Returns immediately, the flow runs on the thread pool.
        /// </summary>
        /// <param name="recipe">The recipe to run</param>
        /// <param name="trigger">Snapshot of the trigger situation, taken synchronously by the caller; null takes a snapshot now</param>
        /// <param name="configure">Optional callback which configures the context before the flow starts</param>
        CaptureFlowHandle Start(CaptureRecipe recipe, FlowTriggerContext trigger = null, Action<CaptureFlowContext> configure = null);

        /// <summary>
        /// The flows which are currently running.
        /// </summary>
        IReadOnlyCollection<CaptureFlowHandle> Running { get; }

        /// <summary>
        /// Stop accepting new flows, cancel the running ones and wait for them (until the token is cancelled).
        /// </summary>
        Task ShutdownAsync(CancellationToken cancellationToken);
    }
}
