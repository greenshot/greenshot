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
using System.Buffers;

namespace Greenshot.Plugin.RapidOcr.Recognition;

/// <summary>
/// An image as the models read it: 3 bytes per pixel in the order blue, green, red (as OpenCV, which the models were trained with), rows without padding.
/// The pixels are outside the managed heap, the image must be disposed
/// </summary>
internal sealed class BgrImage : IDisposable
{
    private readonly NativeBuffer<byte> _pixels;

    public BgrImage(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "The image is empty");
        Width = width;
        Height = height;
        _pixels = new NativeBuffer<byte>(checked(width * height * 3));
    }

    public BgrImage(int width, int height, ReadOnlySpan<byte> data) : this(width, height)
    {
        if (data.Length != _pixels.Length)
        {
            Dispose();
            throw new ArgumentException("The size doesn't match the pixels", nameof(data));
        }
        data.CopyTo(Data);
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>BGR, row after row, outside the managed heap: only valid until the image is disposed</summary>
    public Span<byte> Data => _pixels.Span;

    public void Dispose() => _pixels.Dispose();

    /// <summary>
    /// Bilinear resize with pixel centers as OpenCV's INTER_LINEAR, a new image (also for the same size)
    /// </summary>
    public BgrImage Resize(int width, int height)
    {
        var result = new BgrImage(width, height);
        var columns = new Columns(Width, width);
        try
        {
            var target = result.Data;
            for (int y = 0; y < height; y++)
            {
                ResampleRow(y, height, columns, target.Slice(y * width * 3, width * 3));
            }
        }
        finally
        {
            columns.Dispose();
        }

        return result;
    }

    /// <summary>
    /// Resized (as <see cref="Resize"/>) into a model input: per pixel (value / 255 - 0.5) / 0.5, one plane per channel (blue, green, red)
    /// </summary>
    /// <param name="tensor">the input of the model</param>
    /// <param name="offset">where the first plane of this image starts</param>
    /// <param name="plane">the size of a plane</param>
    /// <param name="rowLength">the length of a row in the planes, at least width</param>
    /// <param name="width">the width to resize to</param>
    /// <param name="height">the height to resize to</param>
    public void WriteNormalized(Span<float> tensor, int offset, int plane, int rowLength, int width, int height)
    {
        var columns = new Columns(Width, width);
        byte[] row = ArrayPool<byte>.Shared.Rent(width * 3);
        try
        {
            var rowSpan = row.AsSpan(0, width * 3);
            for (int y = 0; y < height; y++)
            {
                ResampleRow(y, height, columns, rowSpan);
                int target = offset + y * rowLength;
                for (int c = 0; c < 3; c++)
                {
                    var channel = tensor.Slice(target + c * plane, width);
                    for (int x = 0; x < width; x++)
                    {
                        channel[x] = (rowSpan[x * 3 + c] / 255f - 0.5f) / 0.5f;
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(row);
            columns.Dispose();
        }
    }

    /// <summary>
    /// The source columns of the target columns of a resize, rented
    /// </summary>
    private readonly struct Columns : IDisposable
    {
        public Columns(int sourceWidth, int width)
        {
            Left = ArrayPool<int>.Shared.Rent(width);
            Right = ArrayPool<int>.Shared.Rent(width);
            Fraction = ArrayPool<double>.Shared.Rent(width);
            double scaleX = (double)sourceWidth / width;
            for (int x = 0; x < width; x++)
            {
                double sx = (x + 0.5) * scaleX - 0.5;
                if (sx < 0) sx = 0;
                int ix = (int)sx;
                if (ix >= sourceWidth - 1)
                {
                    ix = sourceWidth - 1;
                    sx = ix;
                }
                Left[x] = ix * 3;
                Right[x] = Math.Min(ix + 1, sourceWidth - 1) * 3;
                Fraction[x] = sx - ix;
            }
        }

        public int[] Left { get; }

        public int[] Right { get; }

        public double[] Fraction { get; }

        public void Dispose()
        {
            ArrayPool<int>.Shared.Return(Left);
            ArrayPool<int>.Shared.Return(Right);
            ArrayPool<double>.Shared.Return(Fraction);
        }
    }

    /// <summary>
    /// Row y of the image resized to target.Length / 3 x height
    /// </summary>
    private void ResampleRow(int y, int height, Columns columns, Span<byte> target)
    {
        ReadOnlySpan<byte> data = Data;
        double scaleY = (double)Height / height;
        double sy = (y + 0.5) * scaleY - 0.5;
        if (sy < 0) sy = 0;
        int iy = (int)sy;
        if (iy >= Height - 1)
        {
            iy = Height - 1;
            sy = iy;
        }
        double fy = sy - iy;
        var row0 = data.Slice(iy * Width * 3, Width * 3);
        var row1 = data.Slice(Math.Min(iy + 1, Height - 1) * Width * 3, Width * 3);
        int width = target.Length / 3;
        for (int x = 0; x < width; x++)
        {
            double wx = columns.Fraction[x];
            int left = columns.Left[x], right = columns.Right[x];
            for (int c = 0; c < 3; c++)
            {
                double top = row0[left + c] * (1 - wx) + row0[right + c] * wx;
                double bottom = row1[left + c] * (1 - wx) + row1[right + c] * wx;
                target[x * 3 + c] = (byte)Math.Round(top * (1 - fy) + bottom * fy);
            }
        }
    }

    /// <summary>
    /// The image with black rows above and below
    /// </summary>
    public BgrImage PadVertically(int top, int bottom)
    {
        var result = new BgrImage(Width, Height + top + bottom);
        Data.CopyTo(result.Data.Slice(top * Width * 3));
        return result;
    }

    /// <summary>
    /// The parallelogram p0 (top left), p1 (top right), p3 (bottom left) cut out as an upright image of the size, bilinear, the border repeated
    /// </summary>
    public BgrImage Crop(PointD p0, PointD p1, PointD p3, int width, int height)
    {
        var result = new BgrImage(width, height);
        ReadOnlySpan<byte> source = Data;
        var target = result.Data;
        double ux = (p1.X - p0.X) / width, uy = (p1.Y - p0.Y) / width;
        double vx = (p3.X - p0.X) / height, vy = (p3.Y - p0.Y) / height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sx = p0.X + x * ux + y * vx;
                double sy = p0.Y + x * uy + y * vy;
                Sample(source, sx, sy, target.Slice((y * width + x) * 3, 3));
            }
        }

        return result;
    }

    /// <summary>
    /// Turned 90 degrees counter clockwise (as numpy rot90)
    /// </summary>
    public BgrImage RotateCounterClockwise()
    {
        var result = new BgrImage(Height, Width);
        ReadOnlySpan<byte> source = Data;
        var target = result.Data;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // (x, y) goes to (y, Width - 1 - x)
                source.Slice((y * Width + x) * 3, 3).CopyTo(target.Slice(((Width - 1 - x) * Height + y) * 3, 3));
            }
        }

        return result;
    }

    private void Sample(ReadOnlySpan<byte> data, double sx, double sy, Span<byte> target)
    {
        sx = Math.Max(0, Math.Min(Width - 1, sx));
        sy = Math.Max(0, Math.Min(Height - 1, sy));
        int ix = (int)sx, iy = (int)sy;
        int ix1 = Math.Min(ix + 1, Width - 1), iy1 = Math.Min(iy + 1, Height - 1);
        double fx = sx - ix, fy = sy - iy;
        int a = (iy * Width + ix) * 3, b = (iy * Width + ix1) * 3, c = (iy1 * Width + ix) * 3, d = (iy1 * Width + ix1) * 3;
        for (int channel = 0; channel < 3; channel++)
        {
            double top = data[a + channel] * (1 - fx) + data[b + channel] * fx;
            double bottom = data[c + channel] * (1 - fx) + data[d + channel] * fx;
            target[channel] = (byte)Math.Round(top * (1 - fy) + bottom * fy);
        }
    }
}

/// <summary>A point with fractions</summary>
internal readonly struct PointD
{
    public PointD(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public double DistanceTo(PointD other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

    public override string ToString() => $"{X:0.#},{Y:0.#}";
}
