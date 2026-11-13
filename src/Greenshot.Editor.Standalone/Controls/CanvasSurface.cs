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
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Greenshot.Document;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Standalone.Controls
{
    /// <summary>
    /// The pixels of a document on the screen: tiles of bitmaps which the document renderer writes into. A change of an
    /// element only writes (and uploads to the graphics card) the tiles it touches, not the whole canvas.
    /// </summary>
    internal sealed class CanvasSurface : IDisposable
    {
        public const int TileSize = 512;
        private readonly WriteableBitmap[] _tiles;
        private readonly int _columns;
        private readonly int _rows;

        /// <param name="area">The canvas of the document, in whole pixels</param>
        public CanvasSurface(RectD area)
        {
            Area = area;
            Width = Math.Max(1, (int)Math.Ceiling(area.Width));
            Height = Math.Max(1, (int)Math.Ceiling(area.Height));
            _columns = (Width + TileSize - 1) / TileSize;
            _rows = (Height + TileSize - 1) / TileSize;
            _tiles = new WriteableBitmap[_columns * _rows];
        }

        /// <summary>
        /// The canvas the surface shows, in the coordinates of the elements
        /// </summary>
        public RectD Area { get; }

        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// Writes the image into the surface, its top left pixel at the pixel (left, top) of the surface
        /// </summary>
        public unsafe void Write(SixLabors.ImageSharp.Image<Rgba32> image, int left, int top)
        {
            int right = Math.Min(Width, left + image.Width), bottom = Math.Min(Height, top + image.Height);
            int fromX = Math.Max(0, left), fromY = Math.Max(0, top);
            if (right <= fromX || bottom <= fromY)
            {
                return;
            }

            for (int row = fromY / TileSize; row <= (bottom - 1) / TileSize; row++)
            {
                for (int column = fromX / TileSize; column <= (right - 1) / TileSize; column++)
                {
                    int tileX = column * TileSize, tileY = row * TileSize;
                    var tile = _tiles[row * _columns + column] ??= new WriteableBitmap(
                        new PixelSize(Math.Min(TileSize, Width - tileX), Math.Min(TileSize, Height - tileY)), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
                    using var buffer = tile.Lock();
                    // The part of the image which lies in this tile
                    int x0 = Math.Max(fromX, tileX), x1 = Math.Min(right, tileX + buffer.Size.Width);
                    int y0 = Math.Max(fromY, tileY), y1 = Math.Min(bottom, tileY + buffer.Size.Height);
                    nint address = buffer.Address;
                    int rowBytes = buffer.RowBytes;
                    image.ProcessPixelRows(rows =>
                    {
                        for (int y = y0; y < y1; y++)
                        {
                            var source = MemoryMarshal.AsBytes(rows.GetRowSpan(y - top).Slice(x0 - left, x1 - x0));
                            source.CopyTo(new Span<byte>((byte*)address + (y - tileY) * rowBytes + (x0 - tileX) * 4, source.Length));
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Draws the tiles which are visible
        /// </summary>
        /// <param name="context">Where to draw</param>
        /// <param name="origin">The top left of the canvas on the screen</param>
        /// <param name="zoom">Screen pixels per canvas pixel</param>
        /// <param name="visible">The part of the screen which is shown</param>
        /// <param name="scaling">Device pixels per screen pixel: the tiles are placed on whole device pixels, so no seam shows between them</param>
        public void Draw(DrawingContext context, Point origin, double zoom, Rect visible, double scaling)
        {
            double Snap(double value) => Math.Round(value * scaling) / scaling;
            for (int row = 0; row < _rows; row++)
            {
                double top = Snap(origin.Y + row * TileSize * zoom), bottom = Snap(origin.Y + Math.Min(Height, (row + 1) * TileSize) * zoom);
                if (bottom < visible.Y || top > visible.Bottom)
                {
                    continue;
                }

                for (int column = 0; column < _columns; column++)
                {
                    double left = Snap(origin.X + column * TileSize * zoom), right = Snap(origin.X + Math.Min(Width, (column + 1) * TileSize) * zoom);
                    if (right < visible.X || left > visible.Right || _tiles[row * _columns + column] is not WriteableBitmap tile)
                    {
                        continue;
                    }

                    context.DrawImage(tile, new Rect(0, 0, tile.PixelSize.Width, tile.PixelSize.Height), new Rect(left, top, right - left, bottom - top));
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i]?.Dispose();
                _tiles[i] = null;
            }
        }
    }
}
