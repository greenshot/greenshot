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

using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Contract for capture sources responsible for acquiring raw capture data (pixels, cursor, metadata).
    /// </summary>
    public interface ICaptureSource
    {
        /// <summary>
        /// Source identifier or name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Acquires the visual/image payload for the current flow.
        /// </summary>
        Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default);
    }
}
