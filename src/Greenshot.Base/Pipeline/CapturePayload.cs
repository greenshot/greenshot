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
using System.Drawing;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Default implementation of ICapturePayload.
    /// </summary>
    public class CapturePayload : ICapturePayload
    {
        private bool _disposed;

        public ICapture RawCapture { get; set; }
        public ISurface Surface { get; set; }
        public string ExtractedText { get; set; }
        public Image SharedRenderedBitmap { get; set; }
        public bool RetainSurfaceForEditor { get; set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Optional delegate to instantiate an ISurface from an ICapture without coupling Greenshot.Base to Greenshot.Editor.
        /// </summary>
        public static Func<ICapture, ISurface> DefaultSurfaceFactory { get; set; }

        public Func<ICapture, ISurface> SurfaceFactory { get; set; }

        public CapturePayload(ICapture rawCapture = null)
        {
            RawCapture = rawCapture;
            SurfaceFactory = DefaultSurfaceFactory;
        }

        public ISurface EnsureSurface()
        {
            if (Surface == null && RawCapture != null)
            {
                var factory = SurfaceFactory ?? DefaultSurfaceFactory;
                if (factory != null)
                {
                    Surface = factory(RawCapture);
                }
            }
            return Surface;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SharedRenderedBitmap?.Dispose();
            SharedRenderedBitmap = null;

            RawCapture?.Dispose();
            RawCapture = null;

            if (!RetainSurfaceForEditor)
            {
                Surface?.Dispose();
                Surface = null;
            }
        }
    }
}
