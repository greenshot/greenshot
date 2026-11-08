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
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Greenshot.Base.Definitions;

/// <summary>
/// A definition in the list of a settings page: icon, name and what it does. Read by a screen reader with <see cref="ToString"/>.
/// </summary>
public sealed class DefinitionListItem
{
    /// <param name="id">the id in the store</param>
    /// <param name="name">the name, or the text for a definition which can't be read</param>
    /// <param name="description">what it does (<see cref="IDefinitionKind{T}.Describe"/>), null when it can't be read</param>
    /// <param name="isReadable">false when the definition can't be read</param>
    /// <param name="icon">the icon of the definition, else the generic one</param>
    /// <param name="isBuiltIn">comes with the plugin, can't be changed or removed</param>
    /// <param name="isApproved">false for a file added or changed outside Greenshot which the user didn't approve yet</param>
    public DefinitionListItem(string id, string name, string description, bool isReadable, ImageSource icon, bool isBuiltIn = false, bool isApproved = true)
    {
        IsApproved = isApproved;
        IsBuiltIn = isBuiltIn;
        Id = id;
        Name = name;
        Description = description;
        IsReadable = isReadable;
        Icon = icon;
    }

    /// <summary>
    /// The item of a stored definition
    /// </summary>
    /// <param name="unreadableName">the name shown for a definition which can't be read</param>
    /// <param name="notApprovedFormat">the description of a definition which isn't approved, {0} is what it does</param>
    public static DefinitionListItem From<T>(DefinitionStore<T> store, StoredDefinition<T> stored, string unreadableName, ImageSource genericIcon, string notApprovedFormat = "{0}") where T : class
    {
        if (store == null) throw new ArgumentNullException(nameof(store));
        if (stored == null) throw new ArgumentNullException(nameof(stored));
        return new DefinitionListItem(stored.Id, stored.Definition != null ? stored.Name : unreadableName, DescriptionOf(store.Kind, stored, notApprovedFormat),
            stored.Definition != null, LoadIcon(store.GetIcon(stored.Id)) ?? genericIcon, stored.IsBuiltIn, stored.IsApproved);
    }

    /// <summary>
    /// What the definition does, with <paramref name="notApprovedFormat"/> when it isn't approved; null when it can't be read
    /// </summary>
    public static string DescriptionOf<T>(IDefinitionKind<T> kind, StoredDefinition<T> stored, string notApprovedFormat) where T : class
    {
        if (stored?.Definition == null) return null;
        string description = kind.Describe(stored.Definition);
        return stored.IsApproved ? description : string.Format(notApprovedFormat ?? "{0}", description);
    }

    public string Id { get; }

    public string Name { get; }

    /// <summary>
    /// What it does, null when the definition can't be read
    /// </summary>
    public string Description { get; }

    public bool IsReadable { get; }

    /// <summary>
    /// Comes with the plugin: can't be changed or removed
    /// </summary>
    public bool IsBuiltIn { get; }

    /// <summary>
    /// False for a file added or changed outside Greenshot which the user didn't approve yet: it isn't used
    /// </summary>
    public bool IsApproved { get; }

    /// <summary>
    /// Can be read and isn't approved: the user can approve it
    /// </summary>
    public bool CanBeApproved => IsReadable && !IsApproved;

    /// <summary>
    /// A stored and approved definition which can be read: it can be changed
    /// </summary>
    public bool IsEditable => IsReadable && IsApproved && !IsBuiltIn;

    public ImageSource Icon { get; }

    /// <summary>
    /// The text a screen reader reads for the row
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Description) ? Name : $"{Name}, {Description}";

    /// <summary>
    /// A stored icon (PNG) for WPF, frozen; null when there is none or it can't be read
    /// </summary>
    public static ImageSource LoadIcon(byte[] png)
    {
        if (png == null)
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(png);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException)
        {
            return null;
        }
    }
}
