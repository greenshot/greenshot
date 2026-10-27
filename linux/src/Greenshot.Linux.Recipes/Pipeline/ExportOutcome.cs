/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

#nullable disable

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Destinations;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// What one destination did with the capture of a recipe run, for the notification at its end
    /// </summary>
    public sealed class ExportOutcome
    {
        public ExportOutcome(IDestination destination, ExportResult result, CaptureDetails captureDetails, Capture source)
        {
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            CaptureDetails = captureDetails;
            Source = source;
        }

        public IDestination Destination { get; }

        public ExportResult Result { get; }

        public CaptureDetails CaptureDetails { get; }

        public Capture Source { get; }

        public string Target => Destination.DisplayName ?? Destination.Designation;
    }

    /// <summary>
    /// The exports of a recipe run; the branches add to the same log
    /// </summary>
    public sealed class ExportLog
    {
        public const string PropertyName = "ExportLog";

        private readonly List<ExportOutcome> _outcomes = new List<ExportOutcome>();

        private int _notified;

        public void Add(ExportOutcome outcome)
        {
            // Canceled by the user, nothing to tell
            if (outcome == null || outcome.Result.Cancelled)
            {
                return;
            }

            lock (_outcomes)
            {
                _outcomes.Add(outcome);
            }
        }

        public IReadOnlyList<ExportOutcome> ToList()
        {
            lock (_outcomes)
            {
                return _outcomes.ToArray();
            }
        }

        /// <summary>
        /// The outcomes which weren't notified yet, they count as notified now
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

        public static void RecordExport(this CaptureFlowContext context, IDestination destination, ExportResult result, CaptureDetails captureDetails, Capture source) =>
            context.GetExportLog().Add(new ExportOutcome(destination, result, captureDetails, source));
    }
}
