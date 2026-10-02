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

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// The outcome of a flow started with the <see cref="ICaptureFlowRunner"/>. A flow never faults: exceptions become <see cref="CaptureFlowState.Failed"/>.
    /// </summary>
    public sealed class CaptureFlowResult
    {
        private CaptureFlowResult(Guid flowId, CaptureFlowState state, string reason, Exception error, int exitCode, CaptureFlowContext context)
        {
            FlowId = flowId;
            State = state;
            Reason = reason;
            Error = error;
            ExitCode = exitCode;
            Context = context;
        }

        public Guid FlowId { get; }

        /// <summary>
        /// Completed, Cancelled or Failed
        /// </summary>
        public CaptureFlowState State { get; }

        /// <summary>
        /// Why the flow was cancelled or failed.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// The exception which failed the flow, if any.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Exit code of the flow (IPC / CLI).
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// The finished (disposed) flow context, for its properties and execution log. Null when the flow didn't start.
        /// </summary>
        public CaptureFlowContext Context { get; }

        public bool IsSuccess => State == CaptureFlowState.Completed;

        public static CaptureFlowResult FromContext(CaptureFlowContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var state = context.State == CaptureFlowState.Failed || context.State == CaptureFlowState.Cancelled
                ? context.State
                : CaptureFlowState.Completed;
            return new CaptureFlowResult(context.ExecutionId, state, context.AbortReason, context.Error, context.ExitCode, context);
        }

        public static CaptureFlowResult Cancelled(Guid flowId, string reason) => new CaptureFlowResult(flowId, CaptureFlowState.Cancelled, reason, null, 0, null);

        public static CaptureFlowResult Failed(Guid flowId, Exception error) => new CaptureFlowResult(flowId, CaptureFlowState.Failed, error?.Message, error, 1, null);
    }
}
