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
using Greenshot.Linux.Editor;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Pipeline
{
    public class CapturePayload : ICapturePayload
    {
        private readonly object _exportSourceLock = new object();
        private Capture _exportSource;
        private bool _disposed;

        public CapturePayload(Capture rawCapture = null)
        {
            RawCapture = rawCapture;
        }

        public Capture RawCapture { get; set; }

        public EditorSurface Surface { get; set; }

        public string ExtractedText { get; set; }

        public bool RetainSurfaceForEditor { get; set; }

        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public SKBitmap Image => Surface?.Image ?? RawCapture?.Image;

        public CaptureDetails Details => RawCapture?.Details;

        public EditorSurface EnsureSurface()
        {
            if (Surface == null && RawCapture != null)
            {
                Surface = new EditorSurface(RawCapture.Image.Copy());
            }

            return Surface;
        }

        public Task<Capture> GetExportSourceAsync(IUiDispatcher ui, CancellationToken cancellationToken = default)
        {
            lock (_exportSourceLock)
            {
                if (_exportSource == null && RawCapture != null)
                {
                    _exportSource = Surface == null || Surface.Elements.Count == 0 && ReferenceEquals(Surface.Image, RawCapture.Image)
                        ? new Capture(RawCapture.Image.Copy(), RawCapture.Details)
                        : new Capture(Surface.Flatten(), RawCapture.Details);
                }

                return Task.FromResult(_exportSource);
            }
        }

        public void InvalidateExportSource()
        {
            Capture exportSource;
            lock (_exportSourceLock)
            {
                exportSource = _exportSource;
                _exportSource = null;
            }

            exportSource?.Dispose();
        }

        public ICapturePayload Clone()
        {
            var clone = new CapturePayload
            {
                RawCapture = RawCapture == null ? null : new Capture(RawCapture.Image.Copy(), RawCapture.Details),
                Surface = CloneSurface(Surface),
                ExtractedText = ExtractedText,
                RetainSurfaceForEditor = false,
            };
            foreach (var kvp in Metadata)
            {
                clone.Metadata[kvp.Key] = kvp.Value;
            }

            return clone;
        }

        private static EditorSurface CloneSurface(EditorSurface surface)
        {
            if (surface == null)
            {
                return null;
            }

            var clone = new EditorSurface(surface.Image.Copy()) { CounterStart = surface.CounterStart };
            clone.LoadElements(surface.SerializeElements());
            return clone;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            InvalidateExportSource();
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
