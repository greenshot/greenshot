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

// Ported from Greenshot.Plugin.CustomUploader, which has no nullable annotations
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Serialization;

namespace Greenshot.Linux.Plugins.CustomUploader.Sxcu;

/// <summary>
/// Reads and checks an uploader file (.sxcu): only what Greenshot can send safely is accepted, everything else is refused with
/// a reason for the user. Nothing of the file is logged, it can hold the key to the user's account.
/// </summary>
public static class UploaderFile
{
    /// <summary>
    /// Uploader files are a few KB, a larger file isn't one
    /// </summary>
    public const int MaximumLength = 256 * 1024;

    private const string MultipartFormData = "MultipartFormData";
    private const string Binary = "Binary";

    private static readonly string[] Methods = { "POST", "PUT", "PATCH" };

    /// <summary>
    /// Reads uploader files as Json.NET (which ShareX uses) does: comments, any casing, numbers where texts are expected.
    /// Property names as the format has them, types come from the class, never from the file.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = GreenshotJson.CreateOptions();
        options.MaxDepth = 16;
        return options;
    }

    /// <summary>
    /// What the request (url, parameters, headers, form fields) can use: nothing which asks the user or picks at random,
    /// the user approves exactly what is sent where
    /// </summary>
    internal static readonly ISet<string> RequestPlaceholders = new HashSet<string>(StringComparer.Ordinal) { "filename", "base64" };

    /// <summary>
    /// What the link, thumbnail, deletion link and error message can use
    /// </summary>
    internal static readonly ISet<string> ResponsePlaceholders = new HashSet<string>(StringComparer.Ordinal)
    {
        "response", "responseurl", "header", "json", "xml", "regex", "filename", "base64"
    };

    // The placeholders of ShareX before 13.7: $json:...$, $response$ etc.
    private static readonly Regex OldSyntax = new Regex(@"\$(json|xml|regex|header|response|responseurl|filename|random|select|prompt|base64)[:$]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex HeaderName = new Regex("^[!#$%&'*+.^_`|~0-9A-Za-z-]+$", RegexOptions.CultureInvariant);

    private static CustomUploaderTexts Language => CustomUploaderTexts.Instance;

    /// <summary>
    /// Read the uploader from the content of an .sxcu file.
    /// </summary>
    /// <exception cref="UploaderFileException">the file can't be used, the message says why</exception>
    public static UploaderDefinition Read(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (json.Length > MaximumLength)
        {
            throw new UploaderFileException(Language.ErrorFileTooLarge);
        }

        UploaderDefinition definition;
        try
        {
            definition = JsonSerializer.Deserialize<UploaderDefinition>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new UploaderFileException(Language.ErrorNotJson, ex);
        }

        if (definition == null)
        {
            throw new UploaderFileException(Language.ErrorNotJson);
        }

        Normalize(definition);
        Check(definition);
        return definition;
    }

    /// <summary>
    /// The server the uploader sends the captures to: scheme, host and port, what the user approves.
    /// </summary>
    public static Uri GetServer(UploaderDefinition definition)
    {
        // The host has no placeholders (checked), the path may have them
        string url = PlaceholderSyntax.Replace(definition.RequestURL, (name, parameters) => "x", true);
        return new Uri(new Uri(url).GetLeftPart(UriPartial.Authority));
    }

    /// <summary>
    /// The server for a user: the host, with the port when it isn't the default one
    /// </summary>
    public static string DescribeServer(UploaderDefinition definition)
    {
        var server = GetServer(definition);
        return server.IsDefaultPort ? server.Host : server.Authority;
    }

    private static void Normalize(UploaderDefinition definition)
    {
        definition.Name = Clean(definition.Name);
        definition.RequestURL = definition.RequestURL?.Trim();
        definition.RequestMethod = (definition.RequestMethod ?? definition.RequestType ?? "POST").Trim().ToUpperInvariant();
        definition.RequestType = null;
        if (string.IsNullOrWhiteSpace(definition.Body) || string.Equals(definition.Body, "None", StringComparison.OrdinalIgnoreCase))
        {
            // Files of ShareX before 13 have no body, a file field means a form
            definition.Body = string.IsNullOrWhiteSpace(definition.FileFormName) ? definition.Body : MultipartFormData;
        }
    }

    private static void Check(UploaderDefinition definition)
    {
        var types = (definition.DestinationType ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
        if (!types.Any(t => t.Equals("ImageUploader", StringComparison.OrdinalIgnoreCase) || t.Equals("FileUploader", StringComparison.OrdinalIgnoreCase)))
        {
            throw new UploaderFileException(Language.ErrorNotFileUploader);
        }

        if (!Methods.Contains(definition.RequestMethod))
        {
            throw new UploaderFileException(string.Format(Language.ErrorMethod, definition.RequestMethod));
        }

        if (string.IsNullOrEmpty(definition.RequestURL))
        {
            throw new UploaderFileException(Language.ErrorNoUrl);
        }

        if (IsOldSyntax(definition))
        {
            throw new UploaderFileException(Language.ErrorOldSyntax);
        }

        CheckServer(definition.RequestURL);

        if (string.Equals(definition.Body, MultipartFormData, StringComparison.OrdinalIgnoreCase))
        {
            definition.Body = MultipartFormData;
            if (string.IsNullOrWhiteSpace(definition.FileFormName))
            {
                throw new UploaderFileException(Language.ErrorNoFileFormName);
            }
        }
        else if (string.Equals(definition.Body, Binary, StringComparison.OrdinalIgnoreCase))
        {
            definition.Body = Binary;
        }
        else
        {
            throw new UploaderFileException(string.Format(Language.ErrorBody, definition.Body ?? "None"));
        }

        CheckPlaceholders(nameof(UploaderDefinition.RequestURL), definition.RequestURL, RequestPlaceholders);
        foreach (var parameter in definition.Parameters ?? new Dictionary<string, string>())
        {
            CheckPlaceholders(nameof(UploaderDefinition.Parameters), parameter.Value, RequestPlaceholders);
        }

        foreach (var argument in definition.Arguments ?? new Dictionary<string, string>())
        {
            CheckPlaceholders(nameof(UploaderDefinition.Arguments), argument.Value, RequestPlaceholders);
        }

        foreach (var header in definition.Headers ?? new Dictionary<string, string>())
        {
            if (!HeaderName.IsMatch(header.Key ?? string.Empty) || (header.Value ?? string.Empty).Any(c => c == '\r' || c == '\n' || c == '\0'))
            {
                throw new UploaderFileException(string.Format(Language.ErrorHeader, Clean(header.Key)));
            }

            CheckPlaceholders(nameof(UploaderDefinition.Headers), header.Value, RequestPlaceholders);
        }

        CheckPlaceholders(nameof(UploaderDefinition.URL), definition.URL, ResponsePlaceholders);
        CheckPlaceholders(nameof(UploaderDefinition.ThumbnailURL), definition.ThumbnailURL, ResponsePlaceholders);
        CheckPlaceholders(nameof(UploaderDefinition.DeletionURL), definition.DeletionURL, ResponsePlaceholders);
        CheckPlaceholders(nameof(UploaderDefinition.ErrorMessage), definition.ErrorMessage, ResponsePlaceholders);

        if (string.IsNullOrEmpty(definition.Name))
        {
            definition.Name = DescribeServer(definition);
        }
    }

    private static void CheckServer(string requestUrl)
    {
        // The part up to the path is the server: it must be fixed, the user approves it
        int schemeEnd = requestUrl.IndexOf("://", StringComparison.Ordinal);
        int authorityEnd = schemeEnd < 0 ? -1 : requestUrl.IndexOfAny(new[] { '/', '?', '#' }, schemeEnd + 3);
        string server = schemeEnd < 0 ? requestUrl : authorityEnd < 0 ? requestUrl : requestUrl.Substring(0, authorityEnd);
        if (server.IndexOfAny(new[] { '{', '}', '\\' }) >= 0)
        {
            throw new UploaderFileException(Language.ErrorDynamicHost);
        }

        if (!Uri.TryCreate(server, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new UploaderFileException(Language.ErrorNoUrl);
        }

        bool isHttps = uri.Scheme == Uri.UriSchemeHttps;
        bool isLocalHttp = uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        if (!isHttps && !isLocalHttp)
        {
            throw new UploaderFileException(Language.ErrorNotHttps);
        }
    }

    private static void CheckPlaceholders(string field, string text, ISet<string> allowed)
    {
        IReadOnlyList<string> names;
        try
        {
            names = PlaceholderSyntax.FindNames(text);
        }
        catch (FormatException ex)
        {
            throw new UploaderFileException(string.Format(Language.ErrorSyntax, field), ex);
        }

        string unsupported = names.FirstOrDefault(name => !allowed.Contains(name));
        if (unsupported != null)
        {
            throw new UploaderFileException(string.Format(Language.ErrorFunction, "{" + Clean(unsupported) + "}", field));
        }
    }

    private static bool IsOldSyntax(UploaderDefinition definition)
    {
        if (Version.TryParse(definition.Version, out var version) && version >= new Version(13, 7))
        {
            return false;
        }

        var texts = new[] { definition.RequestURL, definition.URL, definition.ThumbnailURL, definition.DeletionURL, definition.ErrorMessage }
            .Concat(definition.Parameters?.Values ?? Enumerable.Empty<string>())
            .Concat(definition.Headers?.Values ?? Enumerable.Empty<string>())
            .Concat(definition.Arguments?.Values ?? Enumerable.Empty<string>());
        return texts.Any(text => text != null && OldSyntax.IsMatch(text));
    }

    /// <summary>
    /// A text from the file for the user: one line, no control characters, not endless
    /// </summary>
    internal static string Clean(string text)
    {
        if (text == null)
        {
            return null;
        }

        string cleaned = new string(text.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return cleaned.Length > 100 ? cleaned.Substring(0, 100) : cleaned;
    }
}
