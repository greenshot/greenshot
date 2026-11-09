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

namespace Greenshot.Plugin.RapidOcr.Recognition;

/// <summary>
/// An image as the models read it: 3 bytes per pixel in the order blue, green, red (as OpenCV, which the models were trained with), rows without padding
/// </summary>
internal sealed class BgrImage
{
    public BgrImage(int width, int height, byte[] data = null)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "The image is empty");
        Width = width;
        Height = height;
        Data = data ?? new byte[checked(width * height * 3)];
        if (Data.Length != width * height * 3) throw new ArgumentException("The size doesn't match the pixels", nameof(data));
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>BGR, row after row</summary>
    public byte[] Data { get; }

    /// <summary>
    /// Bilinear resize with pixel centers as OpenCV's INTER_LINEAR
    /// </summary>
    public BgrImage Resize(int width, int height)
    {
        if (width == Width && height == Height) return this;
        var result = new BgrImage(width, height);
        double scaleX = (double)Width / width;
        double scaleY = (double)Height / height;
        var x0 = new int[width];
        var x1 = new int[width];
        var fx = new double[width];
        for (int x = 0; x < width; x++)
        {
            double sx = (x + 0.5) * scaleX - 0.5;
            if (sx < 0) sx = 0;
            int ix = (int)sx;
            if (ix >= Width - 1)
            {
                ix = Width - 1;
                sx = ix;
            }
            x0[x] = ix * 3;
            x1[x] = Math.Min(ix + 1, Width - 1) * 3;
            fx[x] = sx - ix;
        }

        for (int y = 0; y < height; y++)
        {
            double sy = (y + 0.5) * scaleY - 0.5;
            if (sy < 0) sy = 0;
            int iy = (int)sy;
            if (iy >= Height - 1)
            {
                iy = Height - 1;
                sy = iy;
            }
            double fy = sy - iy;
            int row0 = iy * Width * 3;
            int row1 = Math.Min(iy + 1, Height - 1) * Width * 3;
            int target = y * width * 3;
            for (int x = 0; x < width; x++)
            {
                double wx = fx[x];
                for (int c = 0; c < 3; c++)
                {
                    double top = Data[row0 + x0[x] + c] * (1 - wx) + Data[row0 + x1[x] + c] * wx;
                    double bottom = Data[row1 + x0[x] + c] * (1 - wx) + Data[row1 + x1[x] + c] * wx;
                    result.Data[target + x * 3 + c] = (byte)Math.Round(top * (1 - fy) + bottom * fy);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// The image with black rows above and below
    /// </summary>
    public BgrImage PadVertically(int top, int bottom)
    {
        var result = new BgrImage(Width, Height + top + bottom);
        Buffer.BlockCopy(Data, 0, result.Data, top * Width * 3, Data.Length);
        return result;
    }

    /// <summary>
    /// The parallelogram p0 (top left), p1 (top right), p3 (bottom left) cut out as an upright image of the size, bilinear, the border repeated
    /// </summary>
    public BgrImage Crop(PointD p0, PointD p1, PointD p3, int width, int height)
    {
        var result = new BgrImage(width, height);
        double ux = (p1.X - p0.X) / width, uy = (p1.Y - p0.Y) / width;
        double vx = (p3.X - p0.X) / height, vy = (p3.Y - p0.Y) / height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sx = p0.X + x * ux + y * vx;
                double sy = p0.Y + x * uy + y * vy;
                Sample(sx, sy, result.Data, (y * width + x) * 3);
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
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // (x, y) goes to (y, Width - 1 - x)
                int source = (y * Width + x) * 3;
                int target = ((Width - 1 - x) * Height + y) * 3;
                result.Data[target] = Data[source];
                result.Data[target + 1] = Data[source + 1];
                result.Data[target + 2] = Data[source + 2];
            }
        }

        return result;
    }

    private void Sample(double sx, double sy, byte[] target, int offset)
    {
        sx = Math.Max(0, Math.Min(Width - 1, sx));
        sy = Math.Max(0, Math.Min(Height - 1, sy));
        int ix = (int)sx, iy = (int)sy;
        int ix1 = Math.Min(ix + 1, Width - 1), iy1 = Math.Min(iy + 1, Height - 1);
        double fx = sx - ix, fy = sy - iy;
        int a = (iy * Width + ix) * 3, b = (iy * Width + ix1) * 3, c = (iy1 * Width + ix) * 3, d = (iy1 * Width + ix1) * 3;
        for (int channel = 0; channel < 3; channel++)
        {
            double top = Data[a + channel] * (1 - fx) + Data[b + channel] * fx;
            double bottom = Data[c + channel] * (1 - fx) + Data[d + channel] * fx;
            target[offset + channel] = (byte)Math.Round(top * (1 - fy) + bottom * fy);
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
