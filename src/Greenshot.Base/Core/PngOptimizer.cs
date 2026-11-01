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
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Greenshot.Base.Core;

/// <summary>
/// Writes a PNG as small as possible without changing a single pixel, like optipng does:
/// the smallest color type which still holds every pixel (a palette for up to 256 colors, no alpha channel when nothing is transparent), the best zlib level,
/// and the smaller result of the two filter strategies which win on screenshots (none) and on photos (adaptive).
/// </summary>
public static class PngOptimizer
{
    private const int MaxPaletteColors = 256;

    /// <summary>
    /// Write the bitmap as an optimized PNG to the stream
    /// </summary>
    /// <param name="bitmap">Bitmap to write</param>
    /// <param name="destination">Stream to write to</param>
    public static void Save(Bitmap bitmap, Stream destination)
    {
        using var image = ImageSharpHelper.ConvertToImageSharp(bitmap);
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
        var palette = new Dictionary<Rgba32, byte>();
        bool tooManyColors = false, hasAlpha = false;
        image.ProcessPixelRows(accessor =>
        {
            Rgba32 rgba = default;
            for (int y = 0; y < accessor.Height; y++)
            {
                foreach (var pixel in accessor.GetRowSpan(y))
                {
                    pixel.ToRgba32(ref rgba);
                    hasAlpha |= rgba.A != byte.MaxValue;
                    if (tooManyColors || palette.ContainsKey(rgba))
                    {
                        continue;
                    }
                    if (palette.Count == MaxPaletteColors)
                    {
                        tooManyColors = true;
                        continue;
                    }
                    palette.Add(rgba, (byte)palette.Count);
                }
            }
        });

        var encoder = new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestCompression,
            BitDepth = PngBitDepth.Bit8,
            ColorType = hasAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb
        };
        if (!tooManyColors)
        {
            encoder.ColorType = PngColorType.Palette;
            encoder.BitDepth = palette.Count switch
            {
                <= 2 => PngBitDepth.Bit1,
                <= 4 => PngBitDepth.Bit2,
                <= 16 => PngBitDepth.Bit4,
                _ => PngBitDepth.Bit8
            };
            encoder.Quantizer = new ExactPaletteQuantizer(palette);
        }

        // No filter wins on screenshots, as with optipng. With a palette the adaptive filter gives the same size, so it's only tried for photos.
        encoder.FilterMethod = PngFilterMethod.None;
        using var unfiltered = RecyclableMemoryStreamFactory.GetStream("PngOptimizer.None");
        image.SaveAsPng(unfiltered, encoder);
        if (encoder.ColorType == PngColorType.Palette)
        {
            unfiltered.WriteTo(destination);
            return;
        }
        encoder.FilterMethod = PngFilterMethod.Adaptive;
        using var filtered = RecyclableMemoryStreamFactory.GetStream("PngOptimizer.Adaptive");
        image.SaveAsPng(filtered, encoder);
        (filtered.Length < unfiltered.Length ? filtered : unfiltered).WriteTo(destination);
    }

    /// <summary>
    /// The quantizers of ImageSharp 2.x map colors through a cache with 5 bits per channel, so nearly equal colors can swap.
    /// This maps every color to its own palette entry, it only works for images with all their colors in the palette.
    /// </summary>
    private sealed class ExactPaletteQuantizer : IQuantizer
    {
        private readonly Dictionary<Rgba32, byte> _palette;

        public ExactPaletteQuantizer(Dictionary<Rgba32, byte> palette) => _palette = palette;

        public QuantizerOptions Options { get; } = new() { Dither = null, MaxColors = MaxPaletteColors };

        public IQuantizer<TPixel> CreatePixelSpecificQuantizer<TPixel>(Configuration configuration) where TPixel : unmanaged, IPixel<TPixel>
            => CreatePixelSpecificQuantizer<TPixel>(configuration, Options);

        public IQuantizer<TPixel> CreatePixelSpecificQuantizer<TPixel>(Configuration configuration, QuantizerOptions options) where TPixel : unmanaged, IPixel<TPixel>
            => new PixelQuantizer<TPixel>(configuration, Options, _palette);

        private struct PixelQuantizer<TPixel> : IQuantizer<TPixel> where TPixel : unmanaged, IPixel<TPixel>
        {
            private readonly Dictionary<Rgba32, byte> _indexes;
            private readonly TPixel[] _colors;

            public PixelQuantizer(Configuration configuration, QuantizerOptions options, Dictionary<Rgba32, byte> indexes)
            {
                Configuration = configuration;
                Options = options;
                _indexes = indexes;
                _colors = new TPixel[indexes.Count];
                foreach (var entry in indexes)
                {
                    _colors[entry.Value].FromRgba32(entry.Key);
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
                => QuantizerUtilities.QuantizeFrame(ref this, source, bounds);

            public byte GetQuantizedColor(TPixel color, out TPixel match)
            {
                Rgba32 rgba = default;
                color.ToRgba32(ref rgba);
                byte index = _indexes[rgba];
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
