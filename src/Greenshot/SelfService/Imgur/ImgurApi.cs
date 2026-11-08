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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using Greenshot.Base.Http;

namespace Greenshot.SelfService.Imgur
{
    /// <summary>
    /// The calls to Imgur for the earlier anonymous uploads: their details, thumbnail and deletion.
    /// Imgur only lets the client which uploaded an anonymous image delete it, so this needs Greenshot's client id.
    /// </summary>
    public static class ImgurApi
    {
        private const string ApiUrl = "https://api.imgur.com/3/image/";

        /// <summary>
        /// The title and upload time of an image, null when Imgur no longer has it (deleted by the user or Imgur).
        /// </summary>
        public static async Task<(string Title, DateTime? Uploaded)?> GetDetailsAsync(string hash, CancellationToken cancellationToken)
        {
            var result = await HttpService.Current.SendAsync(CreateRequest(HttpMethod.Get, ApiUrl + hash + ".xml"), cancellationToken).ConfigureAwait(false);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            result.EnsureSuccess();
            var document = new XmlDocument();
            document.LoadXml(result.Content);
            string title = document.GetElementsByTagName("title").Item(0)?.InnerText;
            DateTime? uploaded = null;
            if (long.TryParse(document.GetElementsByTagName("datetime").Item(0)?.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds))
            {
                uploaded = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
            }

            return (title, uploaded);
        }

        /// <summary>
        /// The small square thumbnail, it doesn't need the client id.
        /// </summary>
        public static async Task<BitmapSource> GetThumbnailAsync(string hash, CancellationToken cancellationToken)
        {
            byte[] bytes = await HttpService.Current.GetBytesAsync(ThumbnailUrl(hash), cancellationToken).ConfigureAwait(false);
            var thumbnail = new BitmapImage();
            thumbnail.BeginInit();
            thumbnail.CacheOption = BitmapCacheOption.OnLoad;
            thumbnail.StreamSource = new MemoryStream(bytes);
            thumbnail.EndInit();
            thumbnail.Freeze();
            return thumbnail;
        }

        /// <summary>
        /// Delete the image on Imgur, "bad request" means it was already deleted.
        /// </summary>
        public static async Task<bool> DeleteAsync(string deleteHash, CancellationToken cancellationToken)
        {
            var result = await HttpService.Current.SendAsync(CreateRequest(HttpMethod.Delete, ApiUrl + deleteHash + ".xml"), cancellationToken).ConfigureAwait(false);
            if (result.StatusCode != HttpStatusCode.BadRequest)
            {
                result.EnsureSuccess();
            }

            return true;
        }

        public static string PageUrl(string hash) => "https://imgur.com/" + hash;

        public static string ThumbnailUrl(string hash) => $"https://i.imgur.com/{hash}s.jpg";

        public static string DeleteUrl(string deleteHash) => "https://imgur.com/delete/" + deleteHash;

        /// <summary>
        /// A web page with the uploads, which the user opens in a browser: a row per image with its date, thumbnail, link and delete link.
        /// </summary>
        public static string CreateExport(IEnumerable<(string Hash, string DeleteHash, string Title, DateTime? Uploaded)> uploads, string title, string introduction, string deleteText)
        {
            static string Encode(string text) => WebUtility.HtmlEncode(text ?? string.Empty);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset=\"utf-8\">");
            html.AppendLine($"<title>{Encode(title)}</title>");
            html.AppendLine("<style>body{font-family:'Segoe UI',sans-serif;margin:2em}td{padding:6px 12px;vertical-align:middle}img{width:90px;height:90px;object-fit:cover}</style>");
            html.AppendLine("</head><body>");
            html.AppendLine($"<h1>{Encode(title)}</h1>");
            html.AppendLine($"<p>{Encode(introduction)}</p>");
            html.AppendLine("<table>");
            foreach (var upload in uploads)
            {
                string page = Encode(PageUrl(upload.Hash));
                string date = upload.Uploaded?.ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;
                html.AppendLine("<tr>");
                html.AppendLine($"<td><a href=\"{page}\"><img src=\"{Encode(ThumbnailUrl(upload.Hash))}\" alt=\"\"></a></td>");
                html.AppendLine($"<td>{Encode(date)}<br>{Encode(upload.Title)}<br><a href=\"{page}\">{page}</a></td>");
                html.AppendLine($"<td><a href=\"{Encode(DeleteUrl(upload.DeleteHash))}\">{Encode(deleteText)}</a></td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table></body></html>");
            return html.ToString();
        }

        private static HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Client-ID " + ImgurCredentials.ClientId);
            return request;
        }
    }
}
