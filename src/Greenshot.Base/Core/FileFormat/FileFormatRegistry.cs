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
/// Represents a registry for managing supported file format definitions, allowing registration and retrieval of formats by ID, extension, or MIME type.
/// </summary>
public sealed class FileFormatRegistry : IFileFormatRegistry
{
    private readonly object _syncRoot = new object();
    private readonly List<FileFormatDefinition> _formats = new List<FileFormatDefinition>();
    private readonly Dictionary<string, FileFormatDefinition> _formatsById = new Dictionary<string, FileFormatDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FileFormatDefinition> _formatsByExtension = new Dictionary<string, FileFormatDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FileFormatDefinition> _formatsByMimeType = new Dictionary<string, FileFormatDefinition>(StringComparer.OrdinalIgnoreCase);

    public static string GetPreferredExtension(string formatId)
    {
        var supportedFileFormatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        if (supportedFileFormatRegistry != null && supportedFileFormatRegistry.TryGet(formatId, out var format))
        {
            return format.PreferredExtension;
        }

        if (supportedFileFormatRegistry != null || string.IsNullOrWhiteSpace(formatId))
        {
            return WellKnownFileFormats.Png;
        }

        return formatId.Trim().TrimStart('.').ToLowerInvariant();
    }

    public static string GetPreferredExtensionWithDot(string formatId)
    {
        return "." + GetPreferredExtension(formatId);
    }

    public IReadOnlyCollection<FileFormatDefinition> Formats
    {
        get
        {
            lock (_syncRoot)
            {
                return new ReadOnlyCollection<FileFormatDefinition>(_formats.ToArray());
            }
        }
    }

    public void Register(FileFormatDefinition format)
    {
        if (format == null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        lock (_syncRoot)
        {
            EnsureCanRegister(format);
            Add(format);
        }
    }

    public bool RegisterIfMissing(FileFormatDefinition format)
    {
        if (format == null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        lock (_syncRoot)
        {
            if (_formatsById.TryGetValue(format.Id, out var existing))
            {
                if (!AreEquivalent(existing, format))
                {
                    throw new InvalidOperationException($"File format '{format.Id}' is already registered with different metadata.");
                }

                return false;
            }

            EnsureCanRegister(format);
            Add(format);
            return true;
        }
    }

    public bool TryGet(string id, out FileFormatDefinition format)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            format = null;
            return false;
        }

        lock (_syncRoot)
        {
            return _formatsById.TryGetValue(id.Trim(), out format);
        }
    }

    public FileFormatDefinition GetByExtension(string extension)
    {
        string normalized = NormalizeLookupValue(extension, true);
        if (normalized == null)
        {
            return null;
        }

        lock (_syncRoot)
        {
            _formatsByExtension.TryGetValue(normalized, out var format);
            return format;
        }
    }

    public FileFormatDefinition GetByMimeType(string mimeType)
    {
        string normalized = NormalizeLookupValue(mimeType, false);
        if (normalized == null)
        {
            return null;
        }

        int parameterSeparator = normalized.IndexOf(';');
        if (parameterSeparator >= 0)
        {
            normalized = normalized.Substring(0, parameterSeparator).Trim();
        }

        lock (_syncRoot)
        {
            _formatsByMimeType.TryGetValue(normalized, out var format);
            return format;
        }
    }

    private void EnsureCanRegister(FileFormatDefinition format)
    {
        if (_formatsById.ContainsKey(format.Id))
        {
            throw new InvalidOperationException($"File format '{format.Id}' is already registered.");
        }

        foreach (string extension in format.Extensions)
        {
            if (_formatsByExtension.TryGetValue(extension, out var existing))
            {
                throw new InvalidOperationException($"Extension '{extension}' is already registered for file format '{existing.Id}'.");
            }
        }

        foreach (string mimeType in GetMimeTypes(format))
        {
            if (_formatsByMimeType.TryGetValue(mimeType, out var existing))
            {
                throw new InvalidOperationException($"MIME type '{mimeType}' is already registered for file format '{existing.Id}'.");
            }
        }
    }

    private void Add(FileFormatDefinition format)
    {
        _formats.Add(format);
        _formatsById.Add(format.Id, format);
        foreach (string extension in format.Extensions)
        {
            _formatsByExtension.Add(extension, format);
        }

        foreach (string mimeType in GetMimeTypes(format))
        {
            _formatsByMimeType.Add(mimeType, format);
        }
    }

    private static IEnumerable<string> GetMimeTypes(FileFormatDefinition format)
    {
        yield return format.MimeType;
        foreach (string alias in format.MimeTypeAliases)
        {
            yield return alias;
        }
    }

    private static bool AreEquivalent(FileFormatDefinition left, FileFormatDefinition right)
    {
        return string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.PreferredExtension, right.PreferredExtension, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.MimeType, right.MimeType, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.DisplayNameResourceKey, right.DisplayNameResourceKey, StringComparison.Ordinal) &&
               string.Equals(left.FallbackDisplayName, right.FallbackDisplayName, StringComparison.Ordinal) &&
               left.Extensions.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).SequenceEqual(right.Extensions.OrderBy(value => value, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase) &&
               left.MimeTypeAliases.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).SequenceEqual(right.MimeTypeAliases.OrderBy(value => value, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeLookupValue(string value, bool isExtension)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (isExtension)
        {
            normalized = normalized.TrimStart('.');
        }

        return normalized;
    }
}