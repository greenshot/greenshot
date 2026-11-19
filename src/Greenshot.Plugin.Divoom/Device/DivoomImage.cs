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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Greenshot.Base.Core;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Plugin.Divoom.Device;

/// <summary>
/// The pixels of a picture for the screen of a device
/// </summary>
public static class DivoomImage
{
    /// <summary>
    /// The sizes of the screens of the devices
    /// </summary>
    public static readonly int[] PanelSizes = { 16, 32, 64 };

    /// <summary>
    /// The size of the screen, 64 for a value which isn't one
    /// </summary>
    public static int NormalizeSize(int size) => Array.IndexOf(PanelSizes, size) >= 0 ? size : 64;

    /// <summary>
    /// The RGB bytes of the image for a screen of size x size pixels, row by row from the top left: an image of that size
    /// as it is, another one fits into it in the middle (black around it), transparency over black like the dark screen
    /// </summary>
    public static byte[] ToRgb(Image image, int size)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Black);
            var target = FitInto(image.Width, image.Height, size);
            bool sameSize = target.Width == image.Width && target.Height == image.Height;
            // Pixel for pixel when it has the size, else smooth
            graphics.InterpolationMode = sameSize ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            using var attributes = new ImageAttributes();
            // No faded edges from the pixels outside of the image
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, target, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
        }

        var rgb = new byte[size * size * 3];
        BitmapPixels.ProcessPixelRows<Bgra32>(bitmap, pixels =>
        {
            int index = 0;
            for (int y = 0; y < pixels.Height; y++)
            {
                var row = pixels.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    rgb[index++] = row[x].R;
                    rgb[index++] = row[x].G;
                    rgb[index++] = row[x].B;
                }
            }
        }, ImageLockMode.ReadOnly);
        return rgb;
    }

    /// <summary>
    /// Where an image of width x height goes on a square of size: as large as fits, keeping its aspect, in the middle
    /// </summary>
    public static Rectangle FitInto(int width, int height, int size)
    {
        if (width <= 0 || height <= 0)
        {
            return Rectangle.Empty;
        }
        if (width == size && height == size)
        {
            return new Rectangle(0, 0, size, size);
        }
        double scale = Math.Min((double)size / width, (double)size / height);
        int targetWidth = Math.Max(1, (int)Math.Round(width * scale));
        int targetHeight = Math.Max(1, (int)Math.Round(height * scale));
        return new Rectangle((size - targetWidth) / 2, (size - targetHeight) / 2, targetWidth, targetHeight);
    }

    /// <summary>
    /// A test picture: the colors of the edges and a white frame, so it shows that every pixel arrives the right way round
    /// </summary>
    public static byte[] CreateTestPicture(int size)
    {
        var rgb = new byte[size * size * 3];
        int index = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool frame = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                rgb[index++] = frame ? (byte)255 : (byte)(255 * x / (size - 1));
                rgb[index++] = frame ? (byte)255 : (byte)(255 * y / (size - 1));
                rgb[index++] = frame ? (byte)255 : (byte)(255 - 255 * x / (size - 1));
            }
        }
        return rgb;
    }
}
