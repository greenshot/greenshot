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

using System.Net;
using Greenshot.Linux.Core.Output;

namespace Greenshot.Linux.Core.Http;

/// <summary>
/// An encoded capture for an upload: the bytes and the format (png, jpg, webp)
/// </summary>
public sealed class EncodedImage(byte[] bytes, string format)
{
    public string Format { get; } = ImageFormats.Normalize(format);

    public string MimeType => ImageFormats.MimeType(Format);

    public string FileExtension => "." + Format;

    public ReadOnlyMemory<byte> Bytes => bytes;

    public int Length => bytes.Length;

    public Stream OpenRead() => new MemoryStream(bytes, false);

    public byte[] ToArray() => (byte[])bytes.Clone();
}

public sealed class ProgressInfo(string? message, double? percentage = null)
{
    public string? Message { get; } = message;

    public double? Percentage { get; } = percentage;
}

/// <summary>
/// The HTTP client of Greenshot (docs/vNext/http.md): the proxy of the system (http_proxy, https_proxy), a user agent,
/// and no automatic redirects, so an upload with a key never goes to a server which wasn't approved
/// </summary>
public static class HttpService
{
    private static readonly Lazy<HttpClient> Client = new(() =>
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            UseProxy = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Greenshot/{typeof(HttpService).Assembly.GetName().Version?.ToString(3) ?? "1.4"} (Linux)");
        return client;
    });

    public static HttpClient Current => Client.Value;

    /// <summary>
    /// Send the request and read the whole answer
    /// </summary>
    public static async Task<HttpResult> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        using var response = await Current.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        string? content = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new HttpResult(response.StatusCode, response.ReasonPhrase ?? string.Empty, response.RequestMessage?.RequestUri ?? request.RequestUri!,
            HttpResult.CollectHeaders(response), content);
    }
}
