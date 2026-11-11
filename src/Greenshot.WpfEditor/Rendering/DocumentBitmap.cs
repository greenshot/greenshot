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
    /// an export). After a change only the changed area is drawn again.
    /// </summary>
    public sealed class DocumentBitmap : IDisposable
    {
        // Blur, pixelate, magnify and shadows near the edge of a changed area need the pixels around it
        private const int Margin = 32;
        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentBitmap));

        private readonly DocumentRenderCache _cache = new();
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
        public void Update(GreenshotDocument document, RectD? changed)
        {
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
                    try
                    {
                        // PARALLEL: the document is immutable and the cache is only used by one render at a time, the UI keeps
                        // working (and shows the adorners at once) while the pixels are drawn
#pragma warning disable RS0030 // R10: documented parallel branch
                        image = await Task.Run(() => DocumentRenderer.Render(document, drawn, _cache));
#pragma warning restore RS0030
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Drawing the document failed", ex);
                        continue;
                    }

                    using (image)
                    {
                        // The canvas changed size meanwhile: the new bitmap is drawn completely anyway.
                        // Without the dispatcher of the window (tests) the drawing isn't shown.
                        if (area == Area && Bitmap.CheckAccess())
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
            if (!_rendering)
            {
                _cache.KeepOnly(new System.Collections.Generic.HashSet<Guid>(document.Resources.Keys));
            }
        }

        public void Dispose()
        {
            // A render which still runs uses the cache, it is freed with the last decoded images by the garbage collector then
            if (!_rendering)
            {
                _cache.Dispose();
            }
        }
    }
}
