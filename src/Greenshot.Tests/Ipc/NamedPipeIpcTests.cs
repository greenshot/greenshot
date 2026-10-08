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

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Ipc;
using Greenshot.Ipc.BrowserExtension;
using Greenshot.Ipc.Cli;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    public class NamedPipeIpcTests
    {
        /// <summary>
        /// Greenshot.exe sends its command arguments as the same CLI request that greenshot-cli.exe and greenshot-proxy.exe send.
        /// </summary>
        [Fact]
        public void EnvelopeSerialization_Cli_MatchesTheProxyRequest()
        {
            var envelope = IpcEnvelope.CreateCli(new[] { "--file", @"C:\Users\Test\Pictures\screenshot.png" }, IpcSources.Cli, @"C:\Users\Test");

            JObject jobj = JObject.Parse(JsonConvert.SerializeObject(envelope));
            Assert.Equal(1, jobj.Value<int>("version"));
            Assert.Equal("CLI", jobj.Value<string>("command"));
            Assert.Equal("cli", jobj.Value<string>("source"));
            Assert.Equal(@"C:\Users\Test", jobj.Value<string>("cwd"));
            Assert.Equal(new[] { "--file", @"C:\Users\Test\Pictures\screenshot.png" }, jobj["argv"].Values<string>());

            var parsed = CliCommandParser.Parse(envelope.Argv, envelope.Source, envelope.Cwd);
            Assert.True(parsed.Success);
            Assert.Equal("OPEN_FILE", parsed.Envelope.Command);
            Assert.Equal(new[] { @"C:\Users\Test\Pictures\screenshot.png" }, parsed.Envelope.Files);
        }

        [Fact]
        public void NamedPipeEndpoint_GeneratesPipeNameWithUserSid()
        {
            string userSid = NamedPipeEndpoint.GetUserSid();
            Assert.NotNull(userSid);
            Assert.StartsWith("S-", userSid);

            string expectedSid = WindowsIdentity.GetCurrent().User?.Value;
            Assert.Equal(expectedSid, userSid);

            string pipeName = NamedPipeEndpoint.GetPipeName();
            Assert.Equal("Greenshot_" + userSid, pipeName);

            var security = NamedPipeEndpoint.CreateServerSecurity();
            Assert.NotNull(security);
            Assert.True(security.AreAccessRulesProtected);
        }

        [Fact]
        public async Task NamedPipe_EndToEndTransmission_Succeeds()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<IpcEnvelope>();

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.MessageReceived += (s, e) =>
                {
                    tcs.TrySetResult(e);
                };
                server.Start();

                string filePath = @"C:\TestPath\capture.png";
                var envelope = IpcEnvelope.CreateCli(new[] { filePath }, IpcSources.OpenWith, null);

                bool sent = NamedPipeClient.SendMessage(testPipeName, envelope, timeoutMs: 3000);
                Assert.True(sent, "Client should successfully connect and send message to server.");

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(4000));
                Assert.Same(tcs.Task, completedTask);

                var receivedEnvelope = await tcs.Task;
                Assert.NotNull(receivedEnvelope);
                Assert.Equal(1, receivedEnvelope.Version);
                Assert.Equal("open_with", receivedEnvelope.Source);
                Assert.Equal("CLI", receivedEnvelope.Command);
                Assert.Equal(new[] { filePath }, receivedEnvelope.Argv);
            }
        }

        private static void WriteFrame(Stream stream, string json)
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] length = BitConverter.GetBytes((uint)payload.Length);
            stream.Write(length, 0, 4);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private static JObject ReadFrame(Stream stream)
        {
            byte[] length = new byte[4];
            int read = 0;
            while (read < 4)
            {
                int n = stream.Read(length, read, 4 - read);
                if (n == 0) return null;
                read += n;
            }
            byte[] payload = new byte[BitConverter.ToUInt32(length, 0)];
            read = 0;
            while (read < payload.Length)
            {
                int n = stream.Read(payload, read, payload.Length - read);
                if (n == 0) return null;
                read += n;
            }
            return JObject.Parse(Encoding.UTF8.GetString(payload));
        }

        [Fact]
        public async Task NamedPipeServer_WatchConnection_GetsEventsAndTheShutdown()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            bool requestReceived = false;

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.RequestReceived += ctx =>
                {
                    requestReceived = true;
                    return Task.CompletedTask;
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"cli\"}");
                    WriteFrame(client, "{\"version\":1,\"command\":\"WATCH\"}");
                    var reply = await Task.Run(() => ReadFrame(client));
                    Assert.True(reply.Value<bool>("watching"));

                    // The notifications complete before the client reads them: a client that isn't reading must not hold up Greenshot (e.g. its exit)
                    var notify = server.NotifyWatchersAsync(new { @event = "tools_changed" });
                    Assert.Same(notify, await Task.WhenAny(notify, Task.Delay(TimeSpan.FromSeconds(10))));
                    var changed = await Task.Run(() => ReadFrame(client));
                    Assert.NotNull(changed);
                    Assert.Equal("tools_changed", changed.Value<string>("event"));

                    var notifyShutdown = server.NotifyShutdownAsync(NamedPipeServer.ShutdownReasonUpdate);
                    Assert.Same(notifyShutdown, await Task.WhenAny(notifyShutdown, Task.Delay(TimeSpan.FromSeconds(10))));
                    var shutdown = await Task.Run(() => ReadFrame(client));
                    Assert.NotNull(shutdown);
                    Assert.Equal("shutdown", shutdown.Value<string>("event"));
                    Assert.Equal("update", shutdown.Value<string>("reason"));
                    // The browser extension's existing "offline" check
                    Assert.False(shutdown.Value<bool>("greenshot_running"));
                }
            }

            // WATCH is handled by the server itself
            Assert.False(requestReceived);
        }

        [Fact]
        public async Task NamedPipeServer_ConnectionWithoutHello_IsRejected()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            bool requestReceived = false;

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.RequestReceived += ctx =>
                {
                    requestReceived = true;
                    return Task.CompletedTask;
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"source\":\"cli\",\"command\":\"EXIT\"}");

                    var response = await Task.Run(() => ReadFrame(client));
                    Assert.NotNull(response);
                    Assert.Equal("error", response.Value<string>("status"));
                    Assert.Contains("HELLO", response.Value<string>("stderr"));
                }
            }

            Assert.False(requestReceived);
        }

        [Fact]
        public async Task NamedPipeServer_SourceIsBoundFromHello_NotFromLaterEnvelopes()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<IpcRequestContext>();

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.RequestReceived += ctx =>
                {
                    tcs.TrySetResult(ctx);
                    return Task.CompletedTask;
                };
                server.ExtensionOriginValidator = origin => origin == "chrome-extension://abc/";
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"native_messaging\",\"origin\":\"chrome-extension://abc/\"}");
                    // A relayed message claiming to come from the command line
                    WriteFrame(client, "{\"version\":1,\"source\":\"cli\",\"command\":\"TAB_CHANGED\",\"url\":\"https://example.com\"}");

                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(4000));
                    Assert.Same(tcs.Task, completed);

                    var context = await tcs.Task;
                    Assert.Equal("native_messaging", context.Envelope.Source);
                    Assert.Equal("chrome-extension://abc/", context.ConnectionOrigin);
                    Assert.Equal("TAB_CHANGED", context.Envelope.Command);
                }
            }
        }

        [Fact]
        public async Task NamedPipeServer_RepeatedHello_ClosesConnection()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            bool requestReceived = false;

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.RequestReceived += ctx =>
                {
                    requestReceived = true;
                    return Task.CompletedTask;
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"cli\"}");
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"native_messaging\"}");

                    var response = await Task.Run(() => ReadFrame(client));
                    Assert.NotNull(response);
                    Assert.Equal("error", response.Value<string>("status"));
                    Assert.Contains("HELLO", response.Value<string>("stderr"));

                    // The server closed the connection
                    Assert.Null(await Task.Run(() => ReadFrame(client)));
                }
            }

            Assert.False(requestReceived);
        }

        [Fact]
        public async Task NamedPipeServer_TextConnection_ReplyAlwaysEndsWithExitFrame()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");

            using (var server = new NamedPipeServer(testPipeName))
            {
                // A handler that does not reply at all
                server.RequestReceived += ctx => Task.CompletedTask;
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"cli\",\"reply_format\":\"text\"}");
                    WriteFrame(client, "{\"version\":1,\"command\":\"TAB_CHANGED\"}");

                    byte[] frame = await Task.Run(() =>
                    {
                        byte[] length = new byte[4];
                        int read = 0;
                        while (read < 4)
                        {
                            int n = client.Read(length, read, 4 - read);
                            if (n == 0) return null;
                            read += n;
                        }
                        byte[] payload = new byte[BitConverter.ToUInt32(length, 0)];
                        read = 0;
                        while (read < payload.Length)
                        {
                            int n = client.Read(payload, read, payload.Length - read);
                            if (n == 0) return null;
                            read += n;
                        }
                        return payload;
                    });

                    Assert.NotNull(frame);
                    Assert.Equal(5, frame.Length);
                    Assert.Equal((byte)'X', frame[0]);
                    Assert.Equal(0, BitConverter.ToInt32(frame, 1));
                }
            }
        }

        [Fact]
        public async Task NamedPipeServer_UnknownReplyFormat_IsRejected()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.Start();
                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"cli\",\"reply_format\":\"xml\"}");
                    var response = await Task.Run(() => ReadFrame(client));
                    Assert.NotNull(response);
                    Assert.Equal("error", response.Value<string>("status"));
                }
            }
        }

        [Theory]
        [InlineData("chrome-extension://someotherextension/")]
        [InlineData("evil@example.com")]
        [InlineData(null)]
        public async Task NamedPipeServer_NativeMessaging_UnknownOrMissingOrigin_IsRejected(string origin)
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            bool requestReceived = false;

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.ExtensionOriginValidator = new ExtensionOriginPolicy(null).IsAllowed;
                server.RequestReceived += ctx =>
                {
                    requestReceived = true;
                    return Task.CompletedTask;
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    var hello = new JObject { ["version"] = 1, ["command"] = "HELLO", ["source"] = "native_messaging" };
                    if (origin != null)
                    {
                        hello["origin"] = origin;
                    }
                    // Only the HELLO: the server stops reading after rejecting it, and a flush of unread data would block
                    WriteFrame(client, hello.ToString(Formatting.None));

                    var readTask = Task.Run(() => ReadFrame(client));
                    Assert.Same(readTask, await Task.WhenAny(readTask, Task.Delay(4000)));
                    var response = await readTask;
                    Assert.NotNull(response);
                    Assert.Equal("error", response.Value<string>("status"));
                    Assert.Contains("extension", response.Value<string>("stderr"));

                    var closeTask = Task.Run(() => ReadFrame(client));
                    Assert.Same(closeTask, await Task.WhenAny(closeTask, Task.Delay(4000)));
                    Assert.Null(await closeTask);
                }
            }

            Assert.False(requestReceived);
        }

        [Fact]
        public async Task NamedPipeServer_NativeMessaging_OfficialOrigin_IsAcceptedByDefault()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<IpcRequestContext>();

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.RequestReceived += ctx =>
                {
                    tcs.TrySetResult(ctx);
                    return Task.CompletedTask;
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"native_messaging\",\"origin\":\"chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik/\"}");
                    WriteFrame(client, "{\"version\":1,\"command\":\"TAB_CHANGED\",\"url\":\"https://example.com\"}");

                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(4000));
                    Assert.Same(tcs.Task, completed);
                    Assert.Equal("native_messaging", (await tcs.Task).Envelope.Source);
                }
            }
        }

        [Fact]
        public async Task NamedPipeServer_OriginIsNotCheckedForOtherSources()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<IpcRequestContext>();

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.ExtensionOriginValidator = origin => false;
                server.RequestReceived += ctx =>
                {
                    tcs.TrySetResult(ctx);
                    return Task.CompletedTask;
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"cli\"}");
                    WriteFrame(client, "{\"version\":1,\"command\":\"VERSION\"}");

                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(4000));
                    Assert.Same(tcs.Task, completed);
                }
            }
        }

        [Fact]
        public async Task NamedPipeServer_ClientThatDoesNotRead_IsDisconnectedAfterTimeout()
        {
            string testPipeName = NamedPipeEndpoint.GetPipeName() + "_test_" + Guid.NewGuid().ToString("N");
            var replyOutcome = new TaskCompletionSource<Exception>();

            using (var server = new NamedPipeServer(testPipeName))
            {
                server.ReplyWriteTimeout = TimeSpan.FromSeconds(1);
                server.RequestReceived += async ctx =>
                {
                    try
                    {
                        // Far more than a pipe buffer holds, so the write needs the client to read
                        await ctx.ReplyAsync(new { status = "ok", exit_code = 0, stdout = new string('x', 4 * 1024 * 1024) });
                        replyOutcome.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        replyOutcome.TrySetResult(ex);
                        throw;
                    }
                };
                server.Start();

                using (var client = new NamedPipeClientStream(".", testPipeName, PipeDirection.InOut))
                {
                    client.Connect(3000);
                    WriteFrame(client, "{\"version\":1,\"command\":\"HELLO\",\"source\":\"cli\"}");
                    WriteFrame(client, "{\"version\":1,\"command\":\"VERSION\"}");

                    // The client never reads: the reply must give up instead of waiting forever
                    var completed = await Task.WhenAny(replyOutcome.Task, Task.Delay(10000));
                    Assert.Same(replyOutcome.Task, completed);
                    Assert.IsType<IOException>(await replyOutcome.Task);
                }
            }
        }
    }
}
