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

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Greenshot.Linux.Core.Configuration;

namespace Greenshot.Linux.Core.Http;

/// <summary>
/// An OAuth 2 service: the urls and the app credentials. With the Greenshot app (the website redirect) the code comes back
/// through getgreenshot.org, which posts it to Greenshot on localhost (the JsonReceiver of Windows); with an own app the
/// browser is sent to http://127.0.0.1:port/authorize directly.
/// </summary>
public sealed record OAuth2Service(
    string Name,
    string AuthorizeUrl,
    string TokenUrl,
    string ClientId,
    string ClientSecret,
    string? WebsiteRedirectUrl = null,
    string ExtraAuthorizeParameters = "");

/// <summary>
/// OAuth 2 like the OAuth2Helper of Windows: authorize in the browser, get and refresh the tokens. The refresh token is kept
/// in the keyring, the access token only in memory.
/// </summary>
public sealed class OAuth2Client(OAuth2Service service, ISecretStore secrets)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private static readonly ConcurrentDictionary<string, (string Token, DateTimeOffset Expires)> AccessTokens = new();

    private string SecretKey => $"oauth/{service.Name}/refresh_token";

    /// <summary>
    /// How long to wait for the user in the browser
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(4);

    /// <summary>
    /// Opens the browser, xdg-open by default
    /// </summary>
    public Action<string> OpenBrowser { get; init; } = url => ProcessRunner.StartDetached("xdg-open", url);

    public async Task<bool> IsSignedInAsync() => !string.IsNullOrEmpty(await secrets.GetAsync(SecretKey).ConfigureAwait(false));

    public async Task SignOutAsync()
    {
        AccessTokens.TryRemove(service.Name, out _);
        await secrets.RemoveAsync(SecretKey).ConfigureAwait(false);
    }

    /// <summary>
    /// A request with the bearer token; the user signs in first when needed. Null when the user didn't sign in.
    /// </summary>
    public async Task<HttpRequestMessage?> CreateRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (token == null)
        {
            return null;
        }

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    /// <summary>
    /// One sign in or refresh per service at a time, a second upload waits for the first
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var serviceLock = Locks.GetOrAdd(service.Name, _ => new SemaphoreSlim(1, 1));
        await serviceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (AccessTokens.TryGetValue(service.Name, out var cached) && cached.Expires > DateTimeOffset.Now.AddMinutes(1))
            {
                return cached.Token;
            }

            var refreshToken = await secrets.GetAsync(SecretKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshed = await RequestTokenAsync(new Dictionary<string, string>
                {
                    ["refresh_token"] = refreshToken,
                    ["grant_type"] = "refresh_token",
                }, cancellationToken).ConfigureAwait(false);
                if (refreshed != null)
                {
                    return refreshed;
                }
            }

            // No refresh token, or it expired (invalid_grant): sign in again
            var code = await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
            if (code == null)
            {
                return null;
            }

            return await RequestTokenAsync(new Dictionary<string, string>
            {
                ["code"] = code.Value.Code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = code.Value.RedirectUri,
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            serviceLock.Release();
        }
    }

    /// <summary>
    /// Call the token endpoint; stores a new refresh token. Null when the refresh token isn't accepted any more.
    /// </summary>
    private async Task<string?> RequestTokenAsync(Dictionary<string, string> data, CancellationToken cancellationToken)
    {
        data["client_id"] = service.ClientId;
        data["client_secret"] = service.ClientSecret;
        using var request = new HttpRequestMessage(HttpMethod.Post, service.TokenUrl) { Content = new FormUrlEncodedContent(data) };
        var result = await HttpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(result.Content) ? "{}" : result.Content);
        var root = json.RootElement;
        if (root.TryGetProperty("error", out var error))
        {
            if (error.GetString() == "invalid_grant" && data["grant_type"] == "refresh_token")
            {
                await secrets.RemoveAsync(SecretKey).ConfigureAwait(false);
                return null;
            }

            var description = root.TryGetProperty("error_description", out var d) ? d.GetString() : null;
            throw new HttpRequestException($"{service.Name}: {error.GetString()} {description}".Trim());
        }

        result.EnsureSuccess();
        var accessToken = root.GetProperty("access_token").GetString()!;
        var expires = root.TryGetProperty("expires_in", out var seconds) && seconds.TryGetDouble(out var value)
            ? DateTimeOffset.Now.AddSeconds(value)
            : DateTimeOffset.MaxValue;
        AccessTokens[service.Name] = (accessToken, expires);
        if (root.TryGetProperty("refresh_token", out var refresh) && refresh.GetString() is { Length: > 0 } refreshToken)
        {
            await secrets.SetAsync(SecretKey, $"{service.Name} sign in", refreshToken).ConfigureAwait(false);
        }

        return accessToken;
    }

    /// <summary>
    /// Let the user sign in with the browser and wait for the code on localhost
    /// </summary>
    private async Task<(string Code, string RedirectUri)?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        int port = FreePort();
        var prefix = $"http://127.0.0.1:{port}/authorize/";
        bool viaWebsite = service.WebsiteRedirectUrl != null;
        var redirectUri = viaWebsite ? service.WebsiteRedirectUrl! : prefix;
        // The website finds Greenshot by the port in the state
        var state = viaWebsite ? port.ToString(System.Globalization.CultureInfo.InvariantCulture) : Guid.NewGuid().ToString("N");
        var url = $"{service.AuthorizeUrl}?client_id={Uri.EscapeDataString(service.ClientId)}&response_type=code&state={state}"
                  + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}{service.ExtraAuthorizeParameters}";

        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        // The website posts to localhost, not 127.0.0.1
        listener.Prefixes.Add($"http://localhost:{port}/authorize/");
        listener.Start();
        OpenBrowser(url);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            while (true)
            {
                var context = await listener.GetContextAsync().WaitAsync(timeout.Token).ConfigureAwait(false);
                var values = await ReadValuesAsync(context).ConfigureAwait(false);
                if (values == null)
                {
                    continue;
                }

                if (values.TryGetValue("error", out var error))
                {
                    throw new UnauthorizedAccessException(values.GetValueOrDefault("error_description") ?? error);
                }

                if (!viaWebsite && values.GetValueOrDefault("state") != state)
                {
                    continue;
                }

                if (values.TryGetValue("code", out var code) && !string.IsNullOrEmpty(code))
                {
                    return (code, redirectUri);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Warn($"No sign in with {service.Name} within {Timeout}");
            return null;
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// The values of the redirect (query) or of the website (JSON post); answers the browser
    /// </summary>
    private static async Task<Dictionary<string, string>?> ReadValuesAsync(HttpListenerContext context)
    {
        var request = context.Request;
        using var response = context.Response;
        response.AppendHeader("Access-Control-Allow-Origin", "*");
        if (request.HttpMethod == "OPTIONS")
        {
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
            response.AddHeader("Access-Control-Allow-Methods", "POST");
            return null;
        }

        Dictionary<string, string>? values = null;
        if (request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var json = await reader.ReadToEndAsync().ConfigureAwait(false);
            values = JsonSerializer.Deserialize<Dictionary<string, string>>(json, Serialization.GreenshotJson.Options);
            await WriteAsync(response, "application/json", "{\"version\": \"1.4\"}").ConfigureAwait(false);
        }
        else if (request.QueryString.Count > 0)
        {
            values = request.QueryString.AllKeys.Where(k => k != null).ToDictionary(k => k!, k => request.QueryString[k] ?? string.Empty);
            await WriteAsync(response, "text/html; charset=utf-8",
                "<html><body><h2>Greenshot</h2><p>You can close this page and go back to Greenshot.</p></body></html>").ConfigureAwait(false);
        }

        return values;
    }

    private static async Task WriteAsync(HttpListenerResponse response, string type, string content)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentType = type;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer).ConfigureAwait(false);
    }

    private static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }
}
