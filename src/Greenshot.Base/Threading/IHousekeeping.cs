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

using System;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Housekeeping: small work which isn't urgent, like removing temporary files, queued from any part of Greenshot (also plugins).
    /// It runs in the background, one item after the other, once Greenshot has started; what is still queued at exit is dropped.
    /// </summary>
    public interface IHousekeeping
    {
        /// <summary>
        /// Queue work for the background
        /// </summary>
        /// <param name="description">What the work does, for the log</param>
        /// <param name="work">The work, it should stop when the token is cancelled (at exit). An exception is logged, the next item runs.</param>
        /// <param name="delay">How long to wait at least before the work runs, e.g. for a file which may still be in use</param>
        void Enqueue(string description, Func<CancellationToken, Task> work, TimeSpan delay = default);
    }

    /// <summary>
    /// Access to the housekeeping of the running Greenshot
    /// </summary>
    public static class Housekeeping
    {
        /// <summary>
        /// The housekeeping of the running Greenshot, null where there is none (e.g. tests or a second instance)
        /// </summary>
        public static IHousekeeping Current => SimpleServiceProvider.Current?.GetInstance<IHousekeeping>(isOptional: true);
    }
}
