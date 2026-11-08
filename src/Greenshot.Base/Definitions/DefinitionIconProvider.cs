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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Definitions;

/// <summary>
/// The icon keys of definitions: "designation:" (lower case) for the destination of the kind, "designation:name" for one definition.
/// </summary>
public static class DefinitionIcons
{
    /// <summary>
    /// The key of the destination of the kind: the icon of the only definition, else the generic one
    /// </summary>
    public static string GenericKeyFor<T>(IDefinitionKind<T> kind) where T : class => kind.Designation.ToLowerInvariant() + ":";

    public static string KeyFor<T>(IDefinitionKind<T> kind, string name) where T : class => GenericKeyFor(kind) + name;
}

/// <summary>
/// The icons of the destinations of a kind: the stored icon of the definition, else the generic icon of the kind.
/// </summary>
/// <typeparam name="T">the definition</typeparam>
public abstract class DefinitionIconProvider<T> : IIconProvider where T : class
{
    private readonly string _prefix;

    protected DefinitionIconProvider(IDefinitionKind<T> kind)
    {
        _prefix = DefinitionIcons.GenericKeyFor(kind ?? throw new ArgumentNullException(nameof(kind)));
    }

    /// <summary>
    /// The store with the definitions and their icons, read for each icon
    /// </summary>
    protected abstract DefinitionStore<T> CreateStore();

    /// <summary>
    /// The icon of the kind, for a definition without one; the caller owns it
    /// </summary>
    protected abstract Image CreateGenericIcon();

    /// <summary>
    /// The definition has no icon (yet), e.g. fetch it in the background; the menus ask again the next time
    /// </summary>
    protected virtual void OnIconMissing(DefinitionStore<T> store, StoredDefinition<T> stored)
    {
    }

    public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(_prefix, StringComparison.Ordinal);

    public Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
    {
        if (!CanProvide(iconKey)) throw new ArgumentException($"Not a key of {_prefix}", nameof(iconKey));
        string name = iconKey.Substring(_prefix.Length);
        var store = CreateStore();
        // The destination of the kind: with one definition that is what it uses, so it gets its icon
        var stored = name.Length > 0 ? store.Find(name) : SingleDefinition(store);
        byte[] png = store.GetIcon(stored?.Id);
        if (png == null && stored?.Definition != null)
        {
            OnIconMissing(store, stored);
        }

        return Task.FromResult(png == null ? CreateGenericIcon() : FromPng(png) ?? CreateGenericIcon());
    }

    private static StoredDefinition<T> SingleDefinition(DefinitionStore<T> store)
    {
        StoredDefinition<T> single = null;
        foreach (var stored in store.GetAll())
        {
            if (stored.Definition == null) continue;
            if (single != null) return null;
            single = stored;
        }

        return single;
    }

    /// <summary>
    /// A new bitmap, the stream isn't kept (an Image from a stream needs it as long as it lives)
    /// </summary>
    private static Image FromPng(byte[] png)
    {
        try
        {
            using var stream = new MemoryStream(png);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
