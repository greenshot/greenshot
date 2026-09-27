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

using System.Collections.Generic;
using System.Linq;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core.OutputFormats;

/// <summary>
/// Helper class representing an output format option. Used for UI elements like dropdowns to display available output formats with their IDs and display names.
/// </summary>
public sealed class OutputFormatOption
{
    public string Id { get; set; }

    public string DisplayName { get; set; }

    public string DisplayNameWithPreferredExtension { get; set; }
}

/// <summary>
/// Provides extension methods for the IOutputFormatRegistry interface, allowing retrieval of saveable file formats, display names, and format resolution based on requested or fallback IDs.
/// </summary>
public static class OutputFormatRegistryExtensions
{
    /// <summary>
    /// Returns a collection of OutputFormatDefinition objects that can be saved to files, based on the registered file format handlers in the system.
    /// </summary>
    /// <param name="registry"></param>
    /// <returns>A collection of OutputFormatDefinition objects that can be saved to files, ordered by their display names.</returns>
    public static IEnumerable<OutputFormatDefinition> GetSaveableFileFormats(this IOutputFormatRegistry registry)
    {
        if (registry == null)
        {
            return Enumerable.Empty<OutputFormatDefinition>();
        }

        var registeredHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>().ToArray();
        return registry.Formats.Where(format => format.Extensions.All(extension =>
            registeredHandlers.Any(handler => handler.Supports(FileFormatHandlerActions.SaveToFile, extension))))
            .OrderBy(format => format.GetDisplayName());
    }

    /// <summary>
    /// Returns the display name of the specified OutputFormatDefinition, using the translated name if available, or falling back to the fallback display name if not.
    /// </summary>
    /// <param name="format">The OutputFormatDefinition for which to get the display name.</param>
    /// <returns>The display name of the specified OutputFormatDefinition.</returns>
    public static string GetDisplayName(this OutputFormatDefinition format)
    {
        if (format != null && Language.TryGetString(format.DisplayNameResourceKey, out string translatedName) &&
            !string.IsNullOrWhiteSpace(translatedName))
        {
            return translatedName;
        }

        return format?.FallbackDisplayName;
    }

    public static string GetDisplayNameWithPreferredExtension(this OutputFormatDefinition format)
    {
        return format == null
            ? null
            : $"{format.GetDisplayName()} (.{format.PreferredExtension})";
    }

    public static string GetDisplayNameWithSupportedExtensions(this OutputFormatDefinition format)
    {
        return format == null
            ? null
            : $"{format.GetDisplayName()} ({string.Join(", ", format.Extensions.Select(extension => "." + extension))})";
    }

    public static IReadOnlyCollection<OutputFormatOption> GetSaveableFileFormatOptions(this IOutputFormatRegistry registry)
    {
        return registry.GetSaveableFileFormats()
            .Select(format => new OutputFormatOption
            {
                Id = format.Id,
                DisplayName = format.GetDisplayName(),
                DisplayNameWithPreferredExtension = format.GetDisplayNameWithPreferredExtension()
            })
            .ToArray();
    }

    /// <summary>
    /// Returns a collection of OutputFormatOption objects, ensuring that the current format ID is included in the collection.
    /// If the current format ID is not already present, it will be added with its ID as the display name.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="currentFormatId"></param>
    /// <returns></returns>
    public static IReadOnlyCollection<OutputFormatOption> WithCurrentFormat(
        this IReadOnlyCollection<OutputFormatOption> options,
        string currentFormatId)
    {
        var result = options?.ToList() ?? new List<OutputFormatOption>();
        if (!string.IsNullOrWhiteSpace(currentFormatId) &&
            !result.Any(option => string.Equals(option.Id, currentFormatId, System.StringComparison.OrdinalIgnoreCase)))
        {
            result.Add(new OutputFormatOption
            {
                Id = currentFormatId,
                DisplayName = currentFormatId,
                DisplayNameWithPreferredExtension = currentFormatId
            });
        }

        return result;
    }

    /// <summary>
    /// Resolves the appropriate output format ID based on the requested format ID and a fallback format ID.
    /// If the requested format ID is valid and registered, it will be returned.
    /// If not, the fallback format ID will be checked. If neither is valid, the default PNG format will be returned.
    /// </summary>
    /// <param name="registry">The output format registry to use for resolving format IDs.</param>
    /// <param name="requestedFormatId">The requested format ID.</param>
    /// <param name="fallbackFormatId">The fallback format ID to use if the requested format ID is not valid.</param>
    /// <returns>The resolved output format ID.</returns>
    public static string ResolveFormatId(this IOutputFormatRegistry registry, string requestedFormatId, string fallbackFormatId)
    {
        if (registry == null)
        {
            return !string.IsNullOrWhiteSpace(requestedFormatId)
                ? requestedFormatId
                : fallbackFormatId ?? WellKnownOutputFormats.Png;
        }

        if (!string.IsNullOrWhiteSpace(requestedFormatId) && registry.TryGet(requestedFormatId, out var requestedFormat))
        {
            return requestedFormat.Id;
        }

        if (!string.IsNullOrWhiteSpace(fallbackFormatId) && registry.TryGet(fallbackFormatId, out var fallbackFormat))
        {
            return fallbackFormat.Id;
        }

        return registry.TryGet(WellKnownOutputFormats.Png, out var pngFormat) ? pngFormat.Id : WellKnownOutputFormats.Png;
    }
}