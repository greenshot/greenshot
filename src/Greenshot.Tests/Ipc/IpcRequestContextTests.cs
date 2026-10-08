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
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Ipc;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// Replies of the IPC server to a client which already closed its end of the pipe
    /// </summary>
    public class IpcRequestContextTests
    {
        private static IpcEnvelope CreateEnvelope() => IpcEnvelope.CreateCli(new[] { "--exit" }, IpcSources.Cli, @"C:\");

        private static async Task<NamedPipeServerStream> ConnectAsync(string pipeName, PipeDirection clientDirection, Action<NamedPipeClientStream> client)
        {
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var waitForConnection = server.WaitForConnectionAsync();
            using (var clientStream = new NamedPipeClientStream(".", pipeName, clientDirection, PipeOptions.None))
            {
                clientStream.Connect(2000);
                await waitForConnection;
                client(clientStream);
            }

            return server;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Reply_ClientClosedThePipe_IsDroppedWithoutError(bool usesTextFrames)
        {
            string pipeName = "greenshot-test-" + Guid.NewGuid().ToString("N");
            // Like a second Greenshot.exe: it sends its command and closes the pipe without reading the reply
            using var server = await ConnectAsync(pipeName, PipeDirection.Out, client => { });
            var context = new IpcRequestContext(CreateEnvelope(), server) { UsesTextFrames = usesTextFrames };

            await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout = "done" });
            // Nothing more is written after the client is gone
            await context.ReplyAsync(new { stream = "stdout", text = "more" });
            await context.CompleteAsync(0);
        }

        [Fact]
        public async Task Reply_ClientReads_GetsTheReply()
        {
            string pipeName = "greenshot-test-" + Guid.NewGuid().ToString("N");
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            using (server)
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                var waitForConnection = server.WaitForConnectionAsync();
                client.Connect(2000);
                await waitForConnection;

                var context = new IpcRequestContext(CreateEnvelope(), server);
                // The pipe has no buffer: the write completes while the client reads
                var reply = context.ReplyAsync(new { status = "ok" });

                byte[] lengthBytes = new byte[4];
                Assert.Equal(4, client.Read(lengthBytes, 0, 4));
                int length = BitConverter.ToInt32(lengthBytes, 0);
                byte[] payload = new byte[length];
                Assert.Equal(length, client.Read(payload, 0, length));
                await reply;
                Assert.Contains("\"status\":\"ok\"", Encoding.UTF8.GetString(payload));
            }
        }
    }
}
