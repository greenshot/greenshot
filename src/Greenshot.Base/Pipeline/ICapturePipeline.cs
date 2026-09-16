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
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Contract for the capture pipeline execution engine.
    /// </summary>
    public interface ICapturePipeline
    {
        /// <summary>
        /// Executes a capture recipe through the pipeline.
        /// </summary>
        /// <param name="recipe">The recipe to execute.</param>
        /// <param name="trigger">Optional trigger that initiated the execution.</param>
        /// <param name="configureContext">Optional callback to configure the context before execution begins.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resulting flow context.</returns>
        Task<CaptureFlowContext> ExecuteAsync(
            CaptureRecipe recipe,
            ITrigger trigger = null,
            Action<CaptureFlowContext> configureContext = null,
            CancellationToken cancellationToken = default);
    }
}
