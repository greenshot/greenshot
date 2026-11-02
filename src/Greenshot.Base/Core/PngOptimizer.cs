/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using Dapplo.Windows.Common.Structs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Image = SixLabors.ImageSharp.Image;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Greenshot.Base.Core;

/// <summary>
/// Writes a PNG as small as possible without changing a single pixel, like optipng does:
/// the smallest color type which still holds every pixel (a palette for up to 256 colors, no alpha channel when nothing is transparent), the best zlib level,
/// and the filter strategy which wins on screenshots (none) or on photos (adaptive), chosen on bands of the image.
/// </summary>
public static class PngOptimizer
{
    private const int MaxPaletteColors = 256;

    // Bands of rows compressed to choose the filter
    private const int ProbeBands = 8;
    private const int ProbeBandRows = 16;

    /// <summary>
    /// Write the bitmap as an optimized PNG to the stream
    /// </summary>
    /// <param name="bitmap">Bitmap to write</param>
    /// <param name="destination">Stream to write to</param>
    public static unsafe void Save(Bitmap bitmap, Stream destination)
    {
        // 32bpp ARGB and 24bpp RGB without row padding are read in place, everything else is converted to a copy first
        bool isArgb = bitmap.PixelFormat == PixelFormat.Format32bppArgb;
        bool isRgb = bitmap.PixelFormat == PixelFormat.Format24bppRgb && bitmap.Width * 3 % 4 == 0;
        if (!isArgb && !isRgb)
        {
            using var copy = ImageSharpHelper.ConvertToImageSharp(bitmap);
            Save(copy, bitmap, destination);
            return;
        }

        var bitmapData = bitmap.LockBits(new NativeRect(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
        try
        {
            using Image image = isArgb
                ? Image.WrapMemory<Bgra32>((void*)bitmapData.Scan0, bitmap.Width, bitmap.Height)
                : Image.WrapMemory<Bgr24>((void*)bitmapData.Scan0, bitmap.Width, bitmap.Height);
            Save(image, bitmap, destination);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static void Save(Image image, Bitmap bitmap, Stream destination)
    {
        image.Metadata.ResolutionUnits = PixelResolutionUnit.PixelsPerInch;
        image.Metadata.HorizontalResolution = bitmap.HorizontalResolution;
        image.Metadata.VerticalResolution = bitmap.VerticalResolution;
        image.Metadata.GetPngMetadata().TextData.Add(new PngTextData("Software", "Greenshot " + EnvironmentInfo.GetGreenshotVersion(true), string.Empty, string.Empty));
        switch (image)
        {
            case Image<Bgr24> bgr24:
                Save(bgr24, destination);
                break;
            case Image<Bgra32> bgra32:
                Save(bgra32, destination);
                break;
            default:
                throw new NotSupportedException($"Unexpected pixel type {image.PixelType}");
        }
    }

    private static void Save<TPixel>(Image<TPixel> image, Stream destination) where TPixel : unmanaged, IPixel<TPixel>
    {
        // Bgr24 has no alpha, for Bgra32 the JIT removes the type checks
        bool checkAlpha = typeof(TPixel) == typeof(Bgra32);
        var palette = new Dictionary<TPixel, byte>(MaxPaletteColors + 1);
        bool tooManyColors = false, hasAlpha = false;
        image.ProcessPixelRows(accessor =>
        {
            TPixel previous = default;
            bool hasPrevious = false;
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref TPixel pixel = ref row[x];
                    if (checkAlpha && !hasAlpha && Unsafe.As<TPixel, Bgra32>(ref pixel).A != byte.MaxValue)
                    {
                        hasAlpha = true;
                    }
                    // Screenshots have long runs of one color, only a new color needs the dictionary
                    if (tooManyColors || (hasPrevious && pixel.Equals(previous)))
                    {
                        continue;
                    }
                    previous = pixel;
                    hasPrevious = true;
                    if (palette.ContainsKey(pixel))
                    {
                        continue;
                    }
                    if (palette.Count == MaxPaletteColors)
                    {
                        tooManyColors = true;
                        continue;
                    }
                    palette.Add(pixel, (byte)palette.Count);
                }
                // Nothing left to find out: no palette, and alpha is found or impossible
                if (tooManyColors && (hasAlpha || !checkAlpha))
                {
                    return;
                }
            }
        });

        if (!tooManyColors)
        {
            var paletteEncoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Palette,
                BitDepth = palette.Count switch
                {
                    <= 2 => PngBitDepth.Bit1,
                    <= 4 => PngBitDepth.Bit2,
                    <= 16 => PngBitDepth.Bit4,
                    _ => PngBitDepth.Bit8
                },
                // With a palette the adaptive filter gives the same size as no filter
                FilterMethod = PngFilterMethod.None,
                Quantizer = new ExactPaletteQuantizer<TPixel>(palette)
            };
            image.SaveAsPng(destination, paletteEncoder);
            return;
        }

        var colorType = hasAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb;
        image.SaveAsPng(destination, new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestCompression,
            BitDepth = PngBitDepth.Bit8,
            ColorType = colorType,
            FilterMethod = ChooseFilter(image, colorType)
        });
    }

    /// <summary>
    /// No filter wins on screenshots, as with optipng, the adaptive filter wins on photos.
    /// Bands spread over the image are compressed quickly with both, so the full image is compressed only once.
    /// One band from the middle isn't enough, it picked the wrong filter for a web page with a picture in the middle.
    /// </summary>
    private static PngFilterMethod ChooseFilter<TPixel>(Image<TPixel> image, PngColorType colorType) where TPixel : unmanaged, IPixel<TPixel>
    {
        int bandHeight = Math.Min(ProbeBandRows, image.Height);
        long unfilteredSize = 0, filteredSize = 0;
        for (int band = 1; band <= ProbeBands; band++)
        {
            int top = Math.Max(0, Math.Min(image.Height - bandHeight, image.Height * band / (ProbeBands + 1) - bandHeight / 2));
            using var bandImage = image.Clone(context => context.Crop(new Rectangle(0, top, image.Width, bandHeight)));
            unfilteredSize += ProbeSize(bandImage, colorType, PngFilterMethod.None);
            filteredSize += ProbeSize(bandImage, colorType, PngFilterMethod.Adaptive);
        }
        return filteredSize < unfilteredSize ? PngFilterMethod.Adaptive : PngFilterMethod.None;
    }

    private static long ProbeSize(Image image, PngColorType colorType, PngFilterMethod filterMethod)
    {
        using var probe = RecyclableMemoryStreamFactory.GetStream("PngOptimizer.Probe");
        image.SaveAsPng(probe, new PngEncoder { CompressionLevel = PngCompressionLevel.Level6, BitDepth = PngBitDepth.Bit8, ColorType = colorType, FilterMethod = filterMethod });
        return probe.Length;
    }

    /// <summary>
    /// The quantizers of ImageSharp 2.x map colors through a cache with 5 bits per channel, so nearly equal colors can swap.
    /// This maps every color to its own palette entry, it only works for images with all their colors in the palette.
    /// </summary>
    private sealed class ExactPaletteQuantizer<TColor> : IQuantizer where TColor : unmanaged, IPixel<TColor>
    {
        private readonly Dictionary<TColor, byte> _palette;

        public ExactPaletteQuantizer(Dictionary<TColor, byte> palette) => _palette = palette;

        public QuantizerOptions Options { get; } = new() { Dither = null, MaxColors = MaxPaletteColors };

        public IQuantizer<TPixel> CreatePixelSpecificQuantizer<TPixel>(Configuration configuration) where TPixel : unmanaged, IPixel<TPixel>
            => CreatePixelSpecificQuantizer<TPixel>(configuration, Options);

        // The encoder asks for the pixel type of the image, which is the type of the palette
        public IQuantizer<TPixel> CreatePixelSpecificQuantizer<TPixel>(Configuration configuration, QuantizerOptions options) where TPixel : unmanaged, IPixel<TPixel>
            => new PixelQuantizer<TPixel>(configuration, Options, (Dictionary<TPixel, byte>)(object)_palette);

        private readonly struct PixelQuantizer<TPixel> : IQuantizer<TPixel> where TPixel : unmanaged, IPixel<TPixel>
        {
            private readonly Dictionary<TPixel, byte> _indexes;
            private readonly TPixel[] _colors;

            public PixelQuantizer(Configuration configuration, QuantizerOptions options, Dictionary<TPixel, byte> indexes)
            {
                Configuration = configuration;
                Options = options;
                _indexes = indexes;
                _colors = new TPixel[indexes.Count];
                foreach (var entry in indexes)
                {
                    _colors[entry.Value] = entry.Key;
                }
            }

            public Configuration Configuration { get; }

            public QuantizerOptions Options { get; }

            public ReadOnlyMemory<TPixel> Palette => _colors;

            public void AddPaletteColors(Buffer2DRegion<TPixel> pixelRegion)
            {
                // The palette is complete from the start
            }

            public IndexedImageFrame<TPixel> QuantizeFrame(ImageFrame<TPixel> source, Rectangle bounds)
            {
                var quantizer = this;
                return QuantizerUtilities.QuantizeFrame(ref quantizer, source, bounds);
            }

            public byte GetQuantizedColor(TPixel color, out TPixel match)
            {
                byte index = _indexes[color];
                match = _colors[index];
                return index;
            }

            public void Dispose()
            {
                // Nothing to dispose
            }
        }
    }
}
