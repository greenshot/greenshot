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

#if !GREENSHOT_LIGHT
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Shell32;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using log4net;

namespace Greenshot.Destinations.ImageSearch
{
    /// <summary>
    /// A search engine which finds pages and images for an image. None of them has an official API for an upload from a desktop
    /// program, so each engine does what its own web page does; when an engine changes that, only its class here changes.
    /// Checked 2026-11-21: Bing takes a form post from a local page (the browser sends it, the results open in the user's own session);
    /// Yandex returns the id of the uploaded image, its result page opens by that id. Google Lens shows its cookie consent page
    /// in the EU first and loses the posted image, TinEye answers a post with a Cloudflare check: both are left out.
    /// </summary>
    public abstract class ImageSearchEngine
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(ImageSearchEngine));

        /// <summary>
        /// The engines, the first one is used when the user didn't pick one
        /// </summary>
        public static IReadOnlyList<ImageSearchEngine> All { get; } = new ImageSearchEngine[] { new BingImageSearch(), new YandexImageSearch() };

        /// <summary>
        /// The name of the engine, a brand name, so not translated
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// What the user should know before the capture goes to the engine, besides that it leaves the computer; null for nothing
        /// </summary>
        public virtual string PrivacyNote => null;

        /// <summary>
        /// Send the capture to the engine and open the results in the default browser
        /// </summary>
        public abstract Task<ExportResult> SearchAsync(ExportRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// The capture as PNG; the engines read text and details better from a lossless image
        /// </summary>
        protected static Task<EncodedImage> EncodeAsync(ExportRequest request, CancellationToken cancellationToken) =>
            request.Source.EncodeAsync(new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false).PreventGreenshotFormat(), cancellationToken);

        protected ExportResult Failed(Exception exception = null)
        {
            Log.Warn($"Image search with {Name} failed", exception);
            return ExportResult.Failed(string.Format(Texts.Core.DestinationExportfailed, Name), exception);
        }
    }

    /// <summary>
    /// Bing Visual Search: the browser posts the image (base64 in the field imageBin) to the search page, like Bing's own mobile page.
    /// Greenshot writes a local page with the form, the browser submits it when it opens the page.
    /// </summary>
    internal sealed class BingImageSearch : ImageSearchEngine
    {
        private const string SearchUrl = "https://www.bing.com/images/search?view=detailv2&iss=sbiupload&FORM=SBIHMP";

        public override string Name => "Bing";

        public override async Task<ExportResult> SearchAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var image = await EncodeAsync(request, cancellationToken).ConfigureAwait(false);
            var fields = new Dictionary<string, string>
            {
                ["imageBin"] = Convert.ToBase64String(image.ToArray())
            };
            try
            {
                FormPostPage.Open(SearchUrl, fields);
                return ExportResult.Succeeded(clearsModified: false);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception)
            {
                return Failed(ex);
            }
        }
    }

    /// <summary>
    /// Yandex Images: Greenshot uploads the image (field upfile) like Yandex's own page, the answer names the id of the image,
    /// the browser opens the result page for that id. Yandex keeps the uploaded image under a public address, the result page shows it.
    /// </summary>
    internal sealed class YandexImageSearch : ImageSearchEngine
    {
        private const string Host = "https://yandex.com";
        private const string UploadUrl = Host + "/images/touch/search?rpt=imageview&format=json&request=%7B%22blocks%22%3A%5B%7B%22block%22%3A%22cbir-uploader__get-cbir-id%22%7D%5D%7D";

        public override string Name => "Yandex";

        public override string PrivacyNote => Texts.Core.ImageSearchYandexPublic;

        public override async Task<ExportResult> SearchAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var image = await EncodeAsync(request, cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await request.Ui.RunWithProgressAsync(string.Format(Texts.Core.ImageSearchWait, Name), (progress, token) =>
                {
                    var upload = new HttpRequestMessage(HttpMethod.Post, UploadUrl)
                    {
                        Content = HttpContents.Multipart("upfile", image, "capture" + image.FileExtension, progress: progress)
                    };
                    return HttpService.WithoutRedirects.SendAsync(upload, token);
                }, cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccessStatusCode || !TryGetResultUrl(result.Content, out var resultUrl))
                {
                    Log.WarnFormat("Yandex answered the upload with {0}, without the id of the image", (int)result.StatusCode);
                    return Failed();
                }

                DefaultBrowser.Open(resultUrl);
                return ExportResult.Succeeded(clearsModified: false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TimeoutException || ex is System.ComponentModel.Win32Exception)
            {
                return Failed(ex);
            }
        }

        /// <summary>
        /// The result page for the answer of the upload: {"blocks":[{"params":{"cbirId":"...","originalImageUrl":"https://..."}}]}
        /// </summary>
        internal static bool TryGetResultUrl(string json, out string resultUrl)
        {
            resultUrl = null;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array || blocks.GetArrayLength() == 0 ||
                    !blocks[0].TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object ||
                    !parameters.TryGetProperty("cbirId", out var cbirId) || cbirId.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(cbirId.GetString()))
                {
                    return false;
                }

                resultUrl = $"{Host}/images/search?rpt=imageview&cbir_id={Uri.EscapeDataString(cbirId.GetString())}";
                if (parameters.TryGetProperty("originalImageUrl", out var imageUrl) && imageUrl.ValueKind == JsonValueKind.String &&
                    Uri.TryCreate(imageUrl.GetString(), UriKind.Absolute, out var imageUri) && imageUri.Scheme == Uri.UriSchemeHttps)
                {
                    resultUrl += "&url=" + Uri.EscapeDataString(imageUri.AbsoluteUri);
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
#endif
