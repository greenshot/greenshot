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
using Greenshot.Base.Core;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Access to the registered services of the threading model, for code which has no context to get them from
    /// (until the dependency injection of the .NET 10 move).
    /// </summary>
    public static class UiDispatcher
    {
        /// <summary>
        /// The dispatcher of the UI thread, inline when there is none (tests, headless).
        /// </summary>
        public static IUiDispatcher Current => SimpleServiceProvider.Current?.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
    }

    /// <summary>
    /// Access to the STA workers for COM servers.
    /// </summary>
    public static class StaWorkers
    {
        private static readonly Lazy<StaWorkerFactory> Fallback = new Lazy<StaWorkerFactory>(() => new StaWorkerFactory());

        /// <summary>
        /// The worker for the COM server with the name, e.g. "Office" or "MAPI".
        /// </summary>
        public static IStaWorker Get(string name)
        {
            var factory = SimpleServiceProvider.Current?.GetInstance<IStaWorkerFactory>(isOptional: true) ?? Fallback.Value;
            return factory.Get(name);
        }
    }
}
