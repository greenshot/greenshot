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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// The icon of a web site, e.g. for the destination of a service: /favicon.ico, else the icon link of the start page
    /// (same scheme and host), optionally the same for the parent domain. Any icon, PNG, GIF or JPEG is returned as PNG of
    /// <see cref="Size"/>. Only the host is logged.
    /// </summary>
    public static class SiteIcon
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SiteIcon));

        /// <summary>The size of the icon</summary>
        public const int Size = 32;

        /// <summary>A favicon or start page larger than this isn't read</summary>
        public const int MaximumLength = 256 * 1024;

        // Usually background work, some sites answer slowly (catbox.moe took 20 seconds and more)
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(45);

        private static readonly Regex LinkTag = new Regex(@"<link\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        private static readonly Regex Attribute = new Regex(@"(?<![\w-])(?<name>rel|href)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s>]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        /// <summary>
        /// The icon of the site as PNG, null when it has none Greenshot can read. Never throws, except for the cancellation.
        /// </summary>
        /// <param name="httpService">HttpService</param>
        /// <param name="site">The site, its path is kept (a Jira at https://example.com/jira/)</param>
        /// <param name="includeParentDomain">Also try the parent domain (example.com for api.example.com), a host the user didn't name</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task<byte[]> FetchAsync(HttpService httpService, Uri site, bool includeParentDomain, CancellationToken cancellationToken)
        {
            if (httpService == null) throw new ArgumentNullException(nameof(httpService));
            if (site == null) throw new ArgumentNullException(nameof(site));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            foreach (var candidate in GetSites(site, includeParentDomain))
            {
                try
                {
                    var png = ToPng(await GetAsync(httpService, new Uri(candidate, "favicon.ico"), timeout.Token).ConfigureAwait(false));
                    if (png != null)
                    {
                        return png;
                    }

                    var page = await GetAsync(httpService, candidate, timeout.Token).ConfigureAwait(false);
                    var link = page == null ? null : FindIconLink(Encoding.UTF8.GetString(page), candidate);
                    png = link == null ? null : ToPng(await GetAsync(httpService, link, timeout.Token).ConfigureAwait(false));
                    if (png != null)
                    {
                        return png;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    Log.InfoFormat("No icon for {0}: it took longer than {1}", candidate.Host, Timeout);
                    return null;
                }
                catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
                {
                    Log.InfoFormat("No icon from {0}: {1}", candidate.Host, ex.GetType().Name);
                }
            }

            return null;
        }

        /// <summary>
        /// The site (its path ending with /), then its parent domain when asked and there is one (not for an IP address or a single label like localhost)
        /// </summary>
        internal static IEnumerable<Uri> GetSites(Uri site, bool includeParentDomain)
        {
            var builder = new UriBuilder(site) { Query = string.Empty, Fragment = string.Empty };
            if (!builder.Path.EndsWith("/", StringComparison.Ordinal))
            {
                builder.Path += "/";
            }

            var server = builder.Uri;
            yield return server;
            if (!includeParentDomain || server.HostNameType != UriHostNameType.Dns)
            {
                yield break;
            }

            var labels = server.Host.Split('.');
            if (labels.Length > 2)
            {
                yield return new UriBuilder(server.Scheme, string.Join(".", labels.Skip(1))).Uri;
            }
        }

        /// <summary>
        /// The href of the first icon link which stays on the same site (scheme and host), null when there is none
        /// </summary>
        internal static Uri FindIconLink(string html, Uri site)
        {
            try
            {
                foreach (Match tag in LinkTag.Matches(html))
                {
                    string rel = null, href = null;
                    foreach (Match attribute in Attribute.Matches(tag.Value))
                    {
                        if (attribute.Groups["name"].Value.Equals("rel", StringComparison.OrdinalIgnoreCase)) rel = attribute.Groups["value"].Value;
                        else href = WebUtility.HtmlDecode(attribute.Groups["value"].Value);
                    }

                    if (rel == null || href == null || !rel.Split(' ').Any(part => part.Equals("icon", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (Uri.TryCreate(site, href, out var link) && link.Scheme == site.Scheme && string.Equals(link.Host, site.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        return link;
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // A page which isn't worth reading
            }

            return null;
        }

        /// <summary>
        /// The image as 32x32 PNG, null when it isn't an icon, PNG, GIF or JPEG Greenshot can read
        /// </summary>
        internal static byte[] ToPng(byte[] bytes)
        {
            string extension = GuessExtension(bytes);
            if (extension == null)
            {
                return null;
            }

            try
            {
                var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
                using var stream = new MemoryStream(bytes, 0, bytes.Length, false, true);
                if (!fileFormatHandlers.TryLoadFromStream(stream, extension, out var loaded) || loaded == null)
                {
                    return null;
                }

                using (loaded)
                using (var icon = new Bitmap(Size, Size, PixelFormat.Format32bppArgb))
                {
                    using (var graphics = Graphics.FromImage(icon))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(loaded, new Rectangle(0, 0, Size, Size));
                    }

                    using var png = new MemoryStream();
                    icon.Save(png, ImageFormat.Png);
                    return png.ToArray();
                }
            }
            catch (Exception ex) when (ex is ArgumentException or ExternalException or OutOfMemoryException or InvalidOperationException)
            {
                // GDI+ reports a broken image as OutOfMemory or ArgumentException
                Log.DebugFormat("The icon can't be read: {0}", ex.GetType().Name);
                return null;
            }
        }

        /// <summary>
        /// The extension of the image by its first bytes, not by what the server says it is
        /// </summary>
        private static string GuessExtension(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8 || bytes.Length > MaximumLength) return null;
            if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0) return ".ico";
            if (bytes[0] == 0x89 && bytes[1] == 'P' && bytes[2] == 'N' && bytes[3] == 'G') return ".png";
            if (bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == '8') return ".gif";
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";
            return null;
        }

        /// <summary>
        /// The content, null for an error status or when it's too large
        /// </summary>
        private static async Task<byte[]> GetAsync(HttpService httpService, Uri uri, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await httpService.GetBytesAsync(uri.AbsoluteUri, cancellationToken).ConfigureAwait(false);
                if (bytes.Length > MaximumLength)
                {
                    Log.InfoFormat("No icon from {0}: {1} bytes are too many", uri.Host, bytes.Length);
                    return null;
                }

                return bytes;
            }
            catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException)
            {
                // 404 and the like, or no connection: no icon there. Logged: the host and the status, not the path (http.md)
                Log.InfoFormat("No icon from {0}: {1}", uri.Host, ex.Message);
                return null;
            }
        }
    }
}
