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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Linux.Core.Ipc;

/// <summary>
/// The sources a connection announces in its HELLO frame, like on Windows. The source is bound to the connection:
/// later envelopes can't claim another one.
/// </summary>
public static class IpcSources
{
    public const string HelloCommand = "HELLO";

    /// <summary>
    /// An unparsed command line, Greenshot parses it according to the source
    /// </summary>
    public const string CliCommand = "CLI";

    public const string ReplyFormatJson = "json";
    public const string ReplyFormatText = "text";

    /// <summary>
    /// The terminal: "greenshot" itself, as client of the running Greenshot
    /// </summary>
    public const string Cli = "cli";

    /// <summary>
    /// A file manager or a greenshot: link (the desktop file), only files and links
    /// </summary>
    public const string OpenWith = "open_with";

    public const string UrlScheme = "url_scheme";

    /// <summary>
    /// greenshot-mcp, the MCP server for AI tools
    /// </summary>
    public const string Mcp = "mcp";

    public static bool IsKnown(string? source) =>
        source is not null && (source.Equals(Cli, StringComparison.OrdinalIgnoreCase) || source.Equals(OpenWith, StringComparison.OrdinalIgnoreCase)
                               || source.Equals(UrlScheme, StringComparison.OrdinalIgnoreCase) || source.Equals(Mcp, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// A request to the running Greenshot, the IpcEnvelope of Windows (the same JSON names)
/// </summary>
public sealed class IpcEnvelope
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("raw_input")]
    public string? RawInput { get; set; }

    [JsonPropertyName("recipe")]
    public string? Recipe { get; set; }

    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = new();

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("async")]
    public bool Async { get; set; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    /// <summary>
    /// HELLO only: the name of the program which uses greenshot-mcp, as it says (Greenshot checks it itself)
    /// </summary>
    [JsonPropertyName("origin")]
    public string? Origin { get; set; }

    /// <summary>
    /// HELLO only: "json" (default) or "text" (terminal)
    /// </summary>
    [JsonPropertyName("reply_format")]
    public string? ReplyFormat { get; set; }

    /// <summary>
    /// CLI only: the command line without the program name
    /// </summary>
    [JsonPropertyName("argv")]
    public List<string>? Argv { get; set; }

    /// <summary>
    /// RUN_RECIPE only: an expression evaluated after the recipe, its value is the output (--query)
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// The result as one JSON document instead of streamed text (--json)
    /// </summary>
    [JsonPropertyName("json")]
    public bool Json { get; set; }

    /// <summary>
    /// Arguments of other commands (MCP tools)
    /// </summary>
    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; set; }

    [JsonIgnore]
    public bool IsHello => string.Equals(Command, IpcSources.HelloCommand, StringComparison.OrdinalIgnoreCase);

    public static IpcEnvelope CreateHello(string source, string replyFormat, string? origin = null) =>
        new() { Command = IpcSources.HelloCommand, Source = source, ReplyFormat = replyFormat, Origin = origin };

    public static IpcEnvelope CreateCli(IEnumerable<string> argv, string source, string? cwd) =>
        new() { Command = IpcSources.CliCommand, Source = source, Cwd = cwd, Argv = argv.ToList() };

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);

    public static IpcEnvelope? FromBytes(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return JsonSerializer.Deserialize<IpcEnvelope>(bytes, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Frames on the socket: a 4-byte little-endian length and the payload, like the named pipe of Windows
/// </summary>
public static class IpcFraming
{
    /// <summary>
    /// The largest frame accepted, so a misbehaving client can't make Greenshot use memory without end
    /// </summary>
    public const int MaxFrameSize = 32 * 1024 * 1024;

    public static async Task WriteFrameAsync(Stream stream, byte[] payload, CancellationToken cancellationToken = default)
    {
        var frame = new byte[4 + payload.Length];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)payload.Length);
        payload.CopyTo(frame, 4);
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The next frame, null when the other side closed the connection
    /// </summary>
    public static async Task<byte[]?> ReadFrameAsync(Stream stream, int maxSize = MaxFrameSize, CancellationToken cancellationToken = default)
    {
        var header = new byte[4];
        if (!await ReadExactlyOrEndAsync(stream, header, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        uint length = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (length > maxSize)
        {
            throw new IOException($"A frame of {length} bytes is larger than allowed ({maxSize}).");
        }

        var payload = new byte[length];
        if (!await ReadExactlyOrEndAsync(stream, payload, cancellationToken).ConfigureAwait(false))
        {
            throw new IOException("The connection was closed in the middle of a frame.");
        }

        return payload;
    }

    private static async Task<bool> ReadExactlyOrEndAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int count = await stream.ReadAsync(buffer.AsMemory(read), cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                if (read == 0)
                {
                    return false;
                }

                throw new IOException("The connection was closed in the middle of a frame.");
            }

            read += count;
        }

        return true;
    }
}
