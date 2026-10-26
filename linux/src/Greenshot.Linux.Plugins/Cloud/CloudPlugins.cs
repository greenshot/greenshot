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

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Core.Plugins;

namespace Greenshot.Linux.Plugins.Cloud;

/// <summary>
/// The settings of an upload plugin ([Box], [Dropbox]) with the keys of Windows, and the app credentials.
/// The Windows build gets its credentials from the build server; on Linux they are ClientId and ClientSecret in the section,
/// or the environment (GREENSHOT_BOX_CLIENT_ID, GREENSHOT_BOX_CLIENT_SECRET), from an app registered with the service.
/// </summary>
public sealed class CloudSettings(CoreSettings settings, string section)
{
    public string Section => section;

    public string UploadFormat
    {
        get => ImageFormats.Normalize(settings.GetValue(section, nameof(UploadFormat)) ?? "png");
        set => settings.SetValue(section, nameof(UploadFormat), value);
    }

    public int UploadJpegQuality =>
        int.TryParse(settings.GetValue(section, nameof(UploadJpegQuality)), NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ? Math.Clamp(q, 1, 100) : 80;

    public bool AfterUploadLinkToClipBoard
    {
        get => !bool.TryParse(settings.GetValue(section, nameof(AfterUploadLinkToClipBoard)), out var value) || value;
        set => settings.SetValue(section, nameof(AfterUploadLinkToClipBoard), value ? "True" : "False");
    }

    public string ClientId
    {
        get => settings.GetValue(section, nameof(ClientId)) ?? Environment.GetEnvironmentVariable($"GREENSHOT_{section.ToUpperInvariant()}_CLIENT_ID") ?? string.Empty;
        set => settings.SetValue(section, nameof(ClientId), value);
    }

    public string ClientSecret
    {
        get => settings.GetValue(section, nameof(ClientSecret)) ?? Environment.GetEnvironmentVariable($"GREENSHOT_{section.ToUpperInvariant()}_CLIENT_SECRET") ?? string.Empty;
        set => settings.SetValue(section, nameof(ClientSecret), value);
    }

    public bool HasCredentials => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public string? Get(string key) => settings.GetValue(section, key);

    public void Set(string key, string value) => settings.SetValue(section, key, value);

    public EncodedImage Encode(Capture capture) =>
        new(ImageFormats.Encode(capture.Image, UploadFormat, UploadJpegQuality), UploadFormat);

    public string Filename(Capture capture) =>
        FilenamePattern.ForSettings(settings).GetFilename(settings.OutputFileFilenamePattern, UploadFormat, capture.Details);
}

/// <summary>
/// Shared by Box and Dropbox: sign in when needed, upload, link to the clipboard, the texts of the plugin's language file
/// </summary>
public abstract class CloudDestination(CloudSettings settings, ISecretStore secrets) : IDestination
{
    protected CloudSettings Settings => settings;

    public abstract string Designation { get; }

    public string DisplayName => Texts.Get(settings.Section, "upload_menu_item");

    public abstract int Priority { get; }

    public bool IsAvailable => settings.HasCredentials;

    protected abstract OAuth2Service Service { get; }

    public OAuth2Client Client => new(Service, secrets);

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        try
        {
            var link = await UploadAsync(Client, capture, cancellationToken).ConfigureAwait(false);
            if (link == null)
            {
                return ExportResult.Aborted;
            }

            if (link.Length > 0 && settings.AfterUploadLinkToClipBoard)
            {
                await SystemClipboard.SetTextAsync(link).ConfigureAwait(false);
            }

            return ExportResult.Done(message: Texts.Get(settings.Section, "upload_success")) with { Link = link.Length > 0 ? link : null };
        }
        catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or JsonException or TaskCanceledException
                                       or System.Net.HttpListenerException)
        {
            Log.Error($"Upload to {settings.Section} failed", ex);
            return ExportResult.Failed($"{Texts.Get(settings.Section, "upload_failure")} {ex.Message}");
        }
    }

    /// <summary>
    /// The link to the upload, empty when there is none, null when the user didn't sign in
    /// </summary>
    protected abstract Task<string?> UploadAsync(OAuth2Client client, Capture capture, CancellationToken cancellationToken);
}

public sealed class BoxDestination(CloudSettings settings, ISecretStore secrets) : CloudDestination(settings, secrets)
{
    public override string Designation => "Box";

    public override int Priority => 30;

    protected override OAuth2Service Service => new("Box", "https://app.box.com/api/oauth2/authorize", "https://api.box.com/oauth2/token",
        Settings.ClientId, Settings.ClientSecret);

    public string FolderId => Settings.Get(nameof(FolderId)) ?? "0";

    public bool UseSharedLink => !bool.TryParse(Settings.Get(nameof(UseSharedLink)), out var value) || value;

    protected override async Task<string?> UploadAsync(OAuth2Client client, Capture capture, CancellationToken cancellationToken)
    {
        using var request = await client.CreateRequestAsync(HttpMethod.Post, "https://upload.box.com/api/2.0/files/content", cancellationToken).ConfigureAwait(false);
        if (request == null)
        {
            return null;
        }

        request.Content = HttpContents.Multipart("file", Settings.Encode(capture), Settings.Filename(capture),
            new Dictionary<string, string> { ["parent_id"] = FolderId });
        var result = (await HttpService.SendAsync(request, cancellationToken).ConfigureAwait(false)).EnsureSuccess();
        using var upload = JsonDocument.Parse(result.Content);
        var id = upload.RootElement.GetProperty("entries")[0].GetProperty("id").GetString();
        if (!UseSharedLink)
        {
            return $"https://app.box.com/file/{id}";
        }

        using var share = await client.CreateRequestAsync(HttpMethod.Put, $"https://api.box.com/2.0/files/{id}?fields=shared_link", cancellationToken).ConfigureAwait(false);
        if (share == null)
        {
            return null;
        }

        share.Content = new StringContent("{\"shared_link\": {\"access\": \"open\"}}", Encoding.UTF8, "application/json");
        var shared = (await HttpService.SendAsync(share, cancellationToken).ConfigureAwait(false)).EnsureSuccess();
        using var file = JsonDocument.Parse(shared.Content);
        return file.RootElement.GetProperty("shared_link").GetProperty("url").GetString() ?? string.Empty;
    }
}

public sealed class DropboxDestination(CloudSettings settings, ISecretStore secrets) : CloudDestination(settings, secrets)
{
    // For the Dropbox-API-Arg header: everything but ASCII escaped, as Dropbox asks
    private static readonly JsonSerializerOptions HeaderJson = new() { Encoder = JavaScriptEncoder.Default };

    public override string Designation => "Dropbox";

    public override int Priority => 31;

    protected override OAuth2Service Service => new("Dropbox", "https://www.dropbox.com/oauth2/authorize", "https://api.dropbox.com/oauth2/token",
        Settings.ClientId, Settings.ClientSecret, ExtraAuthorizeParameters: "&token_access_type=offline");

    protected override async Task<string?> UploadAsync(OAuth2Client client, Capture capture, CancellationToken cancellationToken)
    {
        var path = "/" + Settings.Filename(capture);
        using var request = await client.CreateRequestAsync(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload", cancellationToken).ConfigureAwait(false);
        if (request == null)
        {
            return null;
        }

        request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(new Dictionary<string, object> { ["autorename"] = true, ["mute"] = true, ["path"] = path }, HeaderJson));
        var content = HttpContents.Upload(Settings.Encode(capture));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Content = content;
        var result = (await HttpService.SendAsync(request, cancellationToken).ConfigureAwait(false)).EnsureSuccess();
        using var upload = JsonDocument.Parse(result.Content);
        var uploadedPath = upload.RootElement.TryGetProperty("path_display", out var p) ? p.GetString() ?? path : path;
        if (!Settings.AfterUploadLinkToClipBoard)
        {
            return string.Empty;
        }

        // A link needs the sharing.write permission of the app; without it the upload is still done
        using var share = await client.CreateRequestAsync(HttpMethod.Post, "https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings", cancellationToken)
            .ConfigureAwait(false);
        if (share == null)
        {
            return string.Empty;
        }

        share.Content = new StringContent(JsonSerializer.Serialize(new { path = uploadedPath }), Encoding.UTF8, "application/json");
        var shared = await HttpService.SendAsync(share, cancellationToken).ConfigureAwait(false);
        if (!shared.IsSuccessStatusCode)
        {
            Log.Warn($"Dropbox made no link ({(int)shared.StatusCode}), the app may lack sharing.write");
            return string.Empty;
        }

        using var link = JsonDocument.Parse(shared.Content);
        return link.RootElement.GetProperty("url").GetString() ?? string.Empty;
    }
}

public sealed class BoxPlugin(CoreSettings settings, ISecretStore secrets) : IGreenshotPlugin
{
    public CloudSettings Settings { get; } = new(settings, "Box");

    public string Name => "Box";

    public string DisplayName => Texts.Linux("plugin_box");

    public BoxDestination Destination => new(Settings, secrets);

    public IReadOnlyList<IDestination> Destinations => [Destination];
}

public sealed class DropboxPlugin(CoreSettings settings, ISecretStore secrets) : IGreenshotPlugin
{
    public CloudSettings Settings { get; } = new(settings, "Dropbox");

    public string Name => "Dropbox";

    public string DisplayName => Texts.Linux("plugin_dropbox");

    public DropboxDestination Destination => new(Settings, secrets);

    public IReadOnlyList<IDestination> Destinations => [Destination];
}
