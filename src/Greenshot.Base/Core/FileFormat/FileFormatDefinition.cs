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
using System.Collections.ObjectModel;
using System.Linq;

namespace Greenshot.Base.Core.FileFormat;

/// <summary>
/// Represents a definition of a file format, including loadable and saveable extensions, MIME type, and display name.
/// </summary>
public sealed class FileFormatDefinition
{
    public FileFormatDefinition(
        string id,
        IEnumerable<string> loadableExtensions,
        IEnumerable<string> saveableExtensions,
        string preferredExtension,
        string mimeType,
        IEnumerable<string> mimeTypeAliases,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A format ID is required.", nameof(id));
        }

        Id = id.Trim().ToLowerInvariant();
        LoadableExtensions = NormalizeExtensions(loadableExtensions, nameof(loadableExtensions));
        SaveableExtensions = NormalizeExtensions(saveableExtensions, nameof(saveableExtensions));
        if (LoadableExtensions.Count == 0 && SaveableExtensions.Count == 0)
        {
            throw new ArgumentException("At least one loadable or saveable extension is required.", nameof(loadableExtensions));
        }

        PreferredExtension = NormalizeExtension(preferredExtension);
        IReadOnlyCollection<string> preferredExtensionList = SaveableExtensions.Count > 0 ? SaveableExtensions : LoadableExtensions;
        if (!preferredExtensionList.Contains(PreferredExtension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The preferred extension must be saveable, or loadable when no saveable extensions exist.", nameof(preferredExtension));
        }

        MimeType = NormalizeMimeType(mimeType, nameof(mimeType));
        MimeTypeAliases = NormalizeMimeTypes(mimeTypeAliases);
        if (MimeTypeAliases.Contains(MimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A MIME alias cannot duplicate the canonical MIME type.", nameof(mimeTypeAliases));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
    }

    public string Id { get; }

    public IReadOnlyCollection<string> LoadableExtensions { get; }

    public IReadOnlyCollection<string> SaveableExtensions { get; }

    public string PreferredExtension { get; }

    public string MimeType { get; }

    public IReadOnlyCollection<string> MimeTypeAliases { get; }

    public string DisplayName { get; }

    public bool CanSave => SaveableExtensions.Count > 0;

    public bool CanOpen => LoadableExtensions.Count > 0;

    private static IReadOnlyCollection<string> NormalizeExtensions(IEnumerable<string> extensions, string parameterName)
    {
        if (extensions == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalized = extensions.Select(extension => NormalizeExtension(extension))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ReadOnlyCollection<string>(normalized);
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("An extension cannot be empty.", nameof(extension));
        }

        string normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (normalized.Length == 0 || normalized.Any(character => !char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != '+'))
        {
            throw new ArgumentException($"'{extension}' is not a valid extension.", nameof(extension));
        }

        return normalized;
    }

    private static IReadOnlyCollection<string> NormalizeMimeTypes(IEnumerable<string> mimeTypes)
    {
        if (mimeTypes == null)
        {
            return new ReadOnlyCollection<string>(Array.Empty<string>());
        }

        var normalized = mimeTypes.Select(mimeType => NormalizeMimeType(mimeType, nameof(mimeTypes)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new ReadOnlyCollection<string>(normalized);
    }

    private static string NormalizeMimeType(string mimeType, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("A MIME type is required.", parameterName);
        }

        string normalized = mimeType.Trim().ToLowerInvariant();
        int separator = normalized.IndexOf('/');
        if (separator <= 0 || separator == normalized.Length - 1 ||
            normalized.IndexOf('/', separator + 1) >= 0 ||
            normalized.Take(separator).Any(character => !IsMimeTokenCharacter(character)) ||
            normalized.Skip(separator + 1).Any(character => !IsMimeTokenCharacter(character)))
        {
            throw new ArgumentException($"'{mimeType}' is not a valid MIME type without parameters.", parameterName);
        }

        return normalized;
    }

    private static bool IsMimeTokenCharacter(char character)
    {
        return character >= 'a' && character <= 'z' || character >= '0' && character <= '9' ||
               "!#$%&'*+-.^_`|~".IndexOf(character) >= 0;
    }
}