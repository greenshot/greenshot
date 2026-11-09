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
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Greenshot.Base.Core;
using Greenshot.Base.Definitions;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.CustomUploader.Sxcu;

namespace Greenshot.Plugin.CustomUploader;

/// <summary>
/// Uploaders as definitions: stored as the checked .sxcu JSON, described and approved by their server.
/// </summary>
internal sealed class UploaderKind : IDefinitionKind<UploaderDefinition>
{
    public static UploaderKind Instance { get; } = new UploaderKind();

    // StringExtensions.Encrypt only keeps ASCII: the JSON escapes everything else
    internal static readonly JsonSerializerOptions AsciiJson = new JsonSerializerOptions(UploaderFile.JsonOptions) { Encoder = JavaScriptEncoder.Default };

    /// <summary>
    /// The .sxcu files of the Uploaders folder: indented, UTF-8 (nothing needs escaping for HTML)
    /// </summary>
    internal static readonly JsonSerializerOptions FileJson = new JsonSerializerOptions(UploaderFile.JsonOptions)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private UploaderKind()
    {
    }

    public string Designation => "CustomUploader";

    public string NameOf(UploaderDefinition definition) => definition.Name;

    /// <summary>
    /// Checked again like an imported file: what is sent stays what the user approved
    /// </summary>
    public UploaderDefinition Read(string json)
    {
        try
        {
            return UploaderFile.Read(json);
        }
        catch (UploaderFileException)
        {
            return null;
        }
    }

    public string Write(UploaderDefinition definition) => JsonSerializer.Serialize(definition, FileJson);

    public string Describe(UploaderDefinition definition) => UploaderFile.DescribeServer(definition);

    /// <summary>
    /// What the user approves for a recipe: the uploader and its server
    /// </summary>
    public RecipeGatedAction CreateGatedAction(UploaderDefinition definition) =>
        new RecipeGatedAction(RecipeGateType.NetworkAccess, $"{definition.Name} ({UploaderFile.DescribeServer(definition)})");
}

/// <summary>
/// An upload, kept so the user can open it again and delete it.
/// </summary>
internal sealed class UploadRecord
{
    public string Uploader { get; set; }

    public string Url { get; set; }

    /// <summary>A secret: whoever has it can delete the upload</summary>
    public string DeletionUrl { get; set; }

    public DateTimeOffset Uploaded { get; set; }
}

/// <summary>
/// The uploaders with their icons (<see cref="DefinitionStore{T}"/>): one .sxcu file per uploader in the Uploaders folder, readable,
/// only the values of the parameters, headers and form fields (the keys) encrypted. Next to them in the [CustomUploader] section
/// the icons, the reminders and the uploads, the uploads encrypted as a whole (their deletion links are secrets).
/// </summary>
internal sealed class UploaderStore : DefinitionStore<UploaderDefinition>
{
    /// <summary>
    /// The newest uploads which are kept, older ones are forgotten
    /// </summary>
    public const int MaximumUploads = 100;

    /// <summary>
    /// The start of an encrypted value in an uploader file; a value without it was written by hand and is used as it is
    /// </summary>
    internal const string EncryptedPrefix = "encrypted:";

    private readonly IDictionary<string, string> _uploads;
    private readonly IDictionary<string, string> _reminders;
    private readonly Func<string, string> _protect;
    private readonly Func<string, string> _unprotect;

    /// <param name="uploaders">file name to uploader file (a <see cref="DefinitionFolder"/>) or a dictionary</param>
    /// <param name="uploads">id to encrypted upload</param>
    /// <param name="changed">Called after a change, saves the configuration</param>
    /// <param name="protect">Encrypts a key and an upload, null: <see cref="StringExtensions.Encrypt"/></param>
    /// <param name="unprotect">Decrypts, null when it can't; null: <see cref="StringExtensions.Decrypt"/></param>
    /// <param name="icons">id of the uploader to its icon (base64 PNG, the public favicon of the service), null: not kept</param>
    /// <param name="reminders">ids of the uploaders with a reminder before each upload, null: not kept</param>
    public UploaderStore(IDictionary<string, string> uploaders, IDictionary<string, string> uploads, Action changed, Func<string, string> protect = null, Func<string, string> unprotect = null,
        IDictionary<string, string> icons = null, IDictionary<string, string> reminders = null)
        : base(UploaderKind.Instance, uploaders, () => Notify(changed), icons,
            json => ProtectKeys(json, protect ?? Encrypt), json => UnprotectKeys(json, unprotect ?? Decrypt))
    {
        _protect = protect ?? Encrypt;
        _unprotect = unprotect ?? Decrypt;
        _reminders = reminders ?? new Dictionary<string, string>();
        _uploads = uploads ?? throw new ArgumentNullException(nameof(uploads));
    }

    private static string Encrypt(string text) => text.Encrypt();

    /// <summary>
    /// Decrypt, null when it can't (StringExtensions.Decrypt gives back what it can't decrypt)
    /// </summary>
    private static string Decrypt(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        string clear = text.Decrypt();
        return clear == text ? null : clear;
    }

    /// <summary>
    /// The uploader file with the values of the parameters, headers and form fields encrypted (escaped first, so the stored
    /// value is ASCII). What can't be read is stored as it is, reading it fails the checks.
    /// </summary>
    internal static string ProtectKeys(string json, Func<string, string> protect)
    {
        var definition = Deserialize(json);
        if (definition == null) return json;
        foreach (var values in ValuesOf(definition))
        {
            foreach (string name in values.Keys.ToList())
            {
                string value = values[name];
                if (!string.IsNullOrEmpty(value) && !value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
                {
                    values[name] = EncryptedPrefix + protect(Uri.EscapeDataString(value));
                }
            }
        }

        return JsonSerializer.Serialize(definition, UploaderKind.FileJson);
    }

    /// <summary>
    /// The uploader file with the keys decrypted, null when one can't be decrypted (e.g. the file of another Windows user):
    /// the uploader can't be used then
    /// </summary>
    internal static string UnprotectKeys(string json, Func<string, string> unprotect)
    {
        var definition = Deserialize(json);
        if (definition == null) return json;
        foreach (var values in ValuesOf(definition))
        {
            foreach (string name in values.Keys.ToList())
            {
                string value = values[name];
                if (value == null || !value.StartsWith(EncryptedPrefix, StringComparison.Ordinal)) continue;
                string escaped = unprotect(value.Substring(EncryptedPrefix.Length));
                if (escaped == null) return null;
                values[name] = Uri.UnescapeDataString(escaped);
            }
        }

        return JsonSerializer.Serialize(definition, UploaderKind.FileJson);
    }

    private static UploaderDefinition Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<UploaderDefinition>(json, UploaderFile.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<Dictionary<string, string>> ValuesOf(UploaderDefinition definition) =>
        new[] { definition.Parameters, definition.Headers, definition.Arguments }.Where(values => values != null);

    /// <summary>
    /// Add the checked uploader without a reminder, it replaces the one with the same name.
    /// </summary>
    public override string Add(UploaderDefinition definition) => Add(definition, false);

    /// <summary>
    /// Add the checked uploader, it replaces the one with the same name.
    /// </summary>
    /// <param name="definition">the checked uploader</param>
    /// <param name="remindBeforeUpload">show a reminder with a preview before each upload</param>
    /// <returns>the id</returns>
    public string Add(UploaderDefinition definition, bool remindBeforeUpload) =>
        Add(definition, id =>
        {
            if (remindBeforeUpload)
            {
                _reminders[id] = bool.TrueString;
            }
            else
            {
                _reminders.Remove(id);
            }
        });

    public override bool Remove(string id) => Remove(id, removed => _reminders.Remove(removed));

    /// <summary>
    /// True when the uploader shows a reminder with a preview before each upload
    /// </summary>
    public bool RemindsBeforeUpload(string id)
    {
        lock (SyncRoot)
        {
            return id != null && _reminders.ContainsKey(id);
        }
    }

    /// <summary>
    /// No reminder before the uploads of the uploader anymore ("don't remind me again")
    /// </summary>
    public void StopReminding(string id)
    {
        bool removed;
        lock (SyncRoot)
        {
            removed = id != null && _reminders.Remove(id);
        }

        if (removed)
        {
            OnChanged();
        }
    }

    /// <summary>
    /// The uploads, newest first.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, UploadRecord>> GetUploads()
    {
        lock (SyncRoot)
        {
            return _uploads
                .Select(entry => new KeyValuePair<string, UploadRecord>(entry.Key, ReadUpload(entry.Value)))
                .Where(entry => entry.Value != null)
                .OrderByDescending(entry => entry.Value.Uploaded)
                .ToList();
        }
    }

    public void AddUpload(UploadRecord upload)
    {
        if (upload == null) throw new ArgumentNullException(nameof(upload));
        lock (SyncRoot)
        {
            _uploads[Guid.NewGuid().ToString("N")] = _protect(JsonSerializer.Serialize(upload, UploaderKind.AsciiJson));
            // The oldest go first, and what can't be read anymore
            var keep = new HashSet<string>(GetUploads().Take(MaximumUploads).Select(entry => entry.Key));
            foreach (string id in _uploads.Keys.Where(id => !keep.Contains(id)).ToList())
            {
                _uploads.Remove(id);
            }
        }

        OnChanged();
    }

    public void ForgetUpload(string id)
    {
        bool removed;
        lock (SyncRoot)
        {
            removed = id != null && _uploads.Remove(id);
        }

        if (removed)
        {
            OnChanged();
        }
    }

    private UploadRecord ReadUpload(string protectedJson)
    {
        string json = _unprotect(protectedJson);
        if (json == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UploadRecord>(json, UploaderFile.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// After any change of any store (an upload on the thread pool, an icon), e.g. for the settings page to show it
    /// </summary>
    public static event EventHandler Changed;

    private static void Notify(Action changed)
    {
        changed?.Invoke();
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
