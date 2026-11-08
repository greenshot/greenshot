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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using Greenshot.Base.Http;
using Greenshot.Base.Serialization;
using log4net;

namespace Greenshot.Plugin.CustomUploader.Sxcu;

/// <summary>
/// What an uploader made of the response: the link, the thumbnail, the deletion link, or the error.
/// </summary>
internal sealed class UploaderResponse
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(UploaderResponse));
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    private UploaderResponse(bool isSuccess, Uri url, Uri thumbnailUrl, Uri deletionUrl, string error)
    {
        IsSuccess = isSuccess;
        Url = url;
        ThumbnailUrl = thumbnailUrl;
        DeletionUrl = deletionUrl;
        Error = error;
    }

    /// <summary>
    /// True when the service accepted the upload (2xx)
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The link to the upload, null when the service didn't return one (or not an http(s) link)
    /// </summary>
    public Uri Url { get; }

    public Uri ThumbnailUrl { get; }

    /// <summary>
    /// The link which deletes the upload: a secret, whoever has it can delete the upload
    /// </summary>
    public Uri DeletionUrl { get; }

    /// <summary>
    /// The error for the user when the upload failed: the ErrorMessage of the uploader, or the status
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Read the response with the URL, ThumbnailURL, DeletionURL and ErrorMessage of the uploader.
    /// </summary>
    public static UploaderResponse From(UploaderDefinition definition, HttpResult result, string filename)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (result == null) throw new ArgumentNullException(nameof(result));

        string Replace(string text) => PlaceholderSyntax.Replace(text, (name, parameters) => Call(name, parameters, result, filename));

        if (!result.IsSuccessStatusCode)
        {
            string error = SafeReplace(Replace, definition.ErrorMessage);
            if (string.IsNullOrWhiteSpace(error))
            {
                // Without ErrorMessage the start of a text answer says more than the status (an HTML error page doesn't)
                string answer = result.Content?.Trim();
                error = string.IsNullOrEmpty(answer) || answer.StartsWith("<", StringComparison.Ordinal)
                    ? $"{(int)result.StatusCode} {result.ReasonPhrase}"
                    : $"{(int)result.StatusCode} {result.ReasonPhrase}: {answer}";
            }

            return new UploaderResponse(false, null, null, null, UploaderFile.Clean(error));
        }

        // An uploader without URL: the response is the link (like ShareX)
        string url = string.IsNullOrEmpty(definition.URL) ? result.Content?.Trim() : SafeReplace(Replace, definition.URL);
        return new UploaderResponse(true, ToLink(url), ToLink(SafeReplace(Replace, definition.ThumbnailURL)), ToLink(SafeReplace(Replace, definition.DeletionURL)), null);
    }

    private static string SafeReplace(Func<string, string> replace, string text)
    {
        try
        {
            return replace(text);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or XmlException or XPathException or ArgumentException or RegexMatchTimeoutException or InvalidOperationException)
        {
            // Not the expected answer (e.g. an HTML error page instead of JSON): no value, the type only, the content can hold secrets
            Log.WarnFormat("The response doesn't fit the uploader: {0}", ex.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Only http and https: a link is opened in the browser and put on the clipboard
    /// </summary>
    private static Uri ToLink(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp ? uri : null;
    }

    private static string Call(string name, IReadOnlyList<string> parameters, HttpResult result, string filename)
    {
        string response = result.Content ?? string.Empty;
        switch (name)
        {
            case "response":
                return response;
            case "responseurl":
                return result.RequestUri?.AbsoluteUri;
            case "header":
                return result.Headers.TryGetValue(parameters.FirstOrDefault() ?? string.Empty, out string header) ? header : null;
            case "filename":
                return filename;
            case "base64":
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(parameters.FirstOrDefault() ?? string.Empty));
            case "json":
                return parameters.Count > 1 ? Json(parameters[0], parameters[1]) : Json(response, parameters.FirstOrDefault());
            case "xml":
                return parameters.Count > 1 ? Xml(parameters[0], parameters[1]) : Xml(response, parameters.FirstOrDefault());
            case "regex":
                // {regex:pattern}, {regex:pattern|group} or {regex:input|pattern|group}, like ShareX
                return parameters.Count > 2
                    ? MatchRegex(parameters[0], parameters[1], parameters[2])
                    : MatchRegex(response, parameters.FirstOrDefault(), parameters.Count > 1 ? parameters[1] : null);
            default:
                // Refused when the uploader was imported
                throw new InvalidOperationException($"The placeholder {name} isn't supported in a response");
        }
    }

    /// <summary>
    /// {json:data.link}: a JSONPath, "$." in front is optional (ShareX writes it without).
    /// Names (.name or ['name']) and array indexes ([0]) are supported, no wildcards, filters or recursive descent.
    /// </summary>
    internal static string Json(string input, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        path = path.Trim();
        if (!path.StartsWith("$", StringComparison.Ordinal))
        {
            path = path.StartsWith("[", StringComparison.Ordinal) ? "$" + path : "$." + path;
        }

        var node = JsonNode.Parse(input, documentOptions: GreenshotJson.DocumentOptions);
        int position = 1;
        while (position < path.Length)
        {
            if (node == null)
            {
                return null;
            }

            int end;
            string name = null;
            int index = -1;
            if (path[position] == '.')
            {
                end = path.IndexOfAny(new[] { '.', '[' }, position + 1);
                end = end < 0 ? path.Length : end;
                name = path.Substring(position + 1, end - position - 1);
            }
            else if (path[position] == '[' && (end = path.IndexOf(']', position)) > 0)
            {
                string part = path.Substring(position + 1, end - position - 1).Trim();
                if (part.Length >= 2 && (part[0] == '\'' || part[0] == '"') && part[part.Length - 1] == part[0])
                {
                    name = part.Substring(1, part.Length - 2);
                }
                else if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out index))
                {
                    throw new InvalidOperationException("Only names and array indexes are supported in a JSON path");
                }
                end++;
            }
            else
            {
                throw new InvalidOperationException("Only names and array indexes are supported in a JSON path");
            }

            if (name is "" or "*")
            {
                throw new InvalidOperationException("Only names and array indexes are supported in a JSON path");
            }
            if (name != null)
            {
                if (node is not JsonObject jsonObject || !jsonObject.TryGetPropertyValue(name, out node))
                {
                    return null;
                }
            }
            else
            {
                if (node is not JsonArray array || index >= array.Count)
                {
                    return null;
                }
                node = array[index];
            }
            position = end;
        }

        return node switch
        {
            // A null value, as Json.NET gave it
            null => string.Empty,
            JsonValue value when value.GetValueKind() == JsonValueKind.String => value.GetValue<string>(),
            _ => node.ToJsonString(GreenshotJson.Options)
        };
    }

    /// <summary>
    /// {xml:/upload/link}: an XPath, the text of the first node. No DTD, no external entities.
    /// </summary>
    internal static string Xml(string input, string xpath)
    {
        if (string.IsNullOrWhiteSpace(xpath))
        {
            return null;
        }

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(new StringReader(input), settings);
        var navigator = new XPathDocument(reader).CreateNavigator();
        return navigator.SelectSingleNode(xpath)?.Value;
    }

    /// <summary>
    /// {regex:pattern|group}: the group by number or name, the whole match without a group
    /// </summary>
    internal static string MatchRegex(string input, string pattern, string group)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return null;
        }

        var match = Regex.Match(input, pattern, RegexOptions.CultureInvariant, RegexTimeout);
        if (!match.Success)
        {
            return null;
        }

        if (string.IsNullOrEmpty(group))
        {
            return match.Value;
        }

        return int.TryParse(group, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? match.Groups[number].Value : match.Groups[group].Value;
    }
}
