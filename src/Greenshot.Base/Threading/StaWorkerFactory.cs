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
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Default <see cref="IStaWorkerFactory"/>: one lazily created <see cref="StaWorker"/> per name, unhealthy workers are replaced.
    /// </summary>
    public sealed class StaWorkerFactory : IStaWorkerFactory, IAsyncDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StaWorkerFactory));
        private readonly Dictionary<string, IStaWorker> _workers = new Dictionary<string, IStaWorker>(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, IStaWorker> _workerFactory;
        private bool _disposed;

        /// <param name="workerFactory">Optional factory for the workers (tests), default creates a <see cref="StaWorker"/></param>
        public StaWorkerFactory(Func<string, IStaWorker> workerFactory = null)
        {
            _workerFactory = workerFactory ?? (name => new StaWorker(name));
        }

        public IStaWorker Get(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            IStaWorker unhealthy = null;
            IStaWorker worker;
            lock (_workers)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(StaWorkerFactory));
                if (_workers.TryGetValue(name, out worker) && worker.IsHealthy)
                {
                    return worker;
                }

                unhealthy = worker;
                worker = _workerFactory(name);
                _workers[name] = worker;
            }

            if (unhealthy != null)
            {
                Log.WarnFormat("Replacing the unhealthy STA worker '{0}'", name);
                unhealthy.DisposeAsync().AsTask().FireAndLog($"Dispose STA worker {name}");
            }

            return worker;
        }

        public async ValueTask DisposeAsync()
        {
            List<IStaWorker> workers;
            lock (_workers)
            {
                if (_disposed) return;
                _disposed = true;
                workers = _workers.Values.ToList();
                _workers.Clear();
            }

            await Task.WhenAll(workers.Select(w => w.DisposeAsync().AsTask())).ConfigureAwait(false);
        }
    }
}
