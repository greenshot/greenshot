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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Greenshot.Plugin.Atlassian.Confluence.Api;

// Only the fields Greenshot reads from the Confluence REST API, ids come as strings

/// <summary>
/// A page, from GET rest/api/content/search
/// </summary>
public sealed class ConfluenceContent
{
    public long Id { get; set; }

    public string Type { get; set; }

    public string Title { get; set; }

    public ConfluenceSpace Space { get; set; }

    [JsonPropertyName("_links")]
    public ConfluenceLinks Links { get; set; }
}

public sealed class ConfluenceLinks
{
    /// <summary>
    /// The page in the browser, relative to the Confluence url
    /// </summary>
    public string WebUi { get; set; }

    /// <summary>
    /// The next page of a list
    /// </summary>
    public string Next { get; set; }
}

/// <summary>
/// A space, from GET rest/api/space (v1) or GET api/v2/spaces (Cloud)
/// </summary>
public sealed class ConfluenceSpace
{
    public string Key { get; set; }

    public string Name { get; set; }

    /// <summary>
    /// v1 with expand=homepage
    /// </summary>
    public ConfluenceContent Homepage { get; set; }

    /// <summary>
    /// v2
    /// </summary>
    public long? HomepageId { get; set; }
}

/// <summary>
/// The signed in user, from GET rest/api/user/current
/// </summary>
public sealed class ConfluenceUser
{
    /// <summary>
    /// known or anonymous
    /// </summary>
    public string Type { get; set; }

    public string DisplayName { get; set; }
}

/// <summary>
/// The answer of a list call
/// </summary>
public sealed class ConfluenceResults<T>
{
    public IList<T> Results { get; set; }

    [JsonPropertyName("_links")]
    public ConfluenceLinks Links { get; set; }
}

/// <summary>
/// The error answer of Confluence, e.g. for an attachment name the page already has
/// </summary>
public sealed class ConfluenceError
{
    public string Message { get; set; }
}

/// <summary>
/// The serializer for the Confluence types, generated at compile time
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(ConfluenceResults<ConfluenceContent>))]
[JsonSerializable(typeof(ConfluenceResults<ConfluenceSpace>))]
[JsonSerializable(typeof(ConfluenceUser))]
[JsonSerializable(typeof(ConfluenceError))]
internal sealed partial class ConfluenceJsonContext : JsonSerializerContext
{
}
