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
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Plugin.Jira.Api;

// Only the fields Greenshot reads, the Jira REST API (v2 on Server/Data Center and Cloud, v3 search on Cloud) sends more.

/// <summary>
/// An issue, from GET issue/{key} or a search
/// </summary>
public sealed class JiraIssue
{
    public string Key { get; set; }

    public JiraIssueFields Fields { get; set; }
}

public sealed class JiraIssueFields
{
    public string Summary { get; set; }

    // "issuetype" in the JSON, the names are matched case insensitive
    public JiraIssueType IssueType { get; set; }

    public JiraUser Reporter { get; set; }

    public JiraUser Assignee { get; set; }

    [JsonConverter(typeof(JiraDateTimeOffsetConverter))]
    public DateTimeOffset? Created { get; set; }
}

public sealed class JiraIssueType
{
    public string Name { get; set; }

    public Uri IconUrl { get; set; }
}

public sealed class JiraUser
{
    public string DisplayName { get; set; }
}

/// <summary>
/// A filter, from GET filter/favourite
/// </summary>
public sealed class JiraFilter
{
    public string Name { get; set; }

    public string Jql { get; set; }
}

public sealed class JiraProject
{
    public string Key { get; set; }
}

public sealed class JiraServerInfo
{
    /// <summary>
    /// "Cloud", "Server" or "DataCenter"
    /// </summary>
    public string DeploymentType { get; set; }
}

/// <summary>
/// The body of POST search (Server/Data Center) and POST search/jql (Cloud)
/// </summary>
public sealed class JiraSearchRequest
{
    public string Jql { get; set; }

    public IList<string> Fields { get; set; }

    public int MaxResults { get; set; }
}

public sealed class JiraSearchResult
{
    public IList<JiraIssue> Issues { get; set; }
}

/// <summary>
/// The body of POST issue/{key}/comment, v2 takes plain text
/// </summary>
public sealed class JiraComment
{
    public string Body { get; set; }
}

/// <summary>
/// Jira writes the offset without colon ("2026-10-20T21:15:00.000+0200"), which System.Text.Json doesn't read
/// </summary>
public sealed class JiraDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        // +0200 -> +02:00
        if (value.Length > 5 && (value[value.Length - 5] == '+' || value[value.Length - 5] == '-') && char.IsDigit(value[value.Length - 1]))
        {
            value = value.Insert(value.Length - 2, ":");
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// The serializer for the Jira types, generated at compile time
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JiraIssue))]
[JsonSerializable(typeof(List<JiraFilter>))]
[JsonSerializable(typeof(List<JiraProject>))]
[JsonSerializable(typeof(JiraServerInfo))]
[JsonSerializable(typeof(JiraSearchRequest))]
[JsonSerializable(typeof(JiraSearchResult))]
[JsonSerializable(typeof(JiraComment))]
[JsonSerializable(typeof(JiraUser))]
internal sealed partial class JiraJsonContext : JsonSerializerContext
{
}
