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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Document.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using log4net;

namespace Greenshot.WpfEditor.Rendering
{
    /// <summary>
    /// One bitmap of the composition: what an element draws, or what an effect makes of everything below it
    /// </summary>
    /// <param name="Id">The element</param>
    /// <param name="Bitmap">The pixels, frozen</param>
    /// <param name="Area">Where the bitmap is, in canvas pixels</param>
    public sealed record CompositionPiece(Guid Id, BitmapSource Bitmap, RectD Area);

    /// <summary>
    /// The document on the screen as a stack of small bitmaps, which WPF blends on the graphics card: every element is drawn
    /// alone by the document renderer (the code that exports) into a bitmap of its size. A moved element keeps its bitmap,
    /// only its place changes; a changed element draws only its own bitmap again.
    /// Effects which change the pixels below them (highlight, obfuscate, a frame around everything) are a bitmap of
    /// everything below them with the effect, drawn again when something below them changes.
    /// </summary>
    public sealed class DocumentComposition : IDisposable
    {
        // Blur, pixelate and magnify near the edge of their area need the pixels around it
        private const int EffectMargin = 32;

        // Bigger bitmaps (the capture, effects over the whole canvas) are drawn in the background
        private const long DrawAtOnceLimit = 1_000_000;

        private static readonly ILog Log = LogManager.GetLogger(typeof(DocumentComposition));

        // One cache for the drawings on the UI thread, one for the background, a cache is used by one drawing at a time
        private readonly DocumentRenderCache _cache = new();
        private readonly DocumentRenderCache _backgroundCache = new();
        private readonly Queue<Entry> _backgroundQueue = new();
        private List<Entry> _entries = new();
        private bool _drawingInBackground;
        private bool _disposed;

        /// <summary>
        /// What a bitmap was drawn from: the element (with its frame and number), for an effect also everything below it
        /// </summary>
        private sealed class Entry
        {
            public Guid Id;
            public Element Element;
            public FrameElement Frame;
            public int StepNumber;
            public IReadOnlyList<Element> Below;
            public RectD Area;
            public CompositionPiece Piece;
            public GreenshotDocument Document;
            // Drawing it failed (e.g. a broken image): not tried again until it changes
            public bool Failed;

            public bool IsEffect => Below != null;
        }

        /// <summary>
        /// The canvas in canvas pixels
        /// </summary>
        public RectD Area { get; private set; }

        /// <summary>
        /// The bitmaps in drawing order
        /// </summary>
        public IReadOnlyList<CompositionPiece> Pieces { get; private set; } = Array.Empty<CompositionPiece>();

        /// <summary>
        /// Raised on the UI thread when the bitmaps or the canvas changed
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// Raised when the size or the place of the canvas changed
        /// </summary>
        public event Action AreaChanged;

        /// <summary>
        /// Shows the document: what didn't change keeps its bitmap, the rest is drawn again. Call it on the UI thread.
        /// </summary>
        public void Update(GreenshotDocument document)
        {
            var canvas = document.CanvasArea();
            var area = new RectD(Math.Floor(canvas.X), Math.Floor(canvas.Y), Math.Max(1, Math.Ceiling(canvas.Width)), Math.Max(1, Math.Ceiling(canvas.Height)));
            bool areaChanged = area != Area;
            Area = area;

            var old = _entries.GroupBy(entry => entry.Id).ToDictionary(group => group.Key, group => group.First());
            var entries = new List<Entry>();
            var drawn = new List<Element>();
            int stepNumber = 0;
            foreach (var element in document.Layers.Where(layer => layer.Visible).SelectMany(layer => layer.Elements))
            {
                if (element is FrameElement { Target: not null })
                {
                    // Drawn with its target
                    drawn.Add(element);
                    continue;
                }

                var entry = new Entry
                {
                    Id = element.Id,
                    Element = element,
                    Frame = document.FrameOf(element),
                    StepNumber = element is StepLabelElement ? ++stepNumber : 0,
                    Document = document
                };
                if (IsEffect(element))
                {
                    entry.Area = element is HighlightElement { Mode: HighlightMode.Area or HighlightMode.Grayscale } or FrameElement { Target: null }
                        ? area
                        : DocumentChanges.DrawnArea(document, element);
                    entry.Below = drawn.Where(below => Intersects(DocumentChanges.DrawnArea(document, below), entry.Area)).ToList();
                }
                else
                {
                    entry.Area = DocumentChanges.DrawnArea(document, element);
                }

                drawn.Add(element);
                if (Intersect(entry.Area, area) is not RectD visible)
                {
                    continue;
                }

                entry.Area = PixelArea(visible);
                entry.Piece = old.TryGetValue(element.Id, out var before) ? Reuse(before, entry) : null;
                if (before is { Failed: true } && ReferenceEquals(before.Element, element) && before.Area == entry.Area)
                {
                    entry.Failed = true;
                }
                else if (entry.Piece == null)
                {
                    // Until it is drawn again, the old bitmap is shown
                    var keep = before?.Piece;
                    if (entry.Area.Width * entry.Area.Height <= DrawAtOnceLimit)
                    {
                        entry.Piece = Draw(entry, _cache);
                        entry.Failed = entry.Piece == null;
                    }
                    else
                    {
                        entry.Piece = keep;
                        _backgroundQueue.Enqueue(entry);
                    }
                }

                entries.Add(entry);
            }

            _entries = entries;
            Pieces = entries.Where(entry => entry.Piece != null).Select(entry => entry.Piece).ToList();
            if (areaChanged)
            {
                AreaChanged?.Invoke();
            }

            Changed?.Invoke();
            if (!_drawingInBackground && _backgroundQueue.Count > 0)
            {
                _ = DrawInBackgroundAsync();
            }
        }

        private static bool IsEffect(Element element) => element is HighlightElement or ObfuscateElement or FrameElement { Target: null };

        /// <summary>
        /// The bitmap of the element before, when the element is the same or only moved by whole pixels
        /// </summary>
        private static CompositionPiece Reuse(Entry before, Entry now)
        {
            if (before.Piece == null || before.StepNumber != now.StepNumber || before.IsEffect != now.IsEffect)
            {
                return null;
            }

            if (now.IsEffect)
            {
                return ReferenceEquals(before.Element, now.Element) && before.Area == now.Area && before.Below.Count == now.Below.Count
                       && before.Below.Zip(now.Below, ReferenceEquals).All(same => same)
                    ? before.Piece
                    : null;
            }

            if (ReferenceEquals(before.Element, now.Element) && ReferenceEquals(before.Frame, now.Frame) && before.Area == now.Area)
            {
                return before.Piece;
            }

            // Moved: the same bitmap in another place
            double dx = now.Element.Bounds.X - before.Element.Bounds.X, dy = now.Element.Bounds.Y - before.Element.Bounds.Y;
            if (dx != Math.Round(dx) || dy != Math.Round(dy) || before.Frame != null || now.Frame != null)
            {
                return null;
            }

            var moved = before.Piece.Area.Offset(dx, dy);
            bool sameElement = Equals(ElementShapes.Unanchored(Anchors.Move(before.Element, dx, dy)), ElementShapes.Unanchored(now.Element));
            // Only when the bitmap wasn't cut by the edge of the canvas, else a part would be missing
            return sameElement && before.Piece.Area == before.Area && moved == now.Area ? before.Piece with { Area = moved } : null;
        }

        private CompositionPiece Draw(Entry entry, DocumentRenderCache cache)
        {
            var document = entry.Document;
            var area = entry.Area;
            int margin = entry.IsEffect ? EffectMargin : 0;
            var below = entry.IsEffect ? new HashSet<Element>(entry.Below) { entry.Element } : null;
            Func<Element, bool> include = entry.IsEffect
                ? element => below.Contains(element)
                : element => element.Id == entry.Id;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                using var image = DocumentRenderer.Render(document, area.Inflate(margin), cache, include);
                var piece = new CompositionPiece(entry.Id, ToBitmap(image, margin, (int)area.Width, (int)area.Height), area);
                Log.DebugFormat("Drawn {0} {1}x{2} in {3} ms", entry.Element.GetType().Name, (int)area.Width, (int)area.Height, watch.ElapsedMilliseconds);
                return piece;
            }
            catch (Exception ex)
            {
                Log.Error($"Drawing the {entry.Element.GetType().Name} failed", ex);
                return null;
            }
        }

        private async Task DrawInBackgroundAsync()
        {
            _drawingInBackground = true;
            try
            {
                while (_backgroundQueue.Count > 0 && !_disposed)
                {
                    var entry = _backgroundQueue.Dequeue();
                    if (!_entries.Contains(entry))
                    {
                        // Replaced by a newer change meanwhile
                        continue;
                    }

                    // PARALLEL: the document is immutable and the background cache is only used here, one drawing at a time;
                    // the UI keeps working while a big bitmap (the capture) is drawn
#pragma warning disable RS0030 // R10: documented parallel branch
                    var piece = await Task.Run(() => Draw(entry, _backgroundCache));
#pragma warning restore RS0030
                    entry.Failed = piece == null;
                    if (piece == null || !_entries.Contains(entry))
                    {
                        continue;
                    }

                    entry.Piece = piece;
                    Pieces = _entries.Where(e => e.Piece != null).Select(e => e.Piece).ToList();
                    Changed?.Invoke();
                }
            }
            finally
            {
                _drawingInBackground = false;
            }
        }

        /// <summary>
        /// The middle of the image (without the margin) as WPF bitmap
        /// </summary>
        private static BitmapSource ToBitmap(Image<Rgba32> image, int margin, int width, int height)
        {
            int stride = width * 4;
            var pixels = new byte[stride * height];
            image.ProcessPixelRows(rows =>
            {
                for (int y = 0; y < height; y++)
                {
                    var row = rows.GetRowSpan(y + margin).Slice(margin, width);
                    int offset = y * stride;
                    foreach (var pixel in row)
                    {
                        pixels[offset++] = pixel.B;
                        pixels[offset++] = pixel.G;
                        pixels[offset++] = pixel.R;
                        pixels[offset++] = pixel.A;
                    }
                }
            });

            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            bitmap.Freeze();
            return bitmap;
        }

        private static RectD PixelArea(RectD area)
        {
            double left = Math.Floor(area.X), top = Math.Floor(area.Y);
            return new RectD(left, top, Math.Max(1, Math.Ceiling(area.Right) - left), Math.Max(1, Math.Ceiling(area.Bottom) - top));
        }

        private static bool Intersects(RectD a, RectD b) => Intersect(a, b) != null;

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
            var keep = new HashSet<Guid>(document.Resources.Keys);
            _cache.KeepOnly(keep);
            if (!_drawingInBackground)
            {
                _backgroundCache.KeepOnly(keep);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _cache.Dispose();
            // A drawing which still runs uses the background cache, it is freed with its images by the garbage collector then
            if (!_drawingInBackground)
            {
                _backgroundCache.Dispose();
            }
        }
    }
}
