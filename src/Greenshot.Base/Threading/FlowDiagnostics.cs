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
using System.Collections.Concurrent;
using System.Linq;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Keeps track of the step every running flow is executing, so the UI-stall watchdog can say what was running during a stall.
    /// </summary>
    public static class FlowDiagnostics
    {
        private static readonly ConcurrentDictionary<Guid, string> RunningSteps = new ConcurrentDictionary<Guid, string>();

        /// <summary>
        /// Mark the flow as running the step until the returned object is disposed.
        /// </summary>
        public static IDisposable EnterStep(Guid flowId, string stepName)
        {
            RunningSteps.TryGetValue(flowId, out var previous);
            RunningSteps[flowId] = stepName;
            return new StepScope(flowId, previous);
        }

        /// <summary>
        /// Remove the flow, called when it ends.
        /// </summary>
        public static void FlowEnded(Guid flowId)
        {
            RunningSteps.TryRemove(flowId, out _);
        }

        /// <summary>
        /// Human readable description of the running steps, e.g. "3f2a…: DestinationExportStep".
        /// </summary>
        public static string Describe()
        {
            var snapshot = RunningSteps.ToArray();
            if (snapshot.Length == 0)
            {
                return "no flow running";
            }

            return string.Join(", ", snapshot.Select(kv => $"{kv.Key.ToString().Substring(0, 8)}: {kv.Value}"));
        }

        private sealed class StepScope : IDisposable
        {
            private readonly Guid _flowId;
            private readonly string _previous;
            private bool _disposed;

            public StepScope(Guid flowId, string previous)
            {
                _flowId = flowId;
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_previous == null)
                {
                    RunningSteps.TryRemove(_flowId, out _);
                }
                else
                {
                    RunningSteps[_flowId] = _previous;
                }
            }
        }
    }
}
