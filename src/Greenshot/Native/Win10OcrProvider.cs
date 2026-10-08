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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using BitmapData = System.Drawing.Imaging.BitmapData;
using WinRtRect = Windows.Foundation.Rect;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// This uses the Windows OcrEngine to perform OCR on the captured image.
    /// The pixels are copied straight into the SoftwareBitmap the engine reads, there is no encoding to a file format in between.
    /// </summary>
    public class Win10OcrProvider : IOcrProvider
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10OcrProvider));
        // Very small images are padded to this size, the engine finds no text on them otherwise
        private const int MinWidth = 130;
        private const int MinHeight = 130;

        /// <summary>
        /// Constructor, logs the available languages when debug logging is on (the provider is created at startup)
        /// </summary>
        public Win10OcrProvider()
        {
            if (Log.IsDebugEnabled)
            {
                LogAvailableLanguages();
            }
        }

        /// <summary>
        /// Separate method, so the OCR engine isn't loaded at startup when nothing is logged
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LogAvailableLanguages()
        {
            foreach (var language in OcrEngine.AvailableRecognizerLanguages)
            {
                Log.DebugFormat("Found language {0} {1}", language.NativeName, language.LanguageTag);
            }
        }

        /// <inheritdoc />
        public IList<OcrLanguage> GetAvailableLanguages() =>
            OcrEngine.AvailableRecognizerLanguages
                .Select(language => new OcrLanguage(language.LanguageTag, language.DisplayName))
                .ToList();

        /// <summary>
        /// Scan the surface for text: only its image (the background) is read, the elements drawn on it are not
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="languageTag">Optional OCR language tag (e.g. en-US, de-DE)</param>
        /// <returns>List of IOcrLineFeature, null when there is no OCR language</returns>
        public Task<List<IOcrLineFeature>> DoOcrAsync(ISurface surface, string languageTag = null) => DoOcrAsync(surface?.Image, languageTag);

        /// <summary>
        /// Scan the Image for text. The pixels are copied before this returns, the image can change while the OCR runs.
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="languageTag">Optional OCR language tag (e.g. en-US, de-DE)</param>
        /// <returns>List of IOcrLineFeature, null when there is no OCR language</returns>
        public async Task<List<IOcrLineFeature>> DoOcrAsync(Image image, string languageTag = null)
        {
            if (image == null)
            {
                return null;
            }

            var ocrEngine = CreateOcrEngine(languageTag);
            if (ocrEngine is null)
            {
                return null;
            }

            var ocrImage = OcrImage.Create(image);
            using (ocrImage.Bitmap)
            {
                var ocrResult = await ocrEngine.RecognizeAsync(ocrImage.Bitmap).AsTask().ConfigureAwait(false);
                return CreateOcrLines(ocrResult, ocrImage, IsWrittenWithoutSpaces(ocrEngine.RecognizerLanguage?.LanguageTag));
            }
        }

        /// <summary>
        /// The engine for the requested language, the configured language, or the languages of the user profile
        /// </summary>
        private static OcrEngine CreateOcrEngine(string languageTag)
        {
            if (string.IsNullOrWhiteSpace(languageTag))
            {
                var win10Config = Dapplo.Ini.IniConfigRegistry.GetSection<Greenshot.Configuration.IWin10Configuration>();
                languageTag = win10Config?.OcrLanguage;
            }

            if (!string.IsNullOrWhiteSpace(languageTag))
            {
                try
                {
                    var winLang = new Windows.Globalization.Language(languageTag.Trim());
                    if (OcrEngine.IsLanguageSupported(winLang))
                    {
                        var ocrEngine = OcrEngine.TryCreateFromLanguage(winLang);
                        if (ocrEngine != null)
                        {
                            return ocrEngine;
                        }
                    }
                    else
                    {
                        Log.WarnFormat("Requested OCR language '{0}' is not currently installed or supported in Windows.", languageTag);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to create OCR engine for language '{languageTag}'", ex);
                }
            }

            return OcrEngine.TryCreateFromUserProfileLanguages();
        }

        /// <summary>
        /// Chinese and Japanese are written without spaces, the engine still puts a space between the words of a line
        /// </summary>
        private static bool IsWrittenWithoutSpaces(string languageTag) =>
            languageTag != null && (languageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || languageTag.StartsWith("ja", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Create the list of IOcrLineFeature, with the bounds in the coordinates of the original image
        /// </summary>
        /// <param name="ocrResult">OcrResult</param>
        /// <param name="ocrImage">OcrImage, how the original image was scaled and padded</param>
        /// <param name="withoutSpaces">true to join the words of a line without spaces</param>
        /// <returns>List of IOcrLineFeature</returns>
        private static List<IOcrLineFeature> CreateOcrLines(OcrResult ocrResult, OcrImage ocrImage, bool withoutSpaces)
        {
            var result = new List<IOcrLineFeature>();

            foreach (var ocrLine in ocrResult.Lines)
            {
                var words = new List<OcrWordInfo>();
                var lineBounds = NativeRect.Empty;

                for (var index = 0; index < ocrLine.Words.Count; index++)
                {
                    var ocrWord = ocrLine.Words[index];
                    var wordBounds = ocrImage.ToImageBounds(ocrWord.BoundingRect);

                    words.Add(new OcrWordInfo
                    {
                        Bounds = wordBounds,
                        Text = ocrWord.Text
                    });

                    lineBounds = index == 0 ? wordBounds : lineBounds.Union(wordBounds);
                }

                if (!lineBounds.IsEmpty)
                {
                    var text = withoutSpaces ? string.Concat(words.Select(word => word.Text)) : ocrLine.Text;
                    result.Add(new DetectedOcrLine(lineBounds, text, words));
                }
            }

            return result;
        }

        /// <summary>
        /// The image the engine reads: a copy of the pixels, scaled down when larger than the engine supports and padded when very small
        /// </summary>
        private sealed class OcrImage
        {
            private OcrImage(SoftwareBitmap bitmap, double scale, int paddingX, int paddingY)
            {
                Bitmap = bitmap;
                Scale = scale;
                PaddingX = paddingX;
                PaddingY = paddingY;
            }

            public SoftwareBitmap Bitmap { get; }

            private double Scale { get; }

            private int PaddingX { get; }

            private int PaddingY { get; }

            /// <summary>
            /// Bounds reported by the engine, in the coordinates of the original image
            /// </summary>
            public NativeRect ToImageBounds(WinRtRect rect)
            {
                int left = (int)Math.Floor((rect.X - PaddingX) / Scale);
                int top = (int)Math.Floor((rect.Y - PaddingY) / Scale);
                int right = (int)Math.Ceiling((rect.X + rect.Width - PaddingX) / Scale);
                int bottom = (int)Math.Ceiling((rect.Y + rect.Height - PaddingY) / Scale);
                return new NativeRect(left, top, right - left, bottom - top);
            }

            public static OcrImage Create(Image image)
            {
                // The engine doesn't take images larger than MaxImageDimension (e.g. several monitors side by side): scale those down
                double maxDimension = OcrEngine.MaxImageDimension;
                double scale = Math.Min(1d, Math.Min(maxDimension / image.Width, maxDimension / image.Height));
                Bitmap ownBitmap = null;
                try
                {
                    Bitmap source;
                    if (scale < 1d)
                    {
                        Log.InfoFormat("Image of {0}x{1} is larger than the OCR supports, scaling it to {2:P0}", image.Width, image.Height, scale);
                        ownBitmap = new Bitmap(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)), PixelFormat.Format32bppPArgb);
                        using var graphics = Graphics.FromImage(ownBitmap);
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(image, new Rectangle(0, 0, ownBitmap.Width, ownBitmap.Height));
                        source = ownBitmap;
                    }
                    else if (image is Bitmap bitmap)
                    {
                        source = bitmap;
                    }
                    else
                    {
                        // A metafile, LockBits needs a bitmap
                        source = ownBitmap = new Bitmap(image);
                    }

                    int paddingX = Math.Max(0, (MinWidth - source.Width) / 2);
                    int paddingY = Math.Max(0, (MinHeight - source.Height) / 2);
                    var softwareBitmap = CopyToSoftwareBitmap(source, paddingX, paddingY);
                    return new OcrImage(softwareBitmap, scale, paddingX, paddingY);
                }
                finally
                {
                    ownBitmap?.Dispose();
                }
            }

            /// <summary>
            /// Copy the pixels row by row into a new SoftwareBitmap, GDI+ converts them to premultiplied BGRA when needed
            /// </summary>
            private static unsafe SoftwareBitmap CopyToSoftwareBitmap(Bitmap source, int paddingX, int paddingY)
            {
                int width = source.Width;
                int height = source.Height;
                var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width + 2 * paddingX, height + 2 * paddingY, BitmapAlphaMode.Premultiplied);
                try
                {
                    BitmapData sourceData = source.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                    try
                    {
                        using var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write);
                        var plane = buffer.GetPlaneDescription(0);
                        using var reference = buffer.CreateReference();
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* target, out _);
                        target += plane.StartIndex;
                        var sourcePixels = (byte*)sourceData.Scan0;

                        if (paddingX > 0 || paddingY > 0)
                        {
                            // Pad with the color of the top left pixel, a border in another color could be read as a character
                            uint padColor = *(uint*)sourcePixels;
                            for (int y = 0; y < plane.Height; y++)
                            {
                                var row = (uint*)(target + y * plane.Stride);
                                for (int x = 0; x < plane.Width; x++)
                                {
                                    row[x] = padColor;
                                }
                            }
                        }

                        long rowBytes = width * 4L;
                        for (int y = 0; y < height; y++)
                        {
                            Buffer.MemoryCopy(sourcePixels + (long)y * sourceData.Stride, target + (long)(y + paddingY) * plane.Stride + paddingX * 4L, rowBytes, rowBytes);
                        }
                    }
                    finally
                    {
                        source.UnlockBits(sourceData);
                    }
                }
                catch
                {
                    softwareBitmap.Dispose();
                    throw;
                }

                return softwareBitmap;
            }
        }

        /// <summary>
        /// Direct access to the memory of a SoftwareBitmap buffer, see https://learn.microsoft.com/windows/uwp/audio-video-camera/imaging#create-or-edit-a-softwarebitmap-programmatically
        /// </summary>
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }
    }
}
