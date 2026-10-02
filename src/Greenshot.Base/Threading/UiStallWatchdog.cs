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
using System.Diagnostics;
using System.Threading;
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Measures how responsive the UI thread is (roadmap Phase 0): a timer posts to the UI thread every 100 ms and logs a warning,
    /// with the currently running flow steps, when a post isn't handled within 250 ms.
    /// Active in debug builds, opt-in for release builds with the EnableUiStallWatchdog setting.
    /// </summary>
    public sealed class UiStallWatchdog : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UiStallWatchdog));
        private readonly SynchronizationContext _uiContext;
        private readonly long _thresholdTicks;
        private readonly Timer _timer;
        private long _pendingSince;
        private int _reported;
        private int _stallCount;
        private long _maxStallTicks;
        private long _totalStallTicks;
        private bool _disposed;

        /// <param name="uiContext">SynchronizationContext of the UI thread</param>
        /// <param name="interval">Interval between two probes, default 100 ms</param>
        /// <param name="threshold">Duration after which a probe counts as stall, default 250 ms</param>
        public UiStallWatchdog(SynchronizationContext uiContext, TimeSpan? interval = null, TimeSpan? threshold = null)
        {
            _uiContext = uiContext ?? throw new ArgumentNullException(nameof(uiContext));
            var probeInterval = interval ?? TimeSpan.FromMilliseconds(100);
            _thresholdTicks = (long)((threshold ?? TimeSpan.FromMilliseconds(250)).TotalSeconds * Stopwatch.Frequency);
            _timer = new Timer(_ => Probe(), null, probeInterval, probeInterval);
        }

        /// <summary>
        /// Number of stalls longer than the threshold.
        /// </summary>
        public int StallCount => Volatile.Read(ref _stallCount);

        /// <summary>
        /// Longest stall.
        /// </summary>
        public TimeSpan MaxStall => TimeSpan.FromSeconds((double)Interlocked.Read(ref _maxStallTicks) / Stopwatch.Frequency);

        /// <summary>
        /// Sum of all stalls.
        /// </summary>
        public TimeSpan TotalStall => TimeSpan.FromSeconds((double)Interlocked.Read(ref _totalStallTicks) / Stopwatch.Frequency);

        private void Probe()
        {
            if (_disposed) return;
            long now = Stopwatch.GetTimestamp();
            long pendingSince = Interlocked.Read(ref _pendingSince);
            if (pendingSince != 0)
            {
                // The previous probe wasn't handled yet
                if (now - pendingSince > _thresholdTicks && Interlocked.Exchange(ref _reported, 1) == 0)
                {
                    Log.WarnFormat("UI thread is not responding for more than {0:0} ms, running: {1}", ToMilliseconds(now - pendingSince), FlowDiagnostics.Describe());
                }

                return;
            }

            Interlocked.Exchange(ref _pendingSince, now);
            try
            {
                _uiContext.Post(_ => ProbeHandled(), null);
            }
            catch (Exception)
            {
                // Message loop is gone, we are shutting down
                Interlocked.Exchange(ref _pendingSince, 0);
            }
        }

        private void ProbeHandled()
        {
            long pendingSince = Interlocked.Exchange(ref _pendingSince, 0);
            if (pendingSince == 0) return;
            long duration = Stopwatch.GetTimestamp() - pendingSince;
            Interlocked.Exchange(ref _reported, 0);
            if (duration <= _thresholdTicks)
            {
                return;
            }

            Interlocked.Increment(ref _stallCount);
            Interlocked.Add(ref _totalStallTicks, duration);
            long max;
            while (duration > (max = Interlocked.Read(ref _maxStallTicks)))
            {
                if (Interlocked.CompareExchange(ref _maxStallTicks, duration, max) == max) break;
            }

            Log.WarnFormat("UI thread stalled for {0:0} ms", ToMilliseconds(duration));
        }

        private static double ToMilliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Dispose();
            Log.InfoFormat("UI stall watchdog summary: {0} stall(s) > threshold, longest {1:0} ms, total {2:0} ms",
                StallCount, MaxStall.TotalMilliseconds, TotalStall.TotalMilliseconds);
        }
    }
}
