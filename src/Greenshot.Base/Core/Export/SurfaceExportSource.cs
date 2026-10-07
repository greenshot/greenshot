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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using log4net;
using Greenshot.Base.Core.FileFormat;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// <see cref="IExportSource"/> over a surface (roadmap section 6, the bridge until the AnnotationDocument exists):
    /// the surface is rendered once, on the UI thread, and the resulting bitmap is owned by this source. Everything else
    /// (effects, color reduction, encoding) happens on the calling pool thread and is cached per output settings.
    /// </summary>
    public sealed class SurfaceExportSource : IExportSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SurfaceExportSource));
        private readonly ISurface _surface;
        private readonly IUiDispatcher _ui;
        // Serializes rendering and encoding: GDI+ images are not thread safe
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, Image> _renders = new Dictionary<string, Image>();
        private readonly Dictionary<string, EncodedImage> _encodings = new Dictionary<string, EncodedImage>();
        private Image _exportImage;
        private int _openLeases;
        private bool _disposed;

        /// <param name="surface">The surface, a WinForms control: only touched through the dispatcher</param>
        /// <param name="ui">Dispatcher for the UI thread</param>
        /// <param name="isModified">Was the surface modified (snapshot at creation, see <see cref="IsModified"/>)</param>
        public SurfaceExportSource(ISurface surface, IUiDispatcher ui, bool isModified)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            IsModified = isModified;
        }

        /// <summary>
        /// Create the source, reading the modified state of the surface on the UI thread.
        /// </summary>
        public static async Task<SurfaceExportSource> CreateAsync(ISurface surface, IUiDispatcher ui, CancellationToken cancellationToken = default)
        {
            bool isModified = await ui.InvokeAsync(() => surface.Modified, cancellationToken).ConfigureAwait(false);
            return new SurfaceExportSource(surface, ui, isModified);
        }

        /// <summary>
        /// True when the capture was changed since it was loaded or saved: a destination can't reuse the original file then.
        /// </summary>
        public bool IsModified { get; }

        /// <summary>
        /// Number of render leases which are not disposed yet (tests, rule R8).
        /// </summary>
        public int OpenLeases => Volatile.Read(ref _openLeases);

        public async Task<IImageLease> RenderAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var image = await RenderLockedAsync(settings, cancellationToken).ConfigureAwait(false);
                // Every lease gets its own copy: GDI+ images can't be used by two threads at the same time (parallel branches),
                // and the lease stays valid when this source is disposed
                var copy = ImageHelper.Clone(image);
                Interlocked.Increment(ref _openLeases);
                return new ImageLease(copy, this);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<Image> RenderLockedAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            string key = CacheKey(settings);
            if (key != null && _renders.TryGetValue(key, out var cached))
            {
                return cached;
            }

            Image baseImage;
            bool ownsBaseImage;
            if (settings.SaveBackgroundOnly)
            {
                // The background only, without the elements: a copy made on the UI thread
                baseImage = await _ui.InvokeAsync(() => ImageHelper.Clone(_surface.Image), cancellationToken).ConfigureAwait(false);
                ownsBaseImage = true;
            }
            else
            {
                // Render the surface once, on the UI thread: this source owns the result
                if (_exportImage == null)
                {
                    var exportImage = await _ui.InvokeAsync(() => _surface.GetImageForExport(), cancellationToken).ConfigureAwait(false);
                    if (_disposed)
                    {
                        // Disposed while rendering
                        exportImage.Dispose();
                        ThrowIfDisposed();
                    }

                    _exportImage = exportImage;
                }

                baseImage = _exportImage;
                ownsBaseImage = false;
            }

            ImageIO.CreateImageForOutput(baseImage, ownsBaseImage, settings, out var rendered);
            if (_disposed)
            {
                // Disposed while rendering
                if (!ReferenceEquals(rendered, _exportImage))
                {
                    rendered.Dispose();
                }

                ThrowIfDisposed();
            }
            if (key != null)
            {
                _renders[key] = rendered;
            }
            else
            {
                // Settings with effects are not cached, keep the image until this source is disposed
                _renders[Guid.NewGuid().ToString()] = rendered;
            }

            return rendered;
        }

        public async Task<EncodedImage> EncodeAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                string key = CacheKey(settings);
                if (key != null && _encodings.TryGetValue(key, out var cached))
                {
                    return cached;
                }

                byte[] bytes;
                if (WellKnownFileFormats.IsEqualFormat(WellKnownFileFormats.Greenshot, settings.Format))
                {
                    // The greenshot format serializes the elements of the surface: UI thread
                    bytes = await _ui.InvokeAsync(() =>
                    {
                        using var stream = RecyclableMemoryStreamFactory.GetStream("SurfaceExportSource.Encode");
                        ImageIO.SaveToStream(_surface, stream, settings);
                        return stream.ToArray();
                    }, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var image = await RenderLockedAsync(settings, cancellationToken).ConfigureAwait(false);
                    using var stream = RecyclableMemoryStreamFactory.GetStream("SurfaceExportSource.Encode");
                    ImageIO.SaveToStream(image, null, stream, settings);
                    bytes = stream.ToArray();
                }

                var encoded = new EncodedImage(bytes, settings.Format);
                if (key != null)
                {
                    _encodings[key] = encoded;
                }

                return encoded;
            }
            finally
            {
                _lock.Release();
            }
        }

        public Task<T> UseSurfaceAsync<T>(Func<ISurface, T> use, CancellationToken cancellationToken)
        {
            if (use == null) throw new ArgumentNullException(nameof(use));
            ThrowIfDisposed();
            return _ui.InvokeAsync(() => use(_surface), cancellationToken);
        }

        /// <summary>
        /// Settings with effects can't be compared, they are not cached.
        /// </summary>
        private static string CacheKey(SurfaceOutputSettings settings)
        {
            if (settings.Effects != null && settings.Effects.Count > 0)
            {
                return null;
            }

            return $"{settings.Format}|{settings.JPGQuality}|{settings.ReduceColors}|{settings.DisableReduceColors}|{settings.SaveBackgroundOnly}";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SurfaceExportSource));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (Volatile.Read(ref _openLeases) > 0)
            {
                Log.WarnFormat("SurfaceExportSource disposed with {0} open image lease(s)", _openLeases);
            }

            foreach (var image in _renders.Values)
            {
                if (!ReferenceEquals(image, _exportImage))
                {
                    image?.Dispose();
                }
            }

            _renders.Clear();
            _encodings.Clear();
            _exportImage?.Dispose();
            _exportImage = null;
        }

        private sealed class ImageLease : IImageLease
        {
            private SurfaceExportSource _owner;

            /// <param name="image">the copy for this lease, the lease owns (disposes) it</param>
            /// <param name="owner">the source, which counts the open leases</param>
            public ImageLease(Image image, SurfaceExportSource owner)
            {
                Image = image;
                _owner = owner;
            }

            public Image Image { get; private set; }

            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                if (owner == null) return;
                Image?.Dispose();
                Image = null;
                Interlocked.Decrement(ref owner._openLeases);
            }
        }
    }
}
