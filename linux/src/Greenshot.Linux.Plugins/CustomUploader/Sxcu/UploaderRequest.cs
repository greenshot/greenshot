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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Greenshot.Linux.Core.Http;

namespace Greenshot.Linux.Plugins.CustomUploader.Sxcu;

/// <summary>
/// The request which uploads a capture with an uploader.
/// </summary>
public static class UploaderRequest
{
    /// <summary>
    /// Create the request: url with parameters, headers and the body with the file.
    /// </summary>
    /// <param name="definition">The checked uploader</param>
    /// <param name="image">The encoded capture</param>
    /// <param name="filename">The name of the file, {filename}</param>
    /// <param name="progress">Receives the upload progress, or null</param>
    public static HttpRequestMessage Create(UploaderDefinition definition, EncodedImage image, string filename, IProgress<ProgressInfo> progress)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (image == null) throw new ArgumentNullException(nameof(image));

        string Replace(string text, bool urlEncode = false) => PlaceholderSyntax.Replace(text, (name, parameters) => Call(name, parameters, filename), urlEncode);

        var url = new StringBuilder(Replace(definition.RequestURL, true));
        foreach (var parameter in definition.Parameters ?? new Dictionary<string, string>())
        {
            url.Append(url.ToString().Contains("?") ? '&' : '?');
            url.Append(Uri.EscapeDataString(parameter.Key)).Append('=').Append(Uri.EscapeDataString(Replace(parameter.Value) ?? string.Empty));
        }

        var request = new HttpRequestMessage(new HttpMethod(definition.RequestMethod), url.ToString());
        if (!UploaderFile.GetServer(definition).Equals(new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority))))
        {
            // Can't happen for a checked uploader, the server has no placeholders
            request.Dispose();
            throw new InvalidOperationException("The request doesn't go to the server of the uploader");
        }

        HttpContent content;
        if (definition.Body == "Binary")
        {
            content = HttpContents.Upload(image, progress);
        }
        else
        {
            var fields = (definition.Arguments ?? new Dictionary<string, string>()).Select(argument => new KeyValuePair<string, string>(argument.Key, Replace(argument.Value) ?? string.Empty));
            content = HttpContents.Multipart(definition.FileFormName, image, filename, fields, progress);
        }

        foreach (var header in definition.Headers ?? new Dictionary<string, string>())
        {
            string value = Replace(header.Value) ?? string.Empty;
            if (value.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
            {
                throw new InvalidOperationException($"The value of header {header.Key} has a line break");
            }

            if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                // The type of a multipart body carries its boundary, only a binary body can have another type
                if (definition.Body == "Binary" && header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && MediaTypeHeaderValue.TryParse(value, out var mediaType))
                {
                    content.Headers.ContentType = mediaType;
                }
                else if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    content.Headers.TryAddWithoutValidation(header.Key, value);
                }

                continue;
            }

            request.Headers.TryAddWithoutValidation(header.Key, value);
        }

        request.Content = content;
        return request;
    }

    private static string Call(string name, IReadOnlyList<string> parameters, string filename) => name switch
    {
        "filename" => filename,
        "base64" => Convert.ToBase64String(Encoding.UTF8.GetBytes(parameters.FirstOrDefault() ?? string.Empty)),
        // Refused when the uploader was imported
        _ => throw new InvalidOperationException($"The placeholder {name} isn't supported in a request")
    };
}
