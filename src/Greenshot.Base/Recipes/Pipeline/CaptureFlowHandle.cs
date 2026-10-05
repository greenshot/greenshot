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

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// A flow started by the <see cref="ICaptureFlowRunner"/>: observe it with <see cref="Completion"/>, stop it with <see cref="Cancel"/>.
    /// </summary>
    public sealed class CaptureFlowHandle
    {
        private readonly CancellationTokenSource _cancellation;
        private Task<CaptureFlowResult> _completion;

        public CaptureFlowHandle(Guid id, CaptureRecipe recipe, CancellationTokenSource cancellation, Task<CaptureFlowResult> completion = null)
        {
            Id = id;
            Recipe = recipe;
            _cancellation = cancellation;
            _completion = completion;
        }

        public Guid Id { get; }

        public CaptureRecipe Recipe { get; }

        /// <summary>
        /// Completes when the flow ended, never faults.
        /// </summary>
        public Task<CaptureFlowResult> Completion => _completion;

        /// <summary>
        /// Token which is cancelled when the flow is cancelled (or the runner shuts down).
        /// </summary>
        public CancellationToken CancellationToken => _cancellation?.Token ?? CancellationToken.None;

        /// <summary>
        /// Request cancellation of the flow.
        /// </summary>
        public void Cancel()
        {
            try
            {
                _cancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already finished
            }
        }

        /// <summary>
        /// Used by the runner to attach the running task.
        /// </summary>
        public void SetCompletion(Task<CaptureFlowResult> completion)
        {
            if (_completion != null) throw new InvalidOperationException("Completion is already set.");
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
        }

        /// <summary>
        /// A handle for a start which was rejected (runner shutting down, exclusive recipe already running).
        /// </summary>
        public static CaptureFlowHandle Rejected(CaptureRecipe recipe, string reason)
        {
            var id = Guid.NewGuid();
            return new CaptureFlowHandle(id, recipe, null, Task.FromResult(CaptureFlowResult.Cancelled(id, reason)));
        }
    }
}
