/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Keeps the icons in the sizes the UI needs. The cache owns the originals and the scaled images and never hands them out:
    /// the UI gets a copy which it owns, so the cache can't dispose an image which is still on the screen (see #962).
    /// </summary>
    public sealed class SizedIconCache
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SizedIconCache));
        private readonly object _lock = new object();
        private readonly Dictionary<string, Image> _originals = new Dictionary<string, Image>();
        private readonly HashSet<string> _missing = new HashSet<string>();
        private readonly Dictionary<string, Bitmap> _scaled = new Dictionary<string, Bitmap>();

        /// <summary>
        /// The cache which the UI uses
        /// </summary>
        public static SizedIconCache Default { get; } = new SizedIconCache();

        /// <summary>
        /// A copy of the icon in the size, the caller owns (disposes) it.
        /// Null when there is no icon, or when it still needs to be loaded with <see cref="LoadAsync"/>.
        /// </summary>
        /// <param name="source">IconSource</param>
        /// <param name="pixelSize">int with the width and height in pixels</param>
        /// <returns>Image or null</returns>
        public Image GetCopy(IconSource source, int pixelSize)
        {
            if (source == null || pixelSize <= 0)
            {
                return null;
            }

            string originalKey = source.OriginalKey(pixelSize);
            string scaledKey = originalKey + "@" + pixelSize;
            lock (_lock)
            {
                if (_scaled.TryGetValue(scaledKey, out var scaled))
                {
                    return new Bitmap(scaled);
                }

                if (!_originals.TryGetValue(originalKey, out var original))
                {
                    if (_missing.Contains(originalKey) || !source.IsSynchronous)
                    {
                        return null;
                    }

                    original = LoadOriginal(source, pixelSize, originalKey);
                    if (original == null)
                    {
                        return null;
                    }
                }

                lock (original)
                {
                    scaled = Scale(original, pixelSize);
                }

                _scaled[scaledKey] = scaled;
                return new Bitmap(scaled);
            }
        }

        /// <summary>
        /// True when the original of the source has to be loaded with <see cref="LoadAsync"/> before <see cref="GetCopy"/> can return it
        /// </summary>
        public bool NeedsLoading(IconSource source, int pixelSize)
        {
            if (source == null || source.IsSynchronous)
            {
                return false;
            }

            string originalKey = source.OriginalKey(pixelSize);
            lock (_lock)
            {
                return !_originals.ContainsKey(originalKey) && !_missing.Contains(originalKey);
            }
        }

        /// <summary>
        /// Load the original of a source which needs time (e.g. exe icons, network)
        /// </summary>
        public async Task LoadAsync(IconSource source, int pixelSize, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                return;
            }

            string originalKey = source.OriginalKey(pixelSize);
            Image image = null;
            try
            {
                image = await source.LoadAsync(pixelSize, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't load the icon {source}", ex);
            }

            lock (_lock)
            {
                if (_originals.ContainsKey(originalKey))
                {
                    // Loaded in the meantime
                    image?.Dispose();
                    return;
                }

                if (image == null)
                {
                    _missing.Add(originalKey);
                    return;
                }

                _originals[originalKey] = image;
            }
        }

        /// <summary>
        /// Remove everything of the source from the cache, for sources which are only used once (an image the UI made itself)
        /// </summary>
        public void Release(IconSource source)
        {
            if (source == null)
            {
                return;
            }

            source.ReleaseOwnedImage();
            string prefix = source.Key;
            lock (_lock)
            {
                foreach (var key in _originals.Keys.Where(k => IsOfSource(k, prefix)).ToList())
                {
                    _originals[key].Dispose();
                    _originals.Remove(key);
                }

                foreach (var key in _scaled.Keys.Where(k => IsOfSource(k, prefix)).ToList())
                {
                    _scaled[key].Dispose();
                    _scaled.Remove(key);
                }

                _missing.RemoveWhere(k => IsOfSource(k, prefix));
            }
        }

        private static bool IsOfSource(string cacheKey, string sourceKey) =>
            cacheKey == sourceKey || cacheKey.StartsWith(sourceKey + "|", StringComparison.Ordinal) || cacheKey.StartsWith(sourceKey + "@", StringComparison.Ordinal);

        private Image LoadOriginal(IconSource source, int pixelSize, string originalKey)
        {
            Image original = null;
            try
            {
                original = source.Load(pixelSize);
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't load the icon {source}", ex);
            }

            if (original == null)
            {
                _missing.Add(originalKey);
                return null;
            }

            _originals[originalKey] = original;
            return original;
        }

        /// <summary>
        /// Scale an icon to a square of the size, centered and keeping the aspect ratio.
        /// Pixel art (all our icons are 16x16) stays sharp: whole multiples use nearest neighbour,
        /// other sizes are first enlarged with nearest neighbour to the next whole multiple and then reduced with a high quality filter.
        /// </summary>
        /// <param name="source">Image</param>
        /// <param name="pixelSize">int with the width and height of the result</param>
        /// <returns>Bitmap, the caller owns it</returns>
        public static Bitmap Scale(Image source, int pixelSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            double factor = Math.Min(pixelSize / (double)source.Width, pixelSize / (double)source.Height);
            int width = Math.Max(1, (int)Math.Round(source.Width * factor));
            int height = Math.Max(1, (int)Math.Round(source.Height * factor));
            bool wholeMultiple = Math.Abs(factor - Math.Round(factor)) < 0.001 && factor >= 1;

            if (wholeMultiple)
            {
                int multiple = (int)Math.Round(factor);
                return Replicate(source, multiple, pixelSize, pixelSize, (pixelSize - width) / 2, (pixelSize - height) / 2);
            }

            Bitmap enlarged = null;
            Image drawSource = source;
            if (factor > 1)
            {
                int multiple = (int)Math.Ceiling(factor);
                enlarged = Replicate(source, multiple, source.Width * multiple, source.Height * multiple, 0, 0);
                drawSource = enlarged;
            }

            try
            {
                return Draw(drawSource, pixelSize, pixelSize, width, height, InterpolationMode.HighQualityBicubic);
            }
            finally
            {
                enlarged?.Dispose();
            }
        }

        /// <summary>
        /// Nearest neighbour by copying every pixel multiple times, exact with every GDI+ implementation (GDI+ itself mixes the border pixels)
        /// </summary>
        private static Bitmap Replicate(Image source, int multiple, int canvasWidth, int canvasHeight, int offsetX, int offsetY)
        {
            using var argbSource = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(argbSource))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
            }

            int sourceWidth = argbSource.Width;
            int sourceHeight = argbSource.Height;
            var sourcePixels = new int[sourceWidth * sourceHeight];
            var sourceData = argbSource.LockBits(new Rectangle(0, 0, sourceWidth, sourceHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < sourceHeight; y++)
                {
                    Marshal.Copy(sourceData.Scan0 + y * sourceData.Stride, sourcePixels, y * sourceWidth, sourceWidth);
                }
            }
            finally
            {
                argbSource.UnlockBits(sourceData);
            }

            var resultPixels = new int[canvasWidth * canvasHeight];
            for (int y = 0; y < sourceHeight * multiple; y++)
            {
                int targetY = y + offsetY;
                if (targetY < 0 || targetY >= canvasHeight)
                {
                    continue;
                }

                for (int x = 0; x < sourceWidth * multiple; x++)
                {
                    int targetX = x + offsetX;
                    if (targetX >= 0 && targetX < canvasWidth)
                    {
                        resultPixels[targetY * canvasWidth + targetX] = sourcePixels[y / multiple * sourceWidth + x / multiple];
                    }
                }
            }

            var result = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppArgb);
            result.SetResolution(IconSizing.DefaultDpi, IconSizing.DefaultDpi);
            var resultData = result.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < canvasHeight; y++)
                {
                    Marshal.Copy(resultPixels, y * canvasWidth, resultData.Scan0 + y * resultData.Stride, canvasWidth);
                }
            }
            finally
            {
                result.UnlockBits(resultData);
            }

            return result;
        }

        private static Bitmap Draw(Image source, int canvasWidth, int canvasHeight, int width, int height, InterpolationMode interpolation)
        {
            var result = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppArgb);
            result.SetResolution(IconSizing.DefaultDpi, IconSizing.DefaultDpi);
            using var graphics = Graphics.FromImage(result);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.InterpolationMode = interpolation;
            using var attributes = new ImageAttributes();
            // Without this the border pixels are mixed with transparent pixels outside of the image
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            var destination = new Rectangle((canvasWidth - width) / 2, (canvasHeight - height) / 2, width, height);
            graphics.DrawImage(source, destination, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            return result;
        }
    }
}
