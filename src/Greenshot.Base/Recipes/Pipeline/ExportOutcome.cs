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
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// One export of a flow: which destination exported what, and its result. The Notification step shows the exports of the flow
    /// (see <see cref="ExportLog"/>).
    /// </summary>
    public sealed class ExportOutcome
    {
        public ExportOutcome(IDestination destination, ExportResult result, ICaptureDetails captureDetails, IExportSource source)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            // The destination which really exported, e.g. the one picked in the destination picker
            Destination = result.ExportedBy ?? destination;
            CaptureDetails = captureDetails;
            Source = source;
        }

        /// <summary>
        /// The destination which exported (<see cref="ExportResult.ExportedBy"/> when another one did it for the called one)
        /// </summary>
        public IDestination Destination { get; }

        public ExportResult Result { get; }

        /// <summary>
        /// The details of the exported capture (e.g. the title of the window), or null
        /// </summary>
        public ICaptureDetails CaptureDetails { get; }

        /// <summary>
        /// What was exported: the capture of the flow, or the copy with the extensions of the destination. Lives as long as the flow.
        /// </summary>
        public IExportSource Source { get; }

        /// <summary>
        /// What the export went to, e.g. the file name or the name of the destination
        /// </summary>
        public string Target => Result.Target ?? Destination.Descriptor?.DisplayName ?? Destination.Designation;
    }

    /// <summary>
    /// The exports of a flow, in their order. One log per flow: it is in <see cref="CaptureFlowContext.Properties"/> from the start, so
    /// the contexts of the nodes and branches, which copy the properties, add to the same log. Thread safe for parallel branches.
    /// </summary>
    public sealed class ExportLog
    {
        /// <summary>
        /// The key in <see cref="CaptureFlowContext.Properties"/>
        /// </summary>
        public const string PropertyName = "ExportLog";

        private readonly List<ExportOutcome> _outcomes = new List<ExportOutcome>();

        /// <summary>
        /// How many of the exports a notification showed already
        /// </summary>
        private int _notified;

        /// <summary>
        /// Adds the export; one which the user declined isn't an export
        /// </summary>
        public void Add(ExportOutcome outcome)
        {
            if (outcome == null || outcome.Result.Status == ExportStatus.Declined)
            {
                return;
            }
            lock (_outcomes)
            {
                _outcomes.Add(outcome);
            }
        }

        /// <summary>
        /// The exports so far, a copy
        /// </summary>
        public IReadOnlyList<ExportOutcome> ToList()
        {
            lock (_outcomes)
            {
                return _outcomes.ToArray();
            }
        }

        /// <summary>
        /// The exports since the last call, for the Notification step: a recipe with two Notification steps doesn't show an
        /// export twice
        /// </summary>
        public IReadOnlyList<ExportOutcome> TakeUnnotified()
        {
            lock (_outcomes)
            {
                var unnotified = _outcomes.Skip(_notified).ToArray();
                _notified = _outcomes.Count;
                return unnotified;
            }
        }
    }

    public static class ExportLogExtensions
    {
        /// <summary>
        /// The export log of the flow
        /// </summary>
        public static ExportLog GetExportLog(this CaptureFlowContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            lock (context.Properties)
            {
                if (!context.Properties.TryGetValue(ExportLog.PropertyName, out var value) || value is not ExportLog log)
                {
                    log = new ExportLog();
                    context.Properties[ExportLog.PropertyName] = log;
                }
                return log;
            }
        }

        /// <summary>
        /// Adds an export to the log of the flow: everything which exports in a flow (the destination steps, the plugin steps)
        /// </summary>
        public static void RecordExport(this CaptureFlowContext context, IDestination destination, ExportResult result, ICaptureDetails captureDetails, IExportSource source) =>
            context.GetExportLog().Add(new ExportOutcome(destination, result, captureDetails, source));
    }
}
