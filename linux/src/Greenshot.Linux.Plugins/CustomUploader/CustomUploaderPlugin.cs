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

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Core.Plugins;
using Greenshot.Linux.Plugins.CustomUploader.Sxcu;
using Capture = Greenshot.Linux.Core.Capturing.Capture;

namespace Greenshot.Linux.Plugins.CustomUploader;

/// <summary>
/// An upload, for the list of the user's uploads: the link and the link to delete it
/// </summary>
public sealed record UploadRecord(string Uploader, string Url, string? DeletionUrl, DateTimeOffset Uploaded);

/// <summary>
/// The uploaders and the uploads. The names and whether to remind before an upload are in [CustomUploader] of greenshot.ini,
/// the uploaders themselves (they hold the keys) and the uploads (with their delete links) in the keyring.
/// </summary>
public sealed class UploaderStore(CoreSettings settings, ISecretStore secrets)
{
    public const string SectionName = "CustomUploader";
    private const int MaxUploads = 100;
    private const string HistoryKey = "customuploader/history";
    private readonly Dictionary<string, UploaderDefinition?> _uploaders = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Names =>
        (settings.GetValue(SectionName, "UploaderNames") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// The uploaders read from the keyring; null for one which can't be read (import it again)
    /// </summary>
    public IReadOnlyDictionary<string, UploaderDefinition?> Uploaders => _uploaders;

    /// <summary>
    /// Read the uploaders from the keyring, each is checked again like a file
    /// </summary>
    public async Task LoadAsync()
    {
        _uploaders.Clear();
        foreach (var name in Names)
        {
            try
            {
                var json = await secrets.GetAsync(Key(name)).ConfigureAwait(false);
                _uploaders[name] = json == null ? null : UploaderFile.Read(json);
            }
            catch (Exception ex) when (ex is UploaderFileException or JsonException or InvalidOperationException)
            {
                Log.Warn($"The uploader {name} can't be read: {ex.Message}");
                _uploaders[name] = null;
            }
        }
    }

    public async Task AddAsync(UploaderDefinition definition, bool remindBeforeUpload)
    {
        var json = JsonSerializer.Serialize(definition, UploaderFile.JsonOptions);
        await secrets.SetAsync(Key(definition.Name), definition.Name, json).ConfigureAwait(false);
        if (!Names.Contains(definition.Name, StringComparer.OrdinalIgnoreCase))
        {
            settings.SetValue(SectionName, "UploaderNames", string.Join(',', Names.Append(definition.Name)));
        }

        SetReminder(definition.Name, remindBeforeUpload);
        settings.Save();
        _uploaders[definition.Name] = definition;
    }

    public async Task RemoveAsync(string name)
    {
        await secrets.RemoveAsync(Key(name)).ConfigureAwait(false);
        settings.SetValue(SectionName, "UploaderNames", string.Join(',', Names.Where(n => !n.Equals(name, StringComparison.OrdinalIgnoreCase))));
        settings.RemoveValue(SectionName, $"Remind.{name}");
        settings.Save();
        _uploaders.Remove(name);
    }

    public bool RemindsBeforeUpload(string name) => bool.TryParse(settings.GetValue(SectionName, $"Remind.{name}"), out var remind) && remind;

    public void SetReminder(string name, bool remind)
    {
        if (remind)
        {
            settings.SetValue(SectionName, $"Remind.{name}", "True");
        }
        else
        {
            settings.RemoveValue(SectionName, $"Remind.{name}");
        }

        settings.Save();
    }

    public async Task<IReadOnlyList<UploadRecord>> GetUploadsAsync()
    {
        try
        {
            var json = await secrets.GetAsync(HistoryKey).ConfigureAwait(false);
            return json == null ? [] : JsonSerializer.Deserialize<List<UploadRecord>>(json) ?? [];
        }
        catch (JsonException ex)
        {
            Log.Warn($"The list of uploads can't be read: {ex.Message}");
            return [];
        }
    }

    public async Task AddUploadAsync(UploadRecord upload)
    {
        var uploads = (await GetUploadsAsync().ConfigureAwait(false)).Prepend(upload).Take(MaxUploads).ToList();
        await secrets.SetAsync(HistoryKey, "Custom uploader uploads", JsonSerializer.Serialize(uploads)).ConfigureAwait(false);
    }

    public async Task ForgetUploadAsync(UploadRecord upload)
    {
        var uploads = (await GetUploadsAsync().ConfigureAwait(false)).Where(u => u != upload).ToList();
        await secrets.SetAsync(HistoryKey, "Custom uploader uploads", JsonSerializer.Serialize(uploads)).ConfigureAwait(false);
    }

    private static string Key(string name) => $"customuploader/{name}";
}

/// <summary>
/// A value of an uploader the user fills in before the import, e.g. the key where the file says YOUR_API_KEY
/// </summary>
public sealed partial class UploaderValue(UploaderValueKind kind, string? name, string? value)
{
    [GeneratedRegex("key|token|secret|password|auth", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretName();

    [GeneratedRegex(@"<[^<>]*>|\[[^\[\]]*\]|\byour[\s_-]|\binsert\b|\bhere\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderText();

    public UploaderValueKind Kind { get; } = kind;

    /// <summary>
    /// The parameter, header or field, null for the address
    /// </summary>
    public string? Name { get; } = name;

    public string? Value { get; set; } = value;

    /// <summary>
    /// A key: shown masked
    /// </summary>
    public bool IsSecret => Kind != UploaderValueKind.RequestUrl && Name != null && SecretName().IsMatch(Name);

    /// <summary>
    /// Still the text of the file, not the user's value
    /// </summary>
    public bool LooksLikePlaceholder => !string.IsNullOrEmpty(Value) && PlaceholderText().IsMatch(Value);

    public string Label => Kind switch
    {
        UploaderValueKind.RequestUrl => CustomUploaderTexts.Get("ValuesRequestUrl"),
        UploaderValueKind.Parameter => string.Format(CustomUploaderTexts.Get("ValuesParameter"), Name),
        UploaderValueKind.Header => string.Format(CustomUploaderTexts.Get("ValuesHeader"), Name),
        _ => string.Format(CustomUploaderTexts.Get("ValuesArgument"), Name),
    };
}

public enum UploaderValueKind
{
    RequestUrl,
    Parameter,
    Header,
    Argument,
}

/// <summary>
/// The import of an .sxcu file without the UI, the logic of CustomUploaderSettingsViewModel of Windows
/// </summary>
public static class UploaderImport
{
    public const int MaxFileSize = 256 * 1024;

    /// <exception cref="UploaderFileException">The file can't be used, the message says why</exception>
    public static UploaderDefinition ReadFile(string path)
    {
        if (new FileInfo(path).Length > MaxFileSize)
        {
            throw new UploaderFileException(CustomUploaderTexts.Instance.ErrorFileTooLarge);
        }

        return UploaderFile.Read(File.ReadAllText(path));
    }

    public static IReadOnlyList<UploaderValue> GetValues(UploaderDefinition definition)
    {
        var values = new List<UploaderValue> { new(UploaderValueKind.RequestUrl, null, definition.RequestURL) };
        values.AddRange((definition.Parameters ?? new Dictionary<string, string>()).Select(e => new UploaderValue(UploaderValueKind.Parameter, e.Key, e.Value)));
        values.AddRange((definition.Headers ?? new Dictionary<string, string>()).Select(e => new UploaderValue(UploaderValueKind.Header, e.Key, e.Value)));
        values.AddRange((definition.Arguments ?? new Dictionary<string, string>()).Select(e => new UploaderValue(UploaderValueKind.Argument, e.Key, e.Value)));
        return values;
    }

    /// <summary>
    /// The uploader with the user's values, checked again like a file
    /// </summary>
    /// <exception cref="UploaderFileException">The values can't be used</exception>
    public static UploaderDefinition WithValues(UploaderDefinition definition, IEnumerable<UploaderValue> values)
    {
        var copy = JsonSerializer.Deserialize<UploaderDefinition>(JsonSerializer.Serialize(definition, UploaderFile.JsonOptions), UploaderFile.JsonOptions)!;
        foreach (var value in values)
        {
            var text = value.Value ?? string.Empty;
            switch (value.Kind)
            {
                case UploaderValueKind.RequestUrl:
                    copy.RequestURL = text;
                    break;
                case UploaderValueKind.Parameter:
                    copy.Parameters[value.Name!] = text;
                    break;
                case UploaderValueKind.Header:
                    copy.Headers[value.Name!] = text;
                    break;
                case UploaderValueKind.Argument:
                    copy.Arguments[value.Name!] = text;
                    break;
            }
        }

        return UploaderFile.Read(JsonSerializer.Serialize(copy, UploaderFile.JsonOptions));
    }

    public static bool HasNoDeletion(UploaderDefinition definition) => string.IsNullOrWhiteSpace(definition.DeletionURL);

    /// <summary>
    /// What the user confirms: where the captures go and which headers go with them, never their values
    /// </summary>
    public static string DescribeImport(UploaderDefinition definition, bool replaces)
    {
        string server = UploaderFile.DescribeServer(definition);
        var text = new StringBuilder(string.Format(CustomUploaderTexts.Get("ImportQuestion"), definition.Name, server));
        var headers = definition.Headers?.Keys.ToList();
        if (headers?.Count > 0)
        {
            text.Append("\n\n").Append(string.Format(CustomUploaderTexts.Get("ImportHeaders"), string.Join(", ", headers)));
        }

        if (HasNoDeletion(definition))
        {
            text.Append("\n\n").Append(string.Format(CustomUploaderTexts.Get("ImportNoDeletion"), server));
        }

        if (replaces)
        {
            text.Append("\n\n").Append(CustomUploaderTexts.Get("ImportReplaces"));
        }

        text.Append("\n\n").Append(string.Format(CustomUploaderTexts.Get("ImportNotGreenshot"), definition.Name, server));
        text.Append("\n\n").Append(CustomUploaderTexts.Get("ImportTrust"));
        return text.ToString();
    }
}

/// <summary>
/// The answer to the reminder before an upload
/// </summary>
public enum UploadReminderAnswer
{
    Cancel,
    Upload,
    UploadAndStopReminding,
}

/// <summary>
/// Uploads with one uploader. The link goes to the clipboard and the notification opens it.
/// </summary>
public sealed class CustomUploaderDestination(
    CoreSettings settings,
    UploaderStore store,
    UploaderDefinition definition,
    Func<Capture, string, string, Task<UploadReminderAnswer>>? remind) : IDestination
{
    public const string DesignationPrefix = "CustomUploader";

    public string Designation => $"{DesignationPrefix}:{definition.Name}";

    public string DisplayName => $"{CustomUploaderTexts.Instance.UploadMenuItem}: {definition.Name}";

    public int Priority => 20;

    public bool IsAvailable => true;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        if (remind != null && store.RemindsBeforeUpload(definition.Name))
        {
            var answer = await remind(capture, definition.Name, UploaderFile.DescribeServer(definition)).ConfigureAwait(true);
            if (answer == UploadReminderAnswer.Cancel)
            {
                return ExportResult.Aborted;
            }

            if (answer == UploadReminderAnswer.UploadAndStopReminding)
            {
                store.SetReminder(definition.Name, false);
            }
        }

        var format = ImageFormats.Normalize(settings.OutputFileFormat);
        var filename = FilenamePattern.ForSettings(settings).GetFilename(settings.OutputFileFilenamePattern, format, capture.Details);
        var image = new EncodedImage(ImageFormats.Encode(capture.Image, format, settings.OutputFileJpegQuality), format);
        UploaderResponse response;
        try
        {
            using var request = UploaderRequest.Create(definition, image, filename, null);
            var result = await HttpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response = UploaderResponse.From(definition, result, filename);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or TaskCanceledException or UploaderFileException)
        {
            Log.Warn($"Upload with {definition.Name} failed: {ex.Message}");
            return ExportResult.Failed(string.Format(CustomUploaderTexts.Get("UploadFailedMessage"), definition.Name, ex.Message));
        }

        if (!response.IsSuccess)
        {
            Log.Warn($"Upload with {definition.Name} failed, the service didn't accept it: {response.Error}");
            return ExportResult.Failed(string.Format(CustomUploaderTexts.Get("UploadFailedMessage"), definition.Name, response.Error));
        }

        if (response.Url == null)
        {
            return ExportResult.Failed(CustomUploaderTexts.Instance.NoLink);
        }

        var link = response.Url.AbsoluteUri;
        await store.AddUploadAsync(new UploadRecord(definition.Name, link, response.DeletionUrl?.AbsoluteUri, DateTimeOffset.Now)).ConfigureAwait(false);
        await SystemClipboard.SetTextAsync(link).ConfigureAwait(false);
        Log.Info($"Uploaded with {definition.Name}");
        return ExportResult.Done(message: link) with { Link = link };
    }
}

/// <summary>
/// The CustomUploader plugin: one destination for every imported uploader
/// </summary>
public sealed class CustomUploaderPlugin(CoreSettings settings, ISecretStore secrets, Func<Capture, string, string, Task<UploadReminderAnswer>>? remind)
    : IGreenshotPlugin
{
    public UploaderStore Store { get; } = new(settings, secrets);

    public string Name => "CustomUploader";

    public string DisplayName => Texts.Linux("plugin_customuploader");

    public IReadOnlyList<IDestination> Destinations =>
        Store.Uploaders.Values.OfType<UploaderDefinition>().Select(d => (IDestination)new CustomUploaderDestination(settings, Store, d, remind)).ToList();
}
