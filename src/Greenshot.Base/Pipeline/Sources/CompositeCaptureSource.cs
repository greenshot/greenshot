/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading.Tasks;

namespace Greenshot.Base.Pipeline.Sources
{
    /// <summary>
    /// Composite capture source that combines multiple capture sources
    /// (e.g. ScreenCaptureSource for screen pixels + CursorCaptureSource for mouse cursor).
    /// </summary>
    public class CompositeCaptureSource : ICaptureSource
    {
        public string Name { get; }
        public List<ICaptureSource> Sources { get; } = new List<ICaptureSource>();

        public CompositeCaptureSource(string name = "CompositeCaptureSource", IEnumerable<ICaptureSource> sources = null)
        {
            Name = name;
            if (sources != null)
            {
                Sources.AddRange(sources);
            }
        }

        public async Task<ICapturePayload> AcquireAsync(CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            ICapturePayload combinedPayload = context.Payload;

            foreach (var source in Sources)
            {
                if (context.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                context.LogStep($"Executing source in composite: {source.Name}");
                var result = await source.AcquireAsync(context, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    combinedPayload = result;
                    context.Payload = combinedPayload;
                }
            }

            return combinedPayload;
        }
    }
}
