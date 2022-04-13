/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// This uses the OcrEngine from Windows 10 to perform OCR on the captured image.
    /// </summary>
    public class Win10OcrProvider : IOcrProvider
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10OcrProvider));
        private const int MinWidth = 130;
        private const int MinHeight = 130;

        /// <summary>
        /// Constructor, this is only debug information
        /// </summary>
        public Win10OcrProvider()
        {
            var languages = OcrEngine.AvailableRecognizerLanguages;
            foreach (var language in languages)
            {
                Log.DebugFormat("Found language {0} {1}", language.NativeName, language.LanguageTag);
            }
        }

        /// <summary>
        /// Scan the surface bitmap for text, and get the OcrResult
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <returns>OcrResult sync</returns>
        public async Task<OcrInformation> DoOcrAsync(ISurface surface)
        {
            OcrInformation result;
            using (var imageStream = new MemoryStream())
            {
                // We only want the background
                var outputSettings = new SurfaceOutputSettings(OutputFormat.png, 0, true)
                {
                    ReduceColors = true,
                    SaveBackgroundOnly = true
                };
                // Force Grayscale output
                outputSettings.Effects.Add(new GrayscaleEffect());
                if (surface.Image.Width < MinWidth || surface.Image.Height < MinHeight)
                {
                    int addedWidth = MinWidth - surface.Image.Width;
                    if (addedWidth < 0)
                    {
                        addedWidth = 0;
                    }
                    else
                    {
                        addedWidth /= 2;
                    }
                    int addedHeight = MinHeight - surface.Image.Height;
                    if (addedHeight < 0)
                    {
                        addedHeight = 0;
                    }
                    else
                    {
                        addedHeight /= 2;
                    }
                    IEffect effect = new ResizeCanvasEffect(addedWidth, addedWidth, addedHeight, addedHeight);
                    outputSettings.Effects.Add(effect);
                }
                ImageIO.SaveToStream(surface, imageStream, outputSettings);
                imageStream.Position = 0;
                var randomAccessStream = imageStream.AsRandomAccessStream();

                result = await DoOcrAsync(randomAccessStream);
            }

            return result;
        }

        /// <summary>
        /// Scan the Image for text, and get the OcrResult
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>OcrResult sync</returns>
        public async Task<OcrInformation> DoOcrAsync(Image image)
        {
            OcrInformation result;
            using (var imageStream = new MemoryStream())
            {
                ImageIO.SaveToStream(image, null, imageStream, new SurfaceOutputSettings());
                imageStream.Position = 0;
                var randomAccessStream = imageStream.AsRandomAccessStream();

                result = await DoOcrAsync(randomAccessStream);
            }

            return result;
        }

        /// <summary>
        /// Scan the surface bitmap for text, and get the OcrResult
        /// </summary>
        /// <param name="randomAccessStream">IRandomAccessStream</param>
        /// <returns>OcrResult sync</returns>
        public async Task<OcrInformation> DoOcrAsync(IRandomAccessStream randomAccessStream)
        {
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (ocrEngine is null)
            {
                return null;
            }

            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            return CreateOcrInformation(ocrResult);
        }

        /// <summary>
        /// Create the OcrInformation
        /// </summary>
        /// <param name="ocrResult">OcrResult</param>
        /// <returns>OcrInformation</returns>
        private static OcrInformation CreateOcrInformation(OcrResult ocrResult)
        {
            var result = new OcrInformation();

            foreach (var ocrLine in ocrResult.Lines)
            {
                var line = new Line(ocrLine.Words.Count)
                {
                    Text = ocrLine.Text
                };

                result.Lines.Add(line);

                for (var index = 0; index < ocrLine.Words.Count; index++)
                {
                    var ocrWord = ocrLine.Words[index];
                    var location = new NativeRect((int) ocrWord.BoundingRect.X, (int) ocrWord.BoundingRect.Y,
                        (int) ocrWord.BoundingRect.Width, (int) ocrWord.BoundingRect.Height);

                    var word = line.Words[index];
                    word.Text = ocrWord.Text;
                    word.Bounds = location;
                }
            }

            return result;
        }
    }
}