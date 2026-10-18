/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Greenshot.Plugin.CustomUploader.Sxcu;
using Newtonsoft.Json;

namespace Greenshot.Plugin.CustomUploader;

/// <summary>
/// An imported uploader, <see cref="Definition"/> is null when it can't be decrypted on this Windows account.
/// </summary>
internal sealed class StoredUploader
{
    public StoredUploader(string id, UploaderDefinition definition)
    {
        Id = id;
        Definition = definition;
    }

    public string Id { get; }

    public UploaderDefinition Definition { get; }

    public string Name => Definition?.Name;
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
/// The uploaders, their icons and the uploads in the [CustomUploader] section, each value encrypted for the Windows user.
/// Used from the settings (UI thread) and the uploads (thread pool), so every access is locked (one lock for all instances,
/// they share the dictionaries of the configuration).
/// </summary>
internal sealed class UploaderStore
{
    /// <summary>
    /// The newest uploads which are kept, older ones are forgotten
    /// </summary>
    public const int MaximumUploads = 100;

    private static readonly object Lock = new object();
    private readonly IDictionary<string, string> _uploaders;
    private readonly IDictionary<string, string> _uploads;
    private readonly IDictionary<string, string> _icons;
    private readonly Action _changed;
    private readonly Func<string, string> _protect;
    private readonly Func<string, string> _unprotect;

    /// <param name="uploaders">id to encrypted uploader</param>
    /// <param name="uploads">id to encrypted upload</param>
    /// <param name="changed">Called after a change, saves the configuration</param>
    /// <param name="protect">Encrypts, null: <see cref="UserProtection.Protect"/></param>
    /// <param name="unprotect">Decrypts, null: <see cref="UserProtection.Unprotect"/></param>
    /// <param name="icons">id of the uploader to its encrypted icon, null: not kept</param>
    public UploaderStore(IDictionary<string, string> uploaders, IDictionary<string, string> uploads, Action changed, Func<string, string> protect = null, Func<string, string> unprotect = null,
        IDictionary<string, string> icons = null)
    {
        _icons = icons ?? new Dictionary<string, string>();
        _uploaders = uploaders ?? throw new ArgumentNullException(nameof(uploaders));
        _uploads = uploads ?? throw new ArgumentNullException(nameof(uploads));
        _changed = changed ?? (() => { });
        _protect = protect ?? UserProtection.Protect;
        _unprotect = unprotect ?? UserProtection.Unprotect;
    }

    /// <summary>
    /// The uploaders, sorted by name; those which can't be decrypted at the end.
    /// </summary>
    public IReadOnlyList<StoredUploader> GetUploaders()
    {
        lock (Lock)
        {
            return _uploaders
                .Select(entry => new StoredUploader(entry.Key, ReadUploader(entry.Value)))
                .OrderBy(uploader => uploader.Definition == null)
                .ThenBy(uploader => uploader.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }

    /// <summary>
    /// The uploader with the name, null when there is none (or it can't be decrypted)
    /// </summary>
    public StoredUploader Find(string name) =>
        string.IsNullOrEmpty(name) ? null : GetUploaders().FirstOrDefault(uploader => string.Equals(uploader.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Add the checked uploader, it replaces the one with the same name.
    /// </summary>
    /// <returns>the id</returns>
    public string Add(UploaderDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        lock (Lock)
        {
            var existing = Find(definition.Name);
            string id = existing?.Id ?? Guid.NewGuid().ToString("N");
            _uploaders[id] = _protect(JsonConvert.SerializeObject(definition));
            // The icon belonged to the old file, maybe another server: fetched again
            _icons.Remove(id);
        }

        OnChanged();
        return GetUploaders().First(uploader => uploader.Definition != null && string.Equals(uploader.Name, definition.Name, StringComparison.OrdinalIgnoreCase)).Id;
    }

    public void Remove(string id)
    {
        bool removed;
        lock (Lock)
        {
            removed = id != null && _uploaders.Remove(id);
            if (removed)
            {
                _icons.Remove(id);
            }
        }

        if (removed)
        {
            OnChanged();
        }
    }

    /// <summary>
    /// The icon of the uploader (PNG), null when there is none or it can't be decrypted
    /// </summary>
    public byte[] GetIcon(string id)
    {
        lock (Lock)
        {
            if (id == null || !_icons.TryGetValue(id, out var protectedIcon))
            {
                return null;
            }

            string base64 = _unprotect(protectedIcon);
            try
            {
                return base64 == null ? null : Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Keep the icon of the uploader, when it is still there (it can be removed while the icon is fetched)
    /// </summary>
    public void SetIcon(string id, byte[] png)
    {
        if (png == null || png.Length == 0) return;
        lock (Lock)
        {
            if (id == null || !_uploaders.ContainsKey(id))
            {
                return;
            }

            _icons[id] = _protect(Convert.ToBase64String(png));
        }

        OnChanged();
    }

    /// <summary>
    /// The uploads, newest first.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, UploadRecord>> GetUploads()
    {
        lock (Lock)
        {
            return _uploads
                .Select(entry => new KeyValuePair<string, UploadRecord>(entry.Key, Read<UploadRecord>(entry.Value)))
                .Where(entry => entry.Value != null)
                .OrderByDescending(entry => entry.Value.Uploaded)
                .ToList();
        }
    }

    public void AddUpload(UploadRecord upload)
    {
        if (upload == null) throw new ArgumentNullException(nameof(upload));
        lock (Lock)
        {
            _uploads[Guid.NewGuid().ToString("N")] = _protect(JsonConvert.SerializeObject(upload));
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
        lock (Lock)
        {
            removed = id != null && _uploads.Remove(id);
        }

        if (removed)
        {
            OnChanged();
        }
    }

    /// <summary>
    /// Checked again like an imported file: what is sent stays what the user approved
    /// </summary>
    private UploaderDefinition ReadUploader(string protectedJson)
    {
        string json = _unprotect(protectedJson);
        if (json == null)
        {
            return null;
        }

        try
        {
            return UploaderFile.Read(json);
        }
        catch (UploaderFileException)
        {
            return null;
        }
    }

    private T Read<T>(string protectedJson) where T : class
    {
        string json = _unprotect(protectedJson);
        if (json == null)
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void OnChanged() => _changed();
}
