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
using System.Threading.Tasks;
using Greenshot.Document;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Rendering;
using Greenshot.Editor.Standalone.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Standalone.Controls
{
    /// <summary>
    /// Keeps the pixels of a tab up to date with its document. The document renderer (ImageSharp, the same pixels as an
    /// export) draws on the thread pool, and only the part in which the document differs from what is shown. While it draws,
    /// newer documents (every mouse move of a drag is one) replace each other: the next drawing is always of the newest.
    /// </summary>
    internal sealed class TabSurface : IDisposable
    {
        private readonly DocumentTabViewModel _tab;
        private readonly RenderCache _cache = new();
        private GreenshotDocument _rendered;
        private bool _busy;
        private bool _disposed;

        public TabSurface(DocumentTabViewModel tab)
        {
            _tab = tab;
            _tab.ShownChanged += Update;
            _tab.Disposed += Dispose;
            Update();
        }

        /// <summary>
        /// The pixels which are shown, null until the document was drawn for the first time
        /// </summary>
        public CanvasSurface Surface { get; private set; }

        /// <summary>
        /// True when the pixels are those of the document of the tab as it is now
        /// </summary>
        public bool IsCurrent => ReferenceEquals(_rendered, _tab.Document);

        /// <summary>
        /// The pixels changed. Raised on the UI thread.
        /// </summary>
        public event Action Painted;

        private void Update()
        {
            if (!_busy && !_disposed)
            {
                DrawAsync().Forget("Drawing the document");
            }
        }

        private async Task DrawAsync()
        {
            _busy = true;
            try
            {
                while (!_disposed && !ReferenceEquals(_tab.Document, _rendered))
                {
                    var document = _tab.Document;
                    var before = _rendered;
                    long start = System.Diagnostics.Stopwatch.GetTimestamp();
                    // PARALLEL: drawing takes long enough to notice, the window stays usable meanwhile
#pragma warning disable RS0030 // R10: the documented boundary to the thread pool for drawing
                    var drawn = await Task.Run(() => Draw(before, document));
#pragma warning restore RS0030
                    if (_disposed)
                    {
                        drawn.Image?.Dispose();
                        _cache.Dispose();
                        return;
                    }

                    using (drawn.Image)
                    {
                        if (drawn.Whole)
                        {
                            Surface?.Dispose();
                            Surface = new CanvasSurface(drawn.Area);
                        }

                        if (drawn.Image != null)
                        {
                            Surface.Write(drawn.Image, (int)Math.Round(drawn.Area.X - Surface.Area.X), (int)Math.Round(drawn.Area.Y - Surface.Area.Y));
                        }
                    }

                    _rendered = document;
                    _cache.Keep(document);
                    if (EditorLog.IsDebugEnabled && drawn.Image != null)
                    {
                        EditorLog.Debug($"Drew {drawn.Area.Width}x{drawn.Area.Height} in {System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds:0} ms");
                    }

                    Painted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                // Shown as it was; the next change tries again
                EditorLog.Error("Drawing the document failed", ex);
                _rendered = _tab.Document;
            }
            finally
            {
                _busy = false;
            }
        }

        private (Image<Rgba32> Image, RectD Area, bool Whole) Draw(GreenshotDocument before, GreenshotDocument document)
        {
            if (DocumentDiff.Changed(before, document) is RectD changed)
            {
                var area = changed.Width > 0 && changed.Height > 0 ? DocumentRenderer.Affected(document, changed) : default;
                return area.Width > 0 && area.Height > 0 ? (DocumentRenderer.Render(document, area, _cache), area, false) : (null, default, false);
            }

            var canvas = document.CanvasArea();
            return (DocumentRenderer.Render(document, null, _cache), canvas, true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _tab.ShownChanged -= Update;
            _tab.Disposed -= Dispose;
            Surface?.Dispose();
            Surface = null;
            if (!_busy)
            {
                _cache.Dispose();
            }
        }
    }
}
