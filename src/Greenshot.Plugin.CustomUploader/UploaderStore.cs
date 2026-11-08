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

    public string Write(UploaderDefinition definition) => JsonSerializer.Serialize(definition, AsciiJson);

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
/// The uploaders with their icons (<see cref="DefinitionStore{T}"/>), and next to them the reminders and the uploads, all in the
/// [CustomUploader] section; the uploads encrypted like the uploaders.
/// </summary>
internal sealed class UploaderStore : DefinitionStore<UploaderDefinition>
{
    /// <summary>
    /// The newest uploads which are kept, older ones are forgotten
    /// </summary>
    public const int MaximumUploads = 100;

    private readonly IDictionary<string, string> _uploads;
    private readonly IDictionary<string, string> _reminders;

    /// <param name="uploaders">id to encrypted uploader</param>
    /// <param name="uploads">id to encrypted upload</param>
    /// <param name="changed">Called after a change, saves the configuration</param>
    /// <param name="protect">Encrypts, null: <see cref="StringExtensions.Encrypt"/></param>
    /// <param name="unprotect">Decrypts, null: <see cref="StringExtensions.Decrypt"/> (which gives back what it can't decrypt, the JSON then can't be read)</param>
    /// <param name="icons">id of the uploader to its icon (base64 PNG, the public favicon of the service), null: not kept</param>
    /// <param name="reminders">ids of the uploaders with a reminder before each upload, null: not kept</param>
    public UploaderStore(IDictionary<string, string> uploaders, IDictionary<string, string> uploads, Action changed, Func<string, string> protect = null, Func<string, string> unprotect = null,
        IDictionary<string, string> icons = null, IDictionary<string, string> reminders = null)
        : base(UploaderKind.Instance, uploaders, () => Notify(changed), icons, protect, unprotect)
    {
        _reminders = reminders ?? new Dictionary<string, string>();
        _uploads = uploads ?? throw new ArgumentNullException(nameof(uploads));
    }

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
            _uploads[Guid.NewGuid().ToString("N")] = Protect(JsonSerializer.Serialize(upload, UploaderKind.AsciiJson));
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
        string json = Unprotect(protectedJson);
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
