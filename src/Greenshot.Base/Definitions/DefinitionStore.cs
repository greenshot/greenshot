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
using Greenshot.Base.Core;

namespace Greenshot.Base.Definitions;

/// <summary>
/// A stored definition, <see cref="Definition"/> is null when it can't be decrypted or read.
/// </summary>
public sealed class StoredDefinition<T> where T : class
{
    public StoredDefinition(string id, T definition, string name, bool isBuiltIn = false, bool isApproved = true)
    {
        IsApproved = isApproved;
        Id = id;
        Definition = definition;
        Name = name;
        IsBuiltIn = isBuiltIn;
    }

    /// <summary>
    /// Comes with Greenshot or the plugin: not stored, can't be changed or removed
    /// </summary>
    public bool IsBuiltIn { get; }

    /// <summary>
    /// False for a file which was added or changed outside Greenshot and which the user didn't approve yet: it isn't used
    /// </summary>
    public bool IsApproved { get; }

    /// <summary>
    /// Can be read and is approved: it is used (a destination, in recipes)
    /// </summary>
    public bool IsUsable => Definition != null && IsApproved;

    public string Id { get; }

    public T Definition { get; }

    /// <summary>
    /// Null when the definition can't be read
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// The definitions of a kind: one JSON value per definition, in a folder of files (<see cref="DefinitionFolder"/>) or a dictionary of
/// an ini section, protected as the plugin says (default <see cref="StringExtensions.Encrypt"/>), and the icons per definition
/// (base64 PNG). Used from the settings (UI thread) and the exports (thread pool), so every access
/// is locked; one lock per kind, the stores of a kind share the dictionaries of the configuration.
/// </summary>
/// <typeparam name="T">the definition</typeparam>
public class DefinitionStore<T> where T : class
{
    /// <summary>
    /// Locks the dictionaries of the store, also those a derived store keeps next to the definitions
    /// </summary>
    protected static readonly object SyncRoot = new object();

    private readonly IDictionary<string, string> _definitions;
    private readonly IDictionary<string, string> _icons;
    private readonly IReadOnlyList<T> _builtIns;
    private readonly Action _changed;
    private readonly Func<string, string> _protect;
    private readonly Func<string, string> _unprotect;

    /// <param name="kind">reads, writes and names the definitions</param>
    /// <param name="definitions">id to protected definition: a <see cref="DefinitionFolder"/> (the id is the file name) or a dictionary</param>
    /// <param name="changed">Called after a change, saves the configuration</param>
    /// <param name="icons">id of the definition to its icon (base64 PNG), null: not kept</param>
    /// <param name="protect">Encrypts, null: <see cref="StringExtensions.Encrypt"/></param>
    /// <param name="unprotect">Decrypts, null: <see cref="StringExtensions.Decrypt"/> (which gives back what it can't decrypt, the JSON then can't be read)</param>
    /// <param name="builtIns">definitions which come with the plugin: listed with the stored ones, not stored, can't be changed or removed; their names can't be used</param>
    public DefinitionStore(IDefinitionKind<T> kind, IDictionary<string, string> definitions, Action changed, IDictionary<string, string> icons = null,
        Func<string, string> protect = null, Func<string, string> unprotect = null, IEnumerable<T> builtIns = null)
    {
        _builtIns = builtIns?.Where(builtIn => builtIn != null).ToList() ?? new List<T>();
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _icons = icons ?? new Dictionary<string, string>();
        _changed = changed ?? (() => { });
        _protect = protect ?? (text => text.Encrypt());
        _unprotect = unprotect ?? (text => string.IsNullOrEmpty(text) ? null : text.Decrypt());
    }

    public IDefinitionKind<T> Kind { get; }

    /// <summary>
    /// The prefix of the ids of the built-in definitions
    /// </summary>
    public const string BuiltInIdPrefix = "builtin:";

    /// <summary>
    /// The definitions, the built-in ones included, sorted by name; those which can't be read at the end.
    /// </summary>
    public IReadOnlyList<StoredDefinition<T>> GetAll()
    {
        lock (SyncRoot)
        {
            var stored = _definitions.Select(entry => Stored(entry.Key, ReadDefinition(entry.Value))).ToList();
            // A stored definition with the name of a built-in one (stored before the program was installed) takes its place
            return stored
                .Concat(_builtIns
                    .Where(builtIn => !stored.Any(s => s.IsUsable && string.Equals(s.Name, Kind.NameOf(builtIn), StringComparison.OrdinalIgnoreCase)))
                    .Select(builtIn => new StoredDefinition<T>(BuiltInIdPrefix + Kind.NameOf(builtIn), builtIn, Kind.NameOf(builtIn), true)))
                .OrderBy(stored => stored.Definition == null)
                .ThenBy(stored => stored.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }

    /// <summary>
    /// The definitions which are used: they can be read and are approved, sorted by name
    /// </summary>
    public IReadOnlyList<T> GetReadable() => GetAll().Where(stored => stored.IsUsable).Select(stored => stored.Definition).ToList();

    /// <summary>
    /// The definition with the name (ignoring case), null when there is none, it can't be read or isn't approved
    /// </summary>
    public StoredDefinition<T> Find(string name) =>
        string.IsNullOrEmpty(name) ? null : GetAll().FirstOrDefault(stored => stored.IsUsable && string.Equals(stored.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The definition with the name, or without a name the only one there is (a recipe written when there was one).
    /// Null when there is none, or no name and more than one.
    /// </summary>
    public T FindOrOnly(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return Find(name)?.Definition;
        }

        var readable = GetReadable();
        return readable.Count == 1 ? readable[0] : null;
    }

    /// <summary>
    /// Add the checked definition, it replaces the one with the same name (and loses its icon, it may be another service now).
    /// </summary>
    /// <returns>the id</returns>
    /// <exception cref="InvalidOperationException">the name is the one of a built-in definition</exception>
    public virtual string Add(T definition) => Add(definition, null);

    /// <summary>
    /// <see cref="Add(T)"/>, <paramref name="alsoForId"/> changes what a derived store keeps for the id, under the same lock and before the change is saved
    /// </summary>
    protected string Add(T definition, Action<string> alsoForId)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        string name = Kind.NameOf(definition);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The definition has no name", nameof(definition));

        if (IsBuiltInName(name)) throw new InvalidOperationException($"{name} comes with Greenshot, it can't be replaced");

        string id;
        lock (SyncRoot)
        {
            // Also one which can't be read anymore can't be replaced by name: it gets a new id. One which isn't approved is
            // replaced: what is written is approved.
            id = GetAll().FirstOrDefault(stored => !stored.IsBuiltIn && stored.Definition != null && string.Equals(stored.Name, name, StringComparison.OrdinalIgnoreCase))?.Id ?? (_definitions is IDefinitionIds ids ? ids.NewId(name) : Guid.NewGuid().ToString("N"));
            _definitions[id] = _protect(Kind.Write(definition));
            _icons.Remove(id);
            alsoForId?.Invoke(id);
        }

        _changed();
        return id;
    }

    /// <summary>
    /// Remove the definition and its icon
    /// </summary>
    /// <returns>false when there was none with the id</returns>
    public virtual bool Remove(string id) => Remove(id, null);

    /// <summary>
    /// <see cref="Remove(string)"/>, <paramref name="alsoForId"/> removes what a derived store keeps for the id, under the same lock and before the change is saved
    /// </summary>
    protected bool Remove(string id, Action<string> alsoForId)
    {
        bool removed;
        lock (SyncRoot)
        {
            removed = id != null && _definitions.Remove(id);
            if (removed)
            {
                _icons.Remove(id);
                alsoForId?.Invoke(id);
            }
        }

        if (removed)
        {
            _changed();
        }

        return removed;
    }

    /// <summary>
    /// The user approved the definition as it is (a file added or changed outside Greenshot)
    /// </summary>
    /// <returns>false when the definitions of the store need no approval or there is none with the id</returns>
    public bool Approve(string id)
    {
        lock (SyncRoot)
        {
            if (id == null || _definitions is not IDefinitionApprovals approvals || !_definitions.ContainsKey(id))
            {
                return false;
            }

            approvals.Approve(id);
        }

        _changed();
        return true;
    }

    /// <summary>
    /// True when a built-in definition has the name (ignoring case) and no stored one took its place: it can't be used for another one
    /// </summary>
    public bool IsBuiltInName(string name) =>
        name != null && GetAll().Any(stored => stored.IsBuiltIn && string.Equals(stored.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The icon of the definition (PNG), null when there is none or it can't be read
    /// </summary>
    public byte[] GetIcon(string id)
    {
        lock (SyncRoot)
        {
            if (id == null || !_icons.TryGetValue(id, out var base64))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Keep the icon of the definition, when it is still there (it can be removed while the icon is fetched)
    /// </summary>
    public void SetIcon(string id, byte[] png)
    {
        if (png == null || png.Length == 0) return;
        lock (SyncRoot)
        {
            if (id == null || !_definitions.ContainsKey(id))
            {
                return;
            }

            _icons[id] = Convert.ToBase64String(png);
        }

        _changed();
    }

    /// <summary>
    /// Encrypt a value a derived store keeps next to the definitions, the same way
    /// </summary>
    protected string Protect(string text) => _protect(text);

    /// <summary>
    /// Decrypt a value a derived store keeps next to the definitions, null when it can't
    /// </summary>
    protected string Unprotect(string text) => _unprotect(text);

    /// <summary>
    /// Tell the owner of the configuration about a change of what a derived store keeps
    /// </summary>
    protected void OnChanged() => _changed();

    private StoredDefinition<T> Stored(string id, T definition) =>
        new StoredDefinition<T>(id, definition, definition == null ? null : Kind.NameOf(definition), isApproved: _definitions is not IDefinitionApprovals approvals || approvals.IsApproved(id));

    private T ReadDefinition(string protectedJson)
    {
        string json = _unprotect(protectedJson);
        return json == null ? null : Kind.Read(json);
    }
}
