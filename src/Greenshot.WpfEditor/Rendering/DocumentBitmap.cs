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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Document;
using Greenshot.Document.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using log4net;

namespace Greenshot.WpfEditor.Rendering
{
    /// <summary>
    /// The picture of a document on the screen: a WPF bitmap of its canvas, drawn by the document renderer (the same pixels as
    /// an export). After a change only the changed area is drawn again, in the background.
    /// The elements which are dragged or drawn right now (live) aren't in the bitmap: they are drawn alone into the overlay at
    /// once, so they move with the mouse and the handles, while the rest of the bitmap stays as it is.
    /// </summary>
    public sealed class DocumentBitmap : IDisposable
    {
        // Blur, pixelate, magnify and shadows near the edge of a changed area need the pixels around it
        private const int Margin = 32;
        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentBitmap));

        private readonly DocumentRenderCache _cache = new();
        // The overlay and the bitmap are drawn on the UI thread too, while a render in the background uses _cache
        private readonly DocumentRenderCache _liveCache = new();
        private ISet<Guid> _live = new HashSet<Guid>();
        private int _liveVersion;
        private byte[] _row = Array.Empty<byte>();

        /// <summary>
        /// The bitmap, replaced when the size of the canvas changes
        /// </summary>
        public WriteableBitmap Bitmap { get; private set; }

        /// <summary>
        /// Where the bitmap is in canvas pixels: its top left pixel is at Area.X, Area.Y
        /// </summary>
        public RectD Area { get; private set; }

        /// <summary>
        /// Raised when Bitmap is a new bitmap
        /// </summary>
        public event Action BitmapReplaced;

        /// <summary>
        /// The live elements alone (transparent around them), null without live elements
        /// </summary>
        public BitmapSource Overlay { get; private set; }

        /// <summary>
        /// Where the overlay is in canvas pixels
        /// </summary>
        public RectD OverlayArea { get; private set; }

        private GreenshotDocument _document;
        private RectD? _pending;
        private bool _pendingAll;
        private bool _rendering;

        /// <summary>
        /// Raised on the UI thread when a part of the bitmap was drawn again
        /// </summary>
        public event Action Rendered;

        /// <summary>
        /// Draws the document again, where it changed or (with changed null) all of it. The drawing runs in the background,
        /// changes which come in meanwhile are drawn together afterwards. Call it on the UI thread.
        /// </summary>
        public void Update(GreenshotDocument document, RectD? changed) => Update(document, changed, null);

        /// <summary>
        /// Like Update, live are the elements which are drawn into the overlay instead of the bitmap. While the same elements
        /// are live, a change of the document is a change of them: only the overlay is drawn again.
        /// </summary>
        public void Update(GreenshotDocument document, RectD? changed, ISet<Guid> live)
        {
            live ??= new HashSet<Guid>();
            var before = _document;
            _document = document;
            var canvas = document.CanvasArea();
            var area = new RectD(Math.Floor(canvas.X), Math.Floor(canvas.Y), Math.Max(1, Math.Ceiling(canvas.Width)), Math.Max(1, Math.Ceiling(canvas.Height)));
            if (Bitmap == null || area != Area)
            {
                Area = area;
                Bitmap = new WriteableBitmap((int)area.Width, (int)area.Height, 96, 96, PixelFormats.Bgra32, null);
                changed = null;
                BitmapReplaced?.Invoke();
            }

            if (Bitmap.CheckAccess() && changed != null && !(live.Count == 0 && _live.Count == 0))
            {
                if (live.SetEquals(_live))
                {
                    // Only the live elements changed
                    DrawOverlay(document);
                    return;
                }

                // Other elements are live now: where the old and the new ones are, the bitmap is drawn at once
                var redraw = Union(LiveBounds(before, _live), Union(LiveBounds(document, _live), LiveBounds(document, live)));
                _live = live;
                _liveVersion++;
                if (redraw is RectD both)
                {
                    DrawNow(document, both);
                }

                DrawOverlay(document);
            }
            else if (_live.Count > 0 || live.Count > 0)
            {
                // Everything is drawn again, the live elements too
                _live = live;
                _liveVersion++;
                DrawOverlay(document);
                changed = null;
            }

            if (changed is RectD part && !_pendingAll)
            {
                _pending = _pending is RectD earlier ? RectD.Union(earlier, part) : part;
            }
            else
            {
                _pendingAll = true;
                _pending = null;
            }

            if (!_rendering)
            {
                _ = RenderPendingAsync();
            }
        }

        private async Task RenderPendingAsync()
        {
            _rendering = true;
            try
            {
                while (_pendingAll || _pending != null)
                {
                    var document = _document;
                    var area = Area;
                    var live = _live;
                    int liveVersion = _liveVersion;
                    var target = _pendingAll ? area : Intersect(_pending.Value.Inflate(2), area);
                    int margin = _pendingAll ? 0 : Margin;
                    _pendingAll = false;
                    _pending = null;
                    if (target is not RectD draw || draw.Width < 1 || draw.Height < 1)
                    {
                        continue;
                    }

                    // Whole pixels of the bitmap
                    int left = (int)Math.Floor(draw.X - area.X), top = (int)Math.Floor(draw.Y - area.Y);
                    int right = Math.Min((int)area.Width, (int)Math.Ceiling(draw.Right - area.X)), bottom = Math.Min((int)area.Height, (int)Math.Ceiling(draw.Bottom - area.Y));
                    int width = right - left, height = bottom - top;
                    if (width <= 0 || height <= 0)
                    {
                        continue;
                    }

                    // Drawn with a margin, so effects at the edge see their neighbours, only the middle is used
                    var drawn = new RectD(area.X + left - margin, area.Y + top - margin, width + 2 * margin, height + 2 * margin);
                    Image<Rgba32> image;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        // PARALLEL: the document is immutable and the cache is only used by one render at a time, the UI keeps
                        // working (and shows the adorners at once) while the pixels are drawn
#pragma warning disable RS0030 // R10: documented parallel branch
                        image = await Task.Run(() => DocumentRenderer.Render(document, drawn, _cache, element => !live.Contains(element.Id)));
#pragma warning restore RS0030
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Drawing the document failed", ex);
                        continue;
                    }

                    Log.DebugFormat("Drawn in the background {0}x{1} in {2} ms", width, height, watch.ElapsedMilliseconds);
                    using (image)
                    {
                        // The canvas changed size meanwhile: the new bitmap is drawn completely anyway.
                        // Without the dispatcher of the window (tests) the drawing isn't shown.
                        if (liveVersion != _liveVersion)
                        {
                            // Other elements are live now, this drawing has the wrong ones: again
                            if (!_pendingAll)
                            {
                                _pending = _pending is RectD earlier ? RectD.Union(earlier, draw) : draw;
                            }
                        }
                        else if (area == Area && Bitmap.CheckAccess())
                        {
                            Copy(image, margin, margin, left, top, width, height);
                        }
                    }

                    Rendered?.Invoke();
                }
            }
            finally
            {
                _rendering = false;
            }
        }

        /// <summary>
        /// Draws the part of the bitmap on this thread, without the live elements
        /// </summary>
        private void DrawNow(GreenshotDocument document, RectD changed)
        {
            var area = Area;
            if (Intersect(changed.Inflate(2), area) is not RectD draw)
            {
                return;
            }

            int left = (int)Math.Floor(draw.X - area.X), top = (int)Math.Floor(draw.Y - area.Y);
            int right = Math.Min((int)area.Width, (int)Math.Ceiling(draw.Right - area.X)), bottom = Math.Min((int)area.Height, (int)Math.Ceiling(draw.Bottom - area.Y));
            int width = right - left, height = bottom - top;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var live = _live;
            var drawn = new RectD(area.X + left - Margin, area.Y + top - Margin, width + 2 * Margin, height + 2 * Margin);
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                using var image = DocumentRenderer.Render(document, drawn, _liveCache, element => !live.Contains(element.Id));
                Copy(image, Margin, Margin, left, top, width, height);
                Log.DebugFormat("Drawn at once {0}x{1} in {2} ms", width, height, watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Log.Error("Drawing the document failed", ex);
            }
        }

        /// <summary>
        /// Draws the live elements alone into the overlay
        /// </summary>
        private void DrawOverlay(GreenshotDocument document)
        {
            Overlay = null;
            var live = _live;
            if (live.Count == 0 || LiveBounds(document, live) is not RectD bounds || Intersect(bounds, Area) is not RectD visible)
            {
                return;
            }

            var area = new RectD(Math.Floor(visible.X), Math.Floor(visible.Y), Math.Ceiling(visible.Right) - Math.Floor(visible.X), Math.Ceiling(visible.Bottom) - Math.Floor(visible.Y));
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                using var image = DocumentRenderer.Render(document, area, _liveCache, element => live.Contains(element.Id));
                var pixels = new byte[image.Width * image.Height * 4];
                image.ProcessPixelRows(rows =>
                {
                    for (int y = 0; y < rows.Height; y++)
                    {
                        var row = rows.GetRowSpan(y);
                        int offset = y * rows.Width * 4;
                        foreach (var pixel in row)
                        {
                            pixels[offset++] = pixel.B;
                            pixels[offset++] = pixel.G;
                            pixels[offset++] = pixel.R;
                            pixels[offset++] = pixel.A;
                        }
                    }
                });
                var overlay = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null, pixels, image.Width * 4);
                overlay.Freeze();
                Overlay = overlay;
                OverlayArea = new RectD(area.X, area.Y, image.Width, image.Height);
                Log.DebugFormat("Overlay {0}x{1} drawn in {2} ms", image.Width, image.Height, watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Log.Error("Drawing the live elements failed", ex);
            }
        }

        /// <summary>
        /// Where the live elements draw (with their frames), with the margin the renderer uses
        /// </summary>
        private static RectD? LiveBounds(GreenshotDocument document, ISet<Guid> live)
        {
            if (document == null || live.Count == 0)
            {
                return null;
            }

            RectD? bounds = null;
            foreach (var element in document.Layers.SelectMany(layer => layer.Elements).Where(element => live.Contains(element.Id)))
            {
                bounds = Union(bounds, DocumentChanges.DrawnArea(document, element));
            }

            return bounds;
        }

        private static RectD? Union(RectD? a, RectD? b) => a is RectD first ? b is RectD second ? RectD.Union(first, second) : first : b;

        private void Copy(Image<Rgba32> image, int fromX, int fromY, int toX, int toY, int width, int height)
        {
            if (_row.Length < width * 4)
            {
                _row = new byte[width * 4];
            }

            image.ProcessPixelRows(pixels =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        var source = pixels.GetRowSpan(fromY + y).Slice(fromX, width);
                        for (int x = 0; x < width; x++)
                        {
                            var pixel = source[x];
                            int offset = x * 4;
                            _row[offset] = pixel.B;
                            _row[offset + 1] = pixel.G;
                            _row[offset + 2] = pixel.R;
                            _row[offset + 3] = pixel.A;
                        }

                        Bitmap.WritePixels(new Int32Rect(toX, toY + y, width, 1), _row, width * 4, 0);
                    }
                });
        }

        private static RectD? Intersect(RectD a, RectD b)
        {
            double left = Math.Max(a.X, b.X), top = Math.Max(a.Y, b.Y), right = Math.Min(a.Right, b.Right), bottom = Math.Min(a.Bottom, b.Bottom);
            return right > left && bottom > top ? new RectD(left, top, right - left, bottom - top) : null;
        }

        /// <summary>
        /// Frees the decoded images of resources the document no longer uses
        /// </summary>
        public void Trim(GreenshotDocument document)
        {
            _liveCache.KeepOnly(new HashSet<Guid>(document.Resources.Keys));
            if (!_rendering)
            {
                _cache.KeepOnly(new HashSet<Guid>(document.Resources.Keys));
            }
        }

        public void Dispose()
        {
            // A render which still runs uses the cache, it is freed with the last decoded images by the garbage collector then
            if (!_rendering)
            {
                _cache.Dispose();
            }

            _liveCache.Dispose();
        }
    }
}
