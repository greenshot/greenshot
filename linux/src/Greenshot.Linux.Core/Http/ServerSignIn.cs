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
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Languages;

namespace Greenshot.Linux.Core.Http;

/// <summary>
/// What a user signs in to a server with: a user name with a password (or an API token), sent as Basic,
/// or only a personal access token, sent as Bearer.
/// </summary>
public sealed class ServerCredentials(string? userName, string? secret)
{
    public string? UserName { get; } = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();

    public string Secret { get; } = secret ?? string.Empty;

    /// <summary>
    /// True when the secret is a personal access token without a user name
    /// </summary>
    public bool IsToken => UserName == null;

    public AuthenticationHeaderValue CreateAuthorization() =>
        IsToken
            ? new AuthenticationHeaderValue("Bearer", Secret)
            : new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Secret}")));

    public bool SameAs(ServerCredentials? other) => other != null && UserName == other.UserName && Secret == other.Secret;

    internal string Serialize() => JsonSerializer.Serialize(new Dictionary<string, string?> { ["user"] = UserName, ["secret"] = Secret });

    internal static ServerCredentials? Deserialize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(value);
            return values == null ? null : new ServerCredentials(values.GetValueOrDefault("user"), values.GetValueOrDefault("secret"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Never the secret, these can end up in a log
    /// </summary>
    public override string ToString() => IsToken ? "(token)" : UserName!;
}

/// <summary>
/// The server doesn't take credentials for now, e.g. it asks for a CAPTCHA in the browser after failed sign-ins.
/// </summary>
public sealed class SignInDeniedException(string reason) : UnauthorizedAccessException($"Sign-in denied: {reason}")
{
    /// <summary>
    /// Throw when the server answered 403 with the header which says why it denies the sign-in
    /// </summary>
    public static void ThrowWhenDenied(HttpResult result, string reasonHeader)
    {
        if (result.StatusCode == HttpStatusCode.Forbidden && result.Headers != null && result.Headers.TryGetValue(reasonHeader, out var reason))
        {
            throw new SignInDeniedException(reason);
        }
    }
}

/// <summary>
/// What the sign-in window offers for a server
/// </summary>
/// <param name="ApiTokenUrl">the page where a cloud user creates an API token, null when the server has no cloud variant</param>
/// <param name="IsCloud">true when the url is a cloud server, it preselects the e-mail address and API token</param>
public sealed record ServerSignInOptions(string? ApiTokenUrl = null, Func<Uri, bool>? IsCloud = null)
{
    public bool IsCloudUrl(string url) => IsCloud != null && Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsCloud(uri);
}

/// <summary>
/// What the sign-in window shows; Connect checks credentials and throws like the connect of <see cref="ServerSignIn"/>
/// </summary>
public sealed record ServerSignInRequest(
    string ServiceName,
    string Url,
    ServerSignInOptions Options,
    ServerCredentials? Credentials,
    bool Remember,
    string? Error,
    Func<ServerCredentials, CancellationToken, Task> ConnectAsync);

/// <summary>
/// The credentials which connected, and if they should be kept in the keyring
/// </summary>
public sealed record ServerSignInAnswer(ServerCredentials Credentials, bool Remember);

/// <summary>
/// Shows the sign-in window, null when the user canceled
/// </summary>
public delegate Task<ServerSignInAnswer?> ServerSignInPrompt(ServerSignInRequest request, CancellationToken cancellationToken);

/// <summary>
/// The sign-in to the server of a plugin (Jira, Confluence) like the ServerSignIn of Windows: the credentials of this session or
/// the ones stored in the keyring first, the sign-in window only when there are none or the server rejects them.
/// </summary>
public sealed class ServerSignIn
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Func<string> _url;
    private readonly Func<string, ServerCredentials, CancellationToken, Task<string?>> _connectAsync;
    private readonly ISecretStore _secrets;
    private readonly ServerSignInPrompt _prompt;
    private volatile SignedIn? _signedIn;
    private volatile bool _verified;
    private ServerSignIn? _sharesWith;

    /// <param name="serviceName">the name of the service in the sign-in window, e.g. Jira</param>
    /// <param name="url">the url of the server from the settings, also the key in the keyring</param>
    /// <param name="connectAsync">
    /// connects with the credentials to the url and returns the name of the account,
    /// throws <see cref="UnauthorizedAccessException"/> when the server rejects them
    /// </param>
    public ServerSignIn(string serviceName, Func<string> url, Func<string, ServerCredentials, CancellationToken, Task<string?>> connectAsync,
        ISecretStore secrets, ServerSignInPrompt prompt, ServerSignInOptions? options = null)
    {
        ServiceName = serviceName;
        _url = url;
        _connectAsync = connectAsync;
        _secrets = secrets;
        _prompt = prompt;
        Options = options ?? new ServerSignInOptions();
    }

    /// <summary>
    /// Signed in, out or to another account
    /// </summary>
    public event EventHandler? Changed;

    public string ServiceName { get; }

    public ServerSignInOptions Options { get; }

    public string Url => _url().Trim();

    public bool IsSignedIn => _signedIn is { } signedIn && signedIn.Url == Url;

    public string? Account => IsSignedIn ? _signedIn!.Account : null;

    private static string SecretKey(string url) => $"server/{url.TrimEnd('/')}";

    private async Task<ServerCredentials?> ReadStoredAsync(string url) =>
        string.IsNullOrEmpty(url) ? null : ServerCredentials.Deserialize(await _secrets.GetAsync(SecretKey(url)).ConfigureAwait(false));

    private Task StoreAsync(string url, ServerCredentials credentials) => _secrets.SetAsync(SecretKey(url), $"{ServiceName} {url}", credentials.Serialize());

    public async Task<bool> HasStoredCredentialsAsync() => await ReadStoredAsync(Url).ConfigureAwait(false) != null;

    /// <summary>
    /// Make sure there is a checked sign-in: with the credentials of this session or the stored ones, otherwise the sign-in window
    /// </summary>
    /// <returns>false when the user canceled the sign-in</returns>
    public async Task<bool> EnsureSignedInAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string url = Url;
            var signedIn = _signedIn;
            if (_verified && signedIn != null && signedIn.Url == url)
            {
                return true;
            }

            var sessionCredentials = signedIn?.Url == url ? signedIn.Credentials : null;
            var storedCredentials = await ReadStoredAsync(url).ConfigureAwait(false);
            var (sharedCredentials, sharedAreStored) = await GetSharedCredentialsAsync(url).ConfigureAwait(false);
            string? error = null;
            var tried = new List<ServerCredentials>();
            foreach (var credentials in new[] { sessionCredentials, storedCredentials, sharedCredentials })
            {
                if (credentials == null || tried.Any(credentials.SameAs))
                {
                    continue;
                }

                tried.Add(credentials);
                try
                {
                    await ConnectAsync(url, credentials, cancellationToken).ConfigureAwait(false);
                    if (credentials == sharedCredentials && sharedAreStored && storedCredentials == null)
                    {
                        // Remembered for the other server, so for this one too
                        await StoreAsync(url, credentials).ConfigureAwait(false);
                    }

                    return true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Info($"{ServiceName} rejected the credentials of {credentials}: {ex.Message}");
                    error = DescribeError(ex);
                }
            }

            return await PromptAsync(url, sessionCredentials ?? storedCredentials ?? sharedCredentials, true, error, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Show the sign-in window, also when signed in (another account, a new token)
    /// </summary>
    /// <returns>false when the user canceled</returns>
    public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string url = Url;
            var stored = await ReadStoredAsync(url).ConfigureAwait(false);
            var (shared, _) = await GetSharedCredentialsAsync(url).ConfigureAwait(false);
            var credentials = (_signedIn?.Url == url ? _signedIn.Credentials : stored) ?? shared;
            return await PromptAsync(url, credentials, true, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Connect again with the credentials of this session or the stored ones, without the sign-in window
    /// </summary>
    /// <returns>the account, null when there are no credentials</returns>
    /// <exception cref="UnauthorizedAccessException">the server rejected the credentials</exception>
    public async Task<string?> TestAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string url = Url;
            var credentials = _signedIn?.Url == url ? _signedIn.Credentials : await ReadStoredAsync(url).ConfigureAwait(false);
            if (credentials == null)
            {
                return null;
            }

            await ConnectAsync(url, credentials, cancellationToken).ConfigureAwait(false);
            return Account;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Servers on the same host usually use the same account, e.g. Jira and Confluence on one Atlassian site:
    /// when a sign-in has no working credentials of its own, it tries the ones of the other before it shows the window
    /// </summary>
    public void ShareCredentialsWith(ServerSignIn other)
    {
        if (other == this)
        {
            throw new ArgumentException("Another sign-in is needed", nameof(other));
        }

        _sharesWith = other;
        other._sharesWith = this;
    }

    private async Task<(ServerCredentials? Credentials, bool AreStored)> GetSharedCredentialsAsync(string url)
    {
        var other = _sharesWith;
        string? otherUrl = other?.Url;
        if (other == null || otherUrl == null || !IsSameHost(url, otherUrl))
        {
            return (null, false);
        }

        var stored = await other.ReadStoredAsync(otherUrl).ConfigureAwait(false);
        var signedIn = other._signedIn;
        return (signedIn != null && signedIn.Url == otherUrl ? signedIn.Credentials : stored, stored != null);
    }

    public static bool IsSameHost(string url, string otherUrl) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && Uri.TryCreate(otherUrl, UriKind.Absolute, out var otherUri)
        && string.Equals(uri.GetLeftPart(UriPartial.Authority), otherUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The next <see cref="EnsureSignedInAsync"/> checks the credentials again, e.g. after a session timeout
    /// </summary>
    public void Recheck() => _verified = false;

    /// <summary>
    /// Forget the credentials, also the stored ones
    /// </summary>
    public async Task SignOutAsync()
    {
        _verified = false;
        _signedIn = null;
        await _secrets.RemoveAsync(SecretKey(Url)).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The text for the user when connecting failed
    /// </summary>
    public static string DescribeError(Exception exception) =>
        exception switch
        {
            SignInDeniedException => Texts.Core("signin_denied"),
            UnauthorizedAccessException => Texts.Core("signin_rejected"),
            _ => Texts.Core("signin_failed", exception.Message),
        };

    private async Task<bool> PromptAsync(string url, ServerCredentials? credentials, bool remember, string? error, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        var request = new ServerSignInRequest(ServiceName, url, Options, credentials, remember, error,
            (newCredentials, token) => ConnectAsync(url, newCredentials, token));
        var answer = await _prompt(request, cancellationToken).ConfigureAwait(false);
        if (answer == null)
        {
            return false;
        }

        try
        {
            if (answer.Remember)
            {
                await StoreAsync(url, answer.Credentials).ConfigureAwait(false);
            }
            else
            {
                await _secrets.RemoveAsync(SecretKey(url)).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            Log.Warn($"Couldn't update the stored credentials of {ServiceName}", ex);
        }

        return IsSignedIn;
    }

    private async Task ConnectAsync(string url, ServerCredentials credentials, CancellationToken cancellationToken)
    {
        string? account;
        try
        {
            account = await _connectAsync(url, credentials, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            // The credentials of the session don't work anymore (token revoked, password changed)
            var signedIn = _signedIn;
            if (signedIn != null && signedIn.Url == url && credentials.SameAs(signedIn.Credentials))
            {
                _verified = false;
                _signedIn = null;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            throw;
        }

        _signedIn = new SignedIn(url, credentials, string.IsNullOrEmpty(account) ? credentials.ToString() : account);
        _verified = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed record SignedIn(string Url, ServerCredentials Credentials, string Account);
}
