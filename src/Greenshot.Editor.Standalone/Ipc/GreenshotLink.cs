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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Document;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Files;

namespace Greenshot.Editor.Standalone.Ipc
{
    /// <summary>
    /// The connection of the editor to the running Greenshot, over Greenshot's named pipe. The editor is a client with the
    /// source "editor": it asks for the destinations (EDITOR_DESTINATIONS) and asks Greenshot to send a document to one of
    /// them (EDITOR_EXPORT). The document itself doesn't go through the pipe: it is a file in the handover folder, and the
    /// request names it. One more connection only watches (WATCH), so the editor knows whether Greenshot is there.
    /// The messages are those of Greenshot's pipe: a length of 4 bytes (little endian) and then JSON in UTF-8.
    /// </summary>
    public sealed class GreenshotLink : IGreenshotLink, IDisposable
    {
        public const string Source = "editor";
        public const string DestinationsCommand = "EDITOR_DESTINATIONS";
        public const string ExportCommand = "EDITOR_EXPORT";
        private const int MaxFrameSize = 4 * 1024 * 1024;

        private readonly Func<CancellationToken, Task<Stream>> _connect;
        private readonly Action<Action> _post;
        private readonly CancellationTokenSource _disposed = new();
        private readonly object _lock = new();
        private IReadOnlyList<EditorDestination> _destinations = Array.Empty<EditorDestination>();
        private bool _watching;

        /// <param name="connect">Opens a connection to Greenshot's pipe</param>
        /// <param name="post">Runs an action on the UI thread</param>
        public GreenshotLink(Func<CancellationToken, Task<Stream>> connect, Action<Action> post)
        {
            _connect = connect ?? throw new ArgumentNullException(nameof(connect));
            _post = post ?? throw new ArgumentNullException(nameof(post));
        }

        /// <summary>
        /// The link to the Greenshot the editor was installed with, null when there is none (the editor runs alone)
        /// </summary>
        public static GreenshotLink Create()
        {
            if (!OperatingSystem.IsWindows() || EditorPaths.GreenshotExecutable is not string greenshot)
            {
                EditorLog.Info("The editor runs without Greenshot: it isn't in the folder Editor of a Greenshot");
                return null;
            }

            return new GreenshotLink(cancellationToken => OperatingSystem.IsWindows() ? GreenshotPipe.ConnectAsync(greenshot, cancellationToken) : throw new PlatformNotSupportedException(), action => Avalonia.Threading.Dispatcher.UIThread.Post(action));
        }

        public bool IsConnected { get; private set; }

        public IReadOnlyList<EditorDestination> Destinations => _destinations;

        /// <summary>
        /// The language Greenshot uses, null until it told
        /// </summary>
        public string Language { get; private set; }

        public event Action Changed;

        /// <summary>
        /// Looks for Greenshot, without waiting for it
        /// </summary>
        public void Start() => RefreshAsync(_disposed.Token).Forget("Looking for Greenshot");

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            bool watch;
            lock (_lock)
            {
                watch = !_watching;
                _watching = true;
            }

            if (watch)
            {
                WatchAsync(_disposed.Token).Forget("Watching Greenshot");
            }

            try
            {
                await LoadDestinationsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                EditorLog.Debug($"Greenshot doesn't answer: {ex.Message}");
                Set(false, Array.Empty<EditorDestination>());
            }
        }

        public async Task ExportAsync(EditorDestination destination, GreenshotDocument document, string title, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(document);
            Directory.CreateDirectory(EditorPaths.HandoverDirectory);
            string file = Path.Combine(EditorPaths.HandoverDirectory, Guid.NewGuid().ToString("N") + DocumentFiles.DocumentExtension);
            if (!string.IsNullOrWhiteSpace(title))
            {
                document = document with { Metadata = (document.Metadata ?? new DocumentMetadata()) with { Title = title } };
            }

            try
            {
                DocumentFiles.Save(document, file);
                var reply = await RequestAsync(new JsonObject
                {
                    ["version"] = 1,
                    ["command"] = ExportCommand,
                    ["files"] = new JsonArray(file),
                    ["parameters"] = new JsonObject { ["destination"] = destination.Designation, ["title"] = title ?? string.Empty }
                }, cancellationToken).ConfigureAwait(false);
                ThrowOnError(reply);
            }
            finally
            {
                try
                {
                    // Greenshot has read it by now (or never will)
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    EditorLog.Warn($"The handed over file {file} could not be removed", ex);
                }
            }
        }

        private async Task LoadDestinationsAsync(CancellationToken cancellationToken)
        {
            var reply = await RequestAsync(new JsonObject { ["version"] = 1, ["command"] = DestinationsCommand }, cancellationToken).ConfigureAwait(false);
            ThrowOnError(reply);
            var destinations = (reply["destinations"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Select(entry => new EditorDestination(Text(entry, "designation"), Text(entry, "name")))
                .Where(entry => !string.IsNullOrEmpty(entry.Designation) && !string.IsNullOrEmpty(entry.Name)).ToList();
            Language = Text(reply, "language");
            Set(true, destinations);
        }

        /// <summary>
        /// Stays connected to Greenshot for as long as it runs: when the connection ends, Greenshot is gone
        /// </summary>
        private async Task WatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = await _connect(cancellationToken).ConfigureAwait(false);
                await WriteFrameAsync(stream, Hello(), cancellationToken).ConfigureAwait(false);
                await WriteFrameAsync(stream, new JsonObject { ["version"] = 1, ["command"] = "WATCH" }, cancellationToken).ConfigureAwait(false);
                while (await ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false) is JsonObject message)
                {
                    if (Text(message, "status") == "error" || Text(message, "event") == "shutdown")
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                EditorLog.Debug($"Greenshot isn't there: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _watching = false;
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    Set(false, Array.Empty<EditorDestination>());
                }
            }
        }

        /// <summary>
        /// One request on its own connection: HELLO, the request, and the reply to it
        /// </summary>
        private async Task<JsonObject> RequestAsync(JsonObject request, CancellationToken cancellationToken)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposed.Token);
            await using var stream = await _connect(linked.Token).ConfigureAwait(false);
            await WriteFrameAsync(stream, Hello(), linked.Token).ConfigureAwait(false);
            await WriteFrameAsync(stream, request, linked.Token).ConfigureAwait(false);
            while (await ReadFrameAsync(stream, linked.Token).ConfigureAwait(false) is JsonObject reply)
            {
                if (reply["event"] == null)
                {
                    return reply;
                }

                if (Text(reply, "event") == "shutdown")
                {
                    break;
                }
            }

            throw new IOException("Greenshot closed the connection without an answer.");
        }

        private static JsonObject Hello() => new() { ["version"] = 1, ["command"] = "HELLO", ["source"] = Source };

        private static void ThrowOnError(JsonObject reply)
        {
            if (Text(reply, "status") != "ok")
            {
                throw new InvalidOperationException(Text(reply, "stderr") ?? "Greenshot reported an error.");
            }
        }

        private static string Text(JsonObject message, string name) => message[name] is JsonValue value && value.TryGetValue(out string text) ? text : null;

        private void Set(bool connected, IReadOnlyList<EditorDestination> destinations)
        {
            _post(() =>
            {
                bool changed = connected != IsConnected || !destinations.SequenceEqual(_destinations);
                IsConnected = connected;
                _destinations = destinations;
                if (changed)
                {
                    Changed?.Invoke();
                }
            });
        }

        internal static async Task WriteFrameAsync(Stream stream, JsonObject message, CancellationToken cancellationToken)
        {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message);
            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)payload.Length);
            payload.CopyTo(frame, 4);
            await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// The next message, null when the other side closed the connection
        /// </summary>
        internal static async Task<JsonObject> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] length = new byte[4];
            if (await ReadExactAsync(stream, length, cancellationToken).ConfigureAwait(false) < 4)
            {
                return null;
            }

            uint size = BinaryPrimitives.ReadUInt32LittleEndian(length);
            if (size < 2 || size > MaxFrameSize)
            {
                throw new IOException($"A message of {size} bytes isn't one of Greenshot.");
            }

            byte[] payload = new byte[size];
            if (await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false) < size)
            {
                return null;
            }

            return JsonNode.Parse(payload) as JsonObject ?? throw new JsonException("The message isn't a JSON object.");
        }

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            return total;
        }

        public void Dispose()
        {
            _disposed.Cancel();
            _disposed.Dispose();
        }
    }
}
