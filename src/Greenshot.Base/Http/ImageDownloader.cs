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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using log4net;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// Downloads an image from a url, e.g. from HTML on the clipboard or dropped on the editor.
    /// </summary>
    public static class ImageDownloader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageDownloader));

        /// <summary>
        /// Download the url to create a Bitmap
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Bitmap, null when it's not possible</returns>
        public static Task<Bitmap> DownloadImageAsync(string url, CancellationToken cancellationToken) =>
            DownloadImageAsync(HttpService.Current, url, cancellationToken);

        /// <summary>
        /// Download the url to build an IDrawableContainer
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>IDrawableContainer, null when it's not possible</returns>
        public static Task<IDrawableContainer> DownloadImageAsDrawableContainerAsync(string url, CancellationToken cancellationToken)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            return DownloadAsync(HttpService.Current, url, (stream, extension) => fileFormatHandlers.LoadDrawablesFromStream(stream, extension).FirstOrDefault(), cancellationToken);
        }

        internal static Task<Bitmap> DownloadImageAsync(HttpService httpService, string url, CancellationToken cancellationToken)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            return DownloadAsync(httpService, url, (stream, extension) => fileFormatHandlers.TryLoadFromStream(stream, extension, out var bitmap) ? bitmap : null, cancellationToken);
        }

        /// <summary>
        /// Download the url, if it's not an image but a page with an image url in its first line, download that.
        /// </summary>
        /// <typeparam name="T">what is loaded from the stream</typeparam>
        /// <param name="httpService">HttpService</param>
        /// <param name="url">Of an image</param>
        /// <param name="load">loads the result from the stream and the extension, null when it's not possible</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>the result or default, errors are logged</returns>
        private static async Task<T> DownloadAsync<T>(HttpService httpService, string url, Func<Stream, string, T> load, CancellationToken cancellationToken) where T : class
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var extensions = string.Join("|", fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadFromStream));

            var imageUrlRegex = new Regex($@"(http|https)://.*(?<extension>{extensions})");
            var match = imageUrlRegex.Match(url);
            try
            {
                string content;
                using (var memoryStream = ToStream(await httpService.GetBytesAsync(url, cancellationToken).ConfigureAwait(false)))
                {
                    try
                    {
                        var result = load(memoryStream, match.Success ? match.Groups["extension"]?.Value : null);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("The download is not an image, checking for an image url", ex);
                    }

                    // If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using var streamReader = new StreamReader(memoryStream, Encoding.UTF8, true);
                    content = await streamReader.ReadLineAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrEmpty(content))
                {
                    return null;
                }

                match = imageUrlRegex.Match(content);
                if (!match.Success)
                {
                    return null;
                }

                using var memoryStream2 = ToStream(await httpService.GetBytesAsync(match.Value, cancellationToken).ConfigureAwait(false));
                return load(memoryStream2, match.Groups["extension"]?.Value);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error("Problem downloading an image from " + HttpService.Describe(TryCreateUri(url)), e);
            }

            return null;
        }

        /// <summary>
        /// Read only, with a visible buffer for the loaders which use GetBuffer
        /// </summary>
        private static MemoryStream ToStream(byte[] bytes) => new MemoryStream(bytes, 0, bytes.Length, false, true);

        private static Uri TryCreateUri(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
    }
}
