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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Helpers.Ipc;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    public class GreenshotProxyIntegrationTests
    {
        private static string GetProxyExePath()
        {
            // Locate greenshot-proxy.exe in the build output directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string candidate = Path.Combine(baseDir, "greenshot-proxy.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Fallback: check relative to solution output
            string slnOutput = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Greenshot\bin\Release\net480\greenshot-proxy.exe"));
            if (File.Exists(slnOutput))
            {
                return slnOutput;
            }

            string slnDebugOutput = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Greenshot\bin\Debug\net480\greenshot-proxy.exe"));
            if (File.Exists(slnDebugOutput))
            {
                return slnDebugOutput;
            }

            return candidate;
        }

        [Fact]
        public void Proxy_ExtensionMode_WhenGreenshotOffline_ReturnsOfflineJsonImmediately()
        {
            string proxyExe = GetProxyExePath();
            if (!File.Exists(proxyExe))
            {
                // Skip if binary not yet built in this test context
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = proxyExe,
                Arguments = "chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik/",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                Assert.NotNull(process);

                // Read 4-byte length prefix from stdout
                byte[] lenBytes = new byte[4];
                int read = process.StandardOutput.BaseStream.Read(lenBytes, 0, 4);
                Assert.Equal(4, read);

                uint payloadLen = BitConverter.ToUInt32(lenBytes, 0);
                Assert.True(payloadLen > 0 && payloadLen < 1024, $"Payload length {payloadLen} is unexpected.");

                byte[] payloadBytes = new byte[payloadLen];
                read = process.StandardOutput.BaseStream.Read(payloadBytes, 0, (int)payloadLen);
                Assert.Equal((int)payloadLen, read);

                string json = Encoding.UTF8.GetString(payloadBytes);
                JObject jobj = JObject.Parse(json);

                Assert.Equal("unavailable", jobj.Value<string>("status"));
                Assert.False(jobj.Value<bool>("greenshot_running"));
                Assert.True(jobj.Value<bool>("retry"));

                bool exited = process.WaitForExit(3000);
                Assert.True(exited, "Proxy should terminate immediately in offline extension mode.");
                Assert.Equal(0, process.ExitCode);
            }
        }

        [Fact]
        public async Task Proxy_FullDuplexRelay_TransparentlyPumpsFramedMessages()
        {
            string proxyExe = GetProxyExePath();
            if (!File.Exists(proxyExe))
            {
                return;
            }

            string pipeName = NamedPipeEndpoint.GetPipeName();
            var pipeSecurity = NamedPipeEndpoint.CreateServerSecurity();

            using (var pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                pipeSecurity))
            {
                var connectTask = pipeServer.WaitForConnectionAsync();

                var startInfo = new ProcessStartInfo
                {
                    FileName = proxyExe,
                    Arguments = "chrome-extension://test-extension-id/",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    Assert.NotNull(process);

                    var completed = await Task.WhenAny(connectTask, Task.Delay(5000));
                    Assert.Same(connectTask, completed);
                    Assert.True(pipeServer.IsConnected);

                    // 1. Send test framed message from test client -> proxy stdin -> pipe
                    string testMessage = "{\"source\":\"test\",\"data\":\"Hello from Extension\"}";
                    byte[] msgBytes = Encoding.UTF8.GetBytes(testMessage);
                    byte[] lenBytes = BitConverter.GetBytes((uint)msgBytes.Length);

                    process.StandardInput.BaseStream.Write(lenBytes, 0, 4);
                    process.StandardInput.BaseStream.Write(msgBytes, 0, msgBytes.Length);
                    process.StandardInput.BaseStream.Flush();

                    // Read from pipeServer
                    byte[] pipeLenBytes = new byte[4];
                    await ReadExactAsync(pipeServer, pipeLenBytes, 4);
                    uint receivedLen = BitConverter.ToUInt32(pipeLenBytes, 0);
                    Assert.Equal((uint)msgBytes.Length, receivedLen);

                    byte[] pipePayloadBytes = new byte[receivedLen];
                    await ReadExactAsync(pipeServer, pipePayloadBytes, (int)receivedLen);
                    string receivedMessage = Encoding.UTF8.GetString(pipePayloadBytes);
                    Assert.Equal(testMessage, receivedMessage);

                    // 2. Send test response from pipe -> proxy -> proxy stdout
                    string testResponse = "{\"status\":\"ok\",\"reply\":\"Hello from Greenshot\"}";
                    byte[] respBytes = Encoding.UTF8.GetBytes(testResponse);
                    byte[] respLenBytes = BitConverter.GetBytes((uint)respBytes.Length);

                    await pipeServer.WriteAsync(respLenBytes, 0, 4);
                    await pipeServer.WriteAsync(respBytes, 0, respBytes.Length);
                    await pipeServer.FlushAsync();

                    // Read from proxy stdout
                    byte[] stdoutLenBytes = new byte[4];
                    await ReadExactAsync(process.StandardOutput.BaseStream, stdoutLenBytes, 4);
                    uint stdoutLen = BitConverter.ToUInt32(stdoutLenBytes, 0);
                    Assert.Equal((uint)respBytes.Length, stdoutLen);

                    byte[] stdoutPayloadBytes = new byte[stdoutLen];
                    await ReadExactAsync(process.StandardOutput.BaseStream, stdoutPayloadBytes, (int)stdoutLen);
                    string receivedResponse = Encoding.UTF8.GetString(stdoutPayloadBytes);
                    Assert.Equal(testResponse, receivedResponse);

                    // Close stdin to signal clean shutdown
                    process.StandardInput.Close();
                    pipeServer.Close();

                    process.WaitForExit(3000);
                }
            }
        }

        [Fact]
        public void Proxy_CliMode_Help_OutputsUsageWithoutPipe()
        {
            string proxyExe = GetProxyExePath();
            if (!File.Exists(proxyExe))
            {
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = proxyExe,
                Arguments = "--help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                Assert.NotNull(process);
                string stdout = process.StandardOutput.ReadToEnd();
                bool exited = process.WaitForExit(3000);

                Assert.True(exited);
                Assert.Equal(0, process.ExitCode);
                Assert.Contains("Greenshot Proxy CLI", stdout);
                Assert.Contains("--list-recipes", stdout);
                Assert.Contains("--recipe", stdout);
            }
        }

        [Fact]
        public async Task Proxy_CliMode_ListRecipes_SendsIpcAndFormatsOutput()
        {
            string proxyExe = GetProxyExePath();
            if (!File.Exists(proxyExe))
            {
                return;
            }

            string pipeName = NamedPipeEndpoint.GetPipeName();
            var pipeSecurity = NamedPipeEndpoint.CreateServerSecurity();

            using (var pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                pipeSecurity))
            {
                var connectTask = pipeServer.WaitForConnectionAsync();

                var startInfo = new ProcessStartInfo
                {
                    FileName = proxyExe,
                    Arguments = "--list-recipes",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    Assert.NotNull(process);

                    var completed = await Task.WhenAny(connectTask, Task.Delay(5000));
                    Assert.Same(connectTask, completed);
                    Assert.True(pipeServer.IsConnected);

                    // Read request from proxy
                    byte[] pipeLenBytes = new byte[4];
                    await ReadExactAsync(pipeServer, pipeLenBytes, 4);
                    uint reqLen = BitConverter.ToUInt32(pipeLenBytes, 0);

                    byte[] reqBytes = new byte[reqLen];
                    await ReadExactAsync(pipeServer, reqBytes, (int)reqLen);
                    string reqJson = Encoding.UTF8.GetString(reqBytes);

                    JObject reqObj = JObject.Parse(reqJson);
                    Assert.Equal("LIST_RECIPES", reqObj.Value<string>("command"));

                    // Send mock recipes response
                    string mockResponse = "{\"status\":\"ok\",\"exit_code\":0,\"recipes\":[{\"command\":\"ocr\",\"id\":\"recipe_ocr\",\"name\":\"OCR\",\"description\":\"OCR text capture\"}]}";
                    byte[] respBytes = Encoding.UTF8.GetBytes(mockResponse);
                    byte[] respLenBytes = BitConverter.GetBytes((uint)respBytes.Length);

                    await pipeServer.WriteAsync(respLenBytes, 0, 4);
                    await pipeServer.WriteAsync(respBytes, 0, respBytes.Length);
                    await pipeServer.FlushAsync();

                    string stdout = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit(3000);

                    Assert.Equal(0, process.ExitCode);
                    Assert.Contains("ocr", stdout);
                    Assert.Contains("recipe_ocr", stdout);
                }
            }
        }

        [Fact]
        public async Task Proxy_CliMode_RunRecipe_SendsContextParametersAndPropagatesOutput()
        {
            string proxyExe = GetProxyExePath();
            if (!File.Exists(proxyExe))
            {
                return;
            }

            string pipeName = NamedPipeEndpoint.GetPipeName();
            var pipeSecurity = NamedPipeEndpoint.CreateServerSecurity();

            using (var pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                pipeSecurity))
            {
                var connectTask = pipeServer.WaitForConnectionAsync();

                var startInfo = new ProcessStartInfo
                {
                    FileName = proxyExe,
                    Arguments = "--recipe ocr destination=clipboard lang=eng",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    Assert.NotNull(process);

                    var completed = await Task.WhenAny(connectTask, Task.Delay(5000));
                    Assert.Same(connectTask, completed);
                    Assert.True(pipeServer.IsConnected);

                    // Read request from proxy
                    byte[] pipeLenBytes = new byte[4];
                    await ReadExactAsync(pipeServer, pipeLenBytes, 4);
                    uint reqLen = BitConverter.ToUInt32(pipeLenBytes, 0);

                    byte[] reqBytes = new byte[reqLen];
                    await ReadExactAsync(pipeServer, reqBytes, (int)reqLen);
                    string reqJson = Encoding.UTF8.GetString(reqBytes);

                    JObject reqObj = JObject.Parse(reqJson);
                    Assert.Equal("RUN_RECIPE", reqObj.Value<string>("command"));
                    Assert.Equal("ocr", reqObj.Value<string>("recipe"));
                    Assert.Equal("clipboard", reqObj["parameters"]?.Value<string>("destination"));
                    Assert.Equal("eng", reqObj["parameters"]?.Value<string>("lang"));

                    // Send mock flow result
                    string mockResponse = "{\"status\":\"ok\",\"exit_code\":0,\"stdout\":\"Recognized text: Hello World\"}";
                    byte[] respBytes = Encoding.UTF8.GetBytes(mockResponse);
                    byte[] respLenBytes = BitConverter.GetBytes((uint)respBytes.Length);

                    await pipeServer.WriteAsync(respLenBytes, 0, 4);
                    await pipeServer.WriteAsync(respBytes, 0, respBytes.Length);
                    await pipeServer.FlushAsync();

                    string stdout = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit(3000);

                    Assert.Equal(0, process.ExitCode);
                    Assert.Contains("Recognized text: Hello World", stdout);
                }
            }
        }

        private static async Task ReadExactAsync(Stream stream, byte[] buffer, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer, total, count - total);
                if (read == 0)
                {
                    throw new EndOfStreamException("Stream closed unexpectedly.");
                }
                total += read;
            }
        }
    }
}
