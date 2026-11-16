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
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Editor.Standalone.Files;
using Greenshot.Editor.Standalone.Ipc;
using Greenshot.Editor.Standalone.Settings;
using Xunit;

namespace Greenshot.Editor.Standalone.Tests.Ipc
{
    /// <summary>
    /// The editor's side of the pipe, against a pipe server in the test which speaks like Greenshot's
    /// </summary>
    public class GreenshotLinkTests
    {
        /// <summary>
        /// A stand-in for Greenshot's pipe server: every connection starts with HELLO, then one request which the test answers
        /// </summary>
        private sealed class FakeGreenshot : IAsyncDisposable
        {
            private readonly string _name = "GreenshotEditorTest_" + Guid.NewGuid().ToString("N");
            private readonly CancellationTokenSource _stop = new();
            private readonly Task _serving;
            // On Unix the pipe is gone when no server instance exists for a moment, and a client which is connecting then waits
            // for ever: this instance is there for as long as the stand-in
            private readonly NamedPipeServerStream _keeper;
            private readonly Func<JsonObject, JsonObject> _answer;

            public List<JsonObject> Hellos { get; } = new();
            public TaskCompletionSource Watching { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource EndWatch { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public FakeGreenshot(Func<JsonObject, JsonObject> answer)
            {
                _answer = answer;
                _keeper = new NamedPipeServerStream(_name, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                _serving = ServeAsync();
            }

            public async Task<Stream> ConnectAsync(CancellationToken cancellationToken)
            {
                var client = new NamedPipeClientStream(".", _name, PipeDirection.InOut, PipeOptions.Asynchronous);
                await client.ConnectAsync(5000, cancellationToken);
                return client;
            }

            private async Task ServeAsync()
            {
                var connections = new List<Task>();
                try
                {
                    while (!_stop.IsCancellationRequested)
                    {
                        var pipe = new NamedPipeServerStream(_name, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                        try
                        {
                            await pipe.WaitForConnectionAsync(_stop.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            await pipe.DisposeAsync();
                            break;
                        }

                        connections.Add(HandleAsync(pipe));
                    }
                }
                finally
                {
                    await Task.WhenAll(connections);
                }
            }

            private async Task HandleAsync(NamedPipeServerStream pipe)
            {
                await using (pipe)
                {
                    try
                    {
                        var hello = await GreenshotLink.ReadFrameAsync(pipe, _stop.Token);
                        lock (Hellos)
                        {
                            Hellos.Add(hello);
                        }

                        var request = await GreenshotLink.ReadFrameAsync(pipe, _stop.Token);
                        if ((string)request["command"] == "WATCH")
                        {
                            await GreenshotLink.WriteFrameAsync(pipe, new JsonObject { ["status"] = "ok" }, _stop.Token);
                            Watching.TrySetResult();
                            await EndWatch.Task.WaitAsync(_stop.Token);
                            await GreenshotLink.WriteFrameAsync(pipe, new JsonObject { ["event"] = "shutdown" }, _stop.Token);
                            return;
                        }

                        await GreenshotLink.WriteFrameAsync(pipe, _answer(request), _stop.Token);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException or IOException)
                    {
                        // The test ended
                    }
                }
            }

            public async ValueTask DisposeAsync()
            {
                await _stop.CancelAsync();
                await _serving;
                await _keeper.DisposeAsync();
                _stop.Dispose();
            }
        }

        private static JsonObject Destinations() => new()
        {
            ["status"] = "ok",
            ["language"] = "de-DE",
            ["destinations"] = new JsonArray(new JsonObject { ["designation"] = "FileDialog", ["name"] = "Save as" }, new JsonObject { ["designation"] = "Clipboard", ["name"] = "Clipboard" },
                new JsonObject { ["designation"] = "", ["name"] = "Without designation" })
        };

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        [Fact]
        public async Task Refresh_ReadsTheDestinations_AsTheSourceEditor()
        {
            var requests = new List<JsonObject>();
            await using var greenshot = new FakeGreenshot(request =>
            {
                requests.Add(request);
                return Destinations();
            });
            using var link = new GreenshotLink(greenshot.ConnectAsync, action => action());
            int changes = 0;
            link.Changed += () => changes++;

            await link.RefreshAsync(Token);

            Assert.True(link.IsConnected);
            Assert.Equal(new[] { new EditorDestination("FileDialog", "Save as"), new EditorDestination("Clipboard", "Clipboard") }, link.Destinations);
            Assert.Equal("de-DE", link.Language);
            Assert.Equal(1, changes);
            Assert.Equal(GreenshotLink.DestinationsCommand, (string)Assert.Single(requests)["command"]);
            await greenshot.Watching.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            lock (greenshot.Hellos)
            {
                Assert.All(greenshot.Hellos, hello =>
                {
                    Assert.Equal("HELLO", (string)hello["command"]);
                    Assert.Equal("editor", (string)hello["source"]);
                });
            }
        }

        [Fact]
        public async Task WhenGreenshotExits_TheLinkIsNoLongerConnected()
        {
            await using var greenshot = new FakeGreenshot(_ => Destinations());
            using var link = new GreenshotLink(greenshot.ConnectAsync, action => action());
            await link.RefreshAsync(Token);
            await greenshot.Watching.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            var gone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            link.Changed += () =>
            {
                if (!link.IsConnected)
                {
                    gone.TrySetResult();
                }
            };

            greenshot.EndWatch.SetResult();

            await gone.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            Assert.Empty(link.Destinations);
        }

        [Fact]
        public async Task WithoutGreenshot_TheLinkIsNotConnected()
        {
            using var link = new GreenshotLink(_ => throw new TimeoutException("Nobody there"), action => action());

            await link.RefreshAsync(Token);

            Assert.False(link.IsConnected);
            Assert.Empty(link.Destinations);
        }

        [Fact]
        public async Task Export_HandsTheDocumentOverAsAFile_AndRemovesIt()
        {
            string file = null, title = null, destination = null;
            Greenshot.Document.GreenshotDocument received = null;
            await using var greenshot = new FakeGreenshot(request =>
            {
                file = (string)request["files"][0];
                destination = (string)request["parameters"]["destination"];
                title = (string)request["parameters"]["title"];
                received = DocumentFiles.Open(file);
                return new JsonObject { ["status"] = "ok" };
            });
            using var link = new GreenshotLink(greenshot.ConnectAsync, action => action());
            var document = Samples.Page(Samples.Green);

            await link.ExportAsync(new EditorDestination("Clipboard", "Clipboard"), document, "My capture", Token);

            Assert.True(EditorPaths.IsHandoverFile(file), file);
            Assert.False(File.Exists(file), "The file is removed once Greenshot answered");
            Assert.Equal("Clipboard", destination);
            Assert.Equal("My capture", title);
            Assert.Equal("My capture", received.Metadata.Title);
            Assert.Equal(document.Layers.Select(layer => layer.Elements.Count), received.Layers.Select(layer => layer.Elements.Count));
        }

        [Fact]
        public async Task Export_WhichGreenshotRefuses_TellsWhy()
        {
            await using var greenshot = new FakeGreenshot(_ => new JsonObject { ["status"] = "error", ["stderr"] = "No such destination." });
            using var link = new GreenshotLink(greenshot.ConnectAsync, action => action());

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => link.ExportAsync(new EditorDestination("Nope", "Nope"), Samples.Page(Samples.Green), null, Token));

            Assert.Equal("No such destination.", error.Message);
        }

        [Fact]
        public async Task AMessage_WhichIsTooLarge_IsNotRead()
        {
            using var stream = new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 1, 2 });
            await Assert.ThrowsAsync<IOException>(() => GreenshotLink.ReadFrameAsync(stream, Token));
        }

        [Fact]
        public async Task AClosedConnection_ReadsAsNoMessage()
        {
            using var stream = new MemoryStream(new byte[] { 10, 0 });
            Assert.Null(await GreenshotLink.ReadFrameAsync(stream, Token));
        }
    }
}
