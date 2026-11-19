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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;

namespace Greenshot.Plugin.Divoom.Device;

/// <summary>
/// The HTTP API of the Divoom devices with Wi-Fi (Pixoo 64): every command is a JSON object posted to http://device/post,
/// the device answers {"error_code": 0}. The devices are found with the server of Divoom, which lists the devices of the
/// network the request comes from. Commands and fields as in https://github.com/PanoramicData/Divoom.Api (MIT) and
/// the API documentation of Divoom (http://doc.divoom-gz.com).
/// Nothing that is sent (the picture) is logged, see HttpService.
/// </summary>
public static class DivoomClient
{
    /// <summary>
    /// Lists the devices in the network of the caller
    /// </summary>
    public const string DiscoveryUrl = "https://app.divoom-gz.com/Device/ReturnSameLANDevice";

    // The device answers within a moment, when it doesn't it is off or not in this network
    private static readonly TimeSpan DeviceTimeout = TimeSpan.FromSeconds(10);

    // The base64 of the picture as it is: the default encoder writes + as \u002B
    private static readonly JsonSerializerOptions CommandOptions = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // The device ignores a picture whose id isn't higher than that of the last one. It is known to stop taking pictures
    // after a few hundred, so like other libraries for it the id starts again (Draw/ResetHttpGifId) after a few.
    internal const int MaximumPictureId = 32;

    /// <summary>
    /// The device is in the local network: not through the proxy (which can't reach it), no redirects
    /// </summary>
    private static readonly Lazy<HttpService> DeviceHttp = new Lazy<HttpService>(() => new HttpService(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseDefaultCredentials = false,
        UseProxy = false
    }, DeviceTimeout));

    /// <summary>
    /// The devices in this network, asks the server of Divoom
    /// </summary>
    /// <exception cref="HttpRequestException">no connection or an error of the server</exception>
    public static async Task<IReadOnlyList<DivoomDevice>> FindDevicesAsync(CancellationToken cancellationToken)
    {
        var result = await HttpService.Current.SendAsync(new HttpRequestMessage(HttpMethod.Get, DiscoveryUrl), cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess();
        return ParseDevices(result.Content);
    }

    /// <summary>
    /// The devices of the answer of the server: {"ReturnCode":0,"DeviceList":[{"DeviceName":"..","DevicePrivateIP":".."}]}
    /// </summary>
    internal static IReadOnlyList<DivoomDevice> ParseDevices(string json)
    {
        var devices = new List<DivoomDevice>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return devices;
        }
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("DeviceList", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return devices;
        }
        foreach (var device in list.EnumerateArray())
        {
            if (device.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            string address = GetString(device, "DevicePrivateIP");
            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }
            devices.Add(new DivoomDevice(GetString(device, "DeviceName"), address.Trim()));
        }
        return devices;
    }

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>
    /// The URL of the commands of the device, null when the address isn't one of a device (a host, an IP address, with a port)
    /// </summary>
    public static Uri GetCommandUri(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }
        address = address.Trim();
        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            address = "http://" + address;
        }
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp || string.IsNullOrEmpty(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return null;
        }
        return new UriBuilder(uri) { Path = "/post" }.Uri;
    }

    /// <summary>
    /// Show the picture on the device: the RGB bytes of size x size pixels, row by row from the top left
    /// </summary>
    /// <param name="commandUri">From <see cref="GetCommandUri"/></param>
    /// <param name="size">16, 32 or 64</param>
    /// <param name="rgb">size * size * 3 bytes</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <exception cref="DivoomException">the device didn't take a command</exception>
    /// <exception cref="HttpRequestException">no connection</exception>
    /// <exception cref="TimeoutException">the device didn't answer</exception>
    public static async Task SendPictureAsync(Uri commandUri, int size, byte[] rgb, CancellationToken cancellationToken)
    {
        if (commandUri == null) throw new ArgumentNullException(nameof(commandUri));
        if (rgb == null) throw new ArgumentNullException(nameof(rgb));
        if (rgb.Length != size * size * 3) throw new ArgumentException($"A picture of {size} x {size} pixels has {size * size * 3} bytes, not {rgb.Length}.", nameof(rgb));

        using var lastId = await SendCommandAsync(commandUri, new Dictionary<string, object> { ["Command"] = "Draw/GetHttpGifId" }, cancellationToken).ConfigureAwait(false);
        int pictureId = lastId.RootElement.TryGetProperty("PicId", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out int value) ? value + 1 : 1;
        if (pictureId > MaximumPictureId)
        {
            (await SendCommandAsync(commandUri, new Dictionary<string, object> { ["Command"] = "Draw/ResetHttpGifId" }, cancellationToken).ConfigureAwait(false)).Dispose();
            pictureId = 1;
        }
        (await SendCommandAsync(commandUri, CreatePictureCommand(pictureId, size, rgb), cancellationToken).ConfigureAwait(false)).Dispose();
    }

    /// <summary>
    /// Draw/SendHttpGif with one frame: an animation of one picture
    /// </summary>
    internal static Dictionary<string, object> CreatePictureCommand(int pictureId, int size, byte[] rgb) => new Dictionary<string, object>
    {
        ["Command"] = "Draw/SendHttpGif",
        ["PicNum"] = 1,
        ["PicWidth"] = size,
        ["PicOffset"] = 0,
        ["PicID"] = pictureId,
        ["PicSpeed"] = 1000,
        ["PicData"] = Convert.ToBase64String(rgb)
    };

    /// <summary>
    /// The command as the device reads it
    /// </summary>
    internal static string ToJson(Dictionary<string, object> command) => JsonSerializer.Serialize(command, CommandOptions);

    /// <summary>
    /// Post the command, the answer of the device when its error_code is 0
    /// </summary>
    private static async Task<JsonDocument> SendCommandAsync(Uri commandUri, Dictionary<string, object> command, CancellationToken cancellationToken)
    {
        // With a length, the device refuses a chunked body ("Request data illegal json")
        var request = new HttpRequestMessage(HttpMethod.Post, commandUri)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(ToJson(command)))
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var result = await DeviceHttp.Value.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccessStatusCode)
        {
            throw new DivoomException(string.Format(CultureInfo.InvariantCulture, "HTTP {0} {1}", (int)result.StatusCode, result.ReasonPhrase));
        }
        return ParseAnswer(result.Content);
    }

    /// <summary>
    /// The answer of the device, it has error_code 0 when the command was taken (a number, some firmware sends a string)
    /// </summary>
    /// <exception cref="DivoomException">another error_code, or not an answer of a device</exception>
    internal static JsonDocument ParseAnswer(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
        }
        catch (JsonException)
        {
            throw new DivoomException("The answer isn't one of a Divoom device.");
        }
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("error_code", out var errorCode))
        {
            document.Dispose();
            throw new DivoomException("The answer isn't one of a Divoom device.");
        }
        string code = errorCode.ValueKind == JsonValueKind.String ? errorCode.GetString() : errorCode.GetRawText();
        if (code != "0")
        {
            document.Dispose();
            throw new DivoomException("error_code " + code);
        }
        return document;
    }

    /// <summary>
    /// The message of the exception for the user: of a failed connection the reason (e.g. the host isn't reachable)
    /// </summary>
    public static string Describe(Exception exception) =>
        exception is HttpRequestException { InnerException: WebException webException } ? webException.Message : exception?.Message;
}
