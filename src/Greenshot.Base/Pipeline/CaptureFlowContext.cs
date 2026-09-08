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
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Encapsulates the execution state and lifecycle of a capture pipeline run.
    /// Strictly decoupled from UI and graphical payload types to ensure clean testability and maintainability.
    /// </summary>
    public class CaptureFlowContext : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Unique execution identifier for tracking/logging this flow.
        /// </summary>
        public Guid ExecutionId { get; } = Guid.NewGuid();

        /// <summary>
        /// The recipe driving this flow.
        /// </summary>
        public CaptureRecipe Recipe { get; }

        /// <summary>
        /// The trigger that initiated this flow (null if triggered manually/programmatically).
        /// </summary>
        public ITrigger Trigger { get; }

        /// <summary>
        /// Current lifecycle state of the flow.
        /// </summary>
        public CaptureFlowState State { get; set; } = CaptureFlowState.NotStarted;

        /// <summary>
        /// Extensible property bag for steps and triggers to share data (e.g. OCR text, window handles, user choices).
        /// </summary>
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The visual payload (bitmap, surface, extracted text). Null until acquisition succeeds.
        /// </summary>
        public ICapturePayload Payload { get; set; }

        /// <summary>
        /// Cancellation token for early termination.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Whether the flow was explicitly aborted or cancelled.
        /// </summary>
        public bool IsAborted => State == CaptureFlowState.Cancelled || State == CaptureFlowState.Failed;

        /// <summary>
        /// Reason for aborting/failing the flow, if any.
        /// </summary>
        public string AbortReason { get; private set; }

        /// <summary>
        /// Optional exception that caused a failure.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Chronological execution log for diagnostic tracking.
        /// </summary>
        public List<string> ExecutionLog { get; } = new List<string>();

        public CaptureFlowContext(CaptureRecipe recipe, ITrigger trigger = null, CancellationToken cancellationToken = default)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            Trigger = trigger;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Log a pipeline step or event.
        /// </summary>
        public void LogStep(string message)
        {
            ExecutionLog.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }

        /// <summary>
        /// Cancel or abort the flow cleanly.
        /// </summary>
        public void Abort(string reason)
        {
            State = CaptureFlowState.Cancelled;
            AbortReason = reason;
            LogStep($"Flow cancelled: {reason}");
        }

        /// <summary>
        /// Mark the flow as failed with an exception.
        /// </summary>
        public void Fail(string reason, Exception ex = null)
        {
            State = CaptureFlowState.Failed;
            AbortReason = reason;
            Error = ex;
            LogStep($"Flow failed: {reason} {(ex != null ? ex.Message : "")}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Payload?.Dispose();
                Payload = null;
            }
        }
    }
}
