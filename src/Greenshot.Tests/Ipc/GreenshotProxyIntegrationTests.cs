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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Helpers.Ipc;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// Locates the native proxy executables in the solution build output (src\Greenshot\bin\&lt;Configuration&gt;\net480).
    /// </summary>
    internal static class ProxyBinaries
    {
        public const string Cli = "greenshot-cli.exe";
        public const string Proxy = "greenshot-proxy.exe";

        public static string Find(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string candidate = Path.Combine(baseDir, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Prefer the configuration the tests were built with
            string[] configurations = baseDir.IndexOf(@"\Release\", StringComparison.OrdinalIgnoreCase) >= 0
                ? new[] { "Release", "Debug" }
                : new[] { "Debug", "Release" };

            for (var dir = new DirectoryInfo(baseDir); dir != null; dir = dir.Parent)
            {
                foreach (var configuration in configurations)
                {
                    string path = Path.Combine(dir.FullName, "Greenshot", "bin", configuration, "net480", fileName);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            return null;
        }

        /// <summary>Reason to skip, or null when the executable exists and the Greenshot pipe is free.</summary>
        public static string GetSkipReason(string fileName, bool needsPipe)
        {
            if (Find(fileName) == null)
            {
                return $"{fileName} was not found in the build output (build the solution with MSBuild, including the C++ projects).";
            }
            if (needsPipe && Process.GetProcessesByName("Greenshot").Length > 0)
            {
                return "Greenshot is running and owns the pipe these tests need.";
            }
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ProxyFactAttribute : FactAttribute
    {
        public ProxyFactAttribute(string fileName, bool needsPipe = true)
        {
            Skip = ProxyBinaries.GetSkipReason(fileName, needsPipe);
        }
    }

    /// <summary>
    /// Runs greenshot-cli.exe / greenshot-proxy.exe against an in-process fake Greenshot pipe server.
    /// </summary>
    public class GreenshotProxyIntegrationTests
    {
        private sealed class ProcessResult
        {
            public int ExitCode;
            public string Stdout;
            public string Stderr;
        }

        private static ProcessStartInfo CreateStartInfo(string fileName, string arguments, bool redirectInput = false)
        {
            return new ProcessStartInfo
            {
                FileName = ProxyBinaries.Find(fileName),
                Arguments = arguments,
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            };
        }

        private static NamedPipeServerStream CreatePipeServer()
        {
            return new NamedPipeServerStream(
                NamedPipeEndpoint.GetPipeName(),
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0,
                0,
                NamedPipeEndpoint.CreateServerSecurity());
        }

        /// <summary>Starts the executable, lets <paramref name="server"/> talk to it, and collects its output.</summary>
        private static async Task<ProcessResult> RunAgainstFakeServerAsync(string fileName, string arguments, Func<NamedPipeServerStream, Task> server)
        {
            using (var pipeServer = CreatePipeServer())
            {
                var connectTask = pipeServer.WaitForConnectionAsync();
                using (var process = Process.Start(CreateStartInfo(fileName, arguments)))
                {
                    Assert.NotNull(process);
                    try
                    {
                        var stdoutTask = process.StandardOutput.ReadToEndAsync();
                        var stderrTask = process.StandardError.ReadToEndAsync();

                        var completed = await Task.WhenAny(connectTask, Task.Delay(10000));
                        Assert.Same(connectTask, completed);

                        await server(pipeServer);

                        Assert.True(process.WaitForExit(10000), "The executable did not exit.");
                        return new ProcessResult
                        {
                            ExitCode = process.ExitCode,
                            Stdout = await stdoutTask,
                            Stderr = await stderrTask
                        };
                    }
                    finally
                    {
                        KillIfRunning(process);
                    }
                }
            }
        }

        [ProxyFact(ProxyBinaries.Cli, needsPipe: false)]
        public void Cli_Help_IsAnsweredWithoutGreenshot()
        {
            using (var process = Process.Start(CreateStartInfo(ProxyBinaries.Cli, "--help")))
            {
                Assert.NotNull(process);
                string stdout = process.StandardOutput.ReadToEnd();
                Assert.True(process.WaitForExit(5000));
                Assert.Equal(0, process.ExitCode);
                Assert.Contains("Greenshot Proxy CLI", stdout);
                Assert.Contains("--list-recipes", stdout);
                Assert.Contains("--recipe", stdout);
            }
        }

        [ProxyFact(ProxyBinaries.Cli)]
        public async Task Cli_ForwardsArgumentsVerbatim_AndPrintsTextFrames()
        {
            var result = await RunAgainstFakeServerAsync(ProxyBinaries.Cli, "--recipe qr --file=a@b.png --offset -5 \"x \\\"y\\\" \u00fc\ud83d\ude00\" --json", async pipe =>
            {
                JObject hello = await ReadJsonFrameAsync(pipe);
                Assert.Equal("HELLO", hello.Value<string>("command"));
                Assert.Equal("cli", hello.Value<string>("source"));
                Assert.Equal("text", hello.Value<string>("reply_format"));

                JObject request = await ReadJsonFrameAsync(pipe);
                Assert.Equal("CLI", request.Value<string>("command"));
                Assert.Equal(
                    new[] { "--recipe", "qr", "--file=a@b.png", "--offset", "-5", "x \"y\" \u00fc\ud83d\ude00", "--json" },
                    request["argv"].Values<string>().ToArray());
                Assert.Equal(Path.GetTempPath().TrimEnd('\\'), request.Value<string>("cwd").TrimEnd('\\'), StringComparer.OrdinalIgnoreCase);

                await WriteTextFrameAsync(pipe, 'O', "result \ud83d\ude80\n");
                await WriteTextFrameAsync(pipe, 'E', "warning\n");
                await WriteExitFrameAsync(pipe, 7);
            });

            Assert.Equal(7, result.ExitCode);
            Assert.Equal("result \ud83d\ude80\n", result.Stdout);
            Assert.Equal("warning\n", result.Stderr);
        }

        [ProxyFact(ProxyBinaries.Cli)]
        public async Task Cli_ConnectionClosedWithoutExitFrame_ReportsFailure()
        {
            var result = await RunAgainstFakeServerAsync(ProxyBinaries.Cli, "--list-recipes", async pipe =>
            {
                await ReadJsonFrameAsync(pipe);
                await ReadJsonFrameAsync(pipe);
                pipe.Disconnect();
            });

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("closed", result.Stderr);
        }

        [ProxyFact(ProxyBinaries.Proxy)]
        public async Task Proxy_UrlScheme_AnnouncesUrlSchemeSource()
        {
            var result = await RunAgainstFakeServerAsync(ProxyBinaries.Proxy, "\"greenshot://recipe/qr?x=1\"", async pipe =>
            {
                JObject hello = await ReadJsonFrameAsync(pipe);
                Assert.Equal("url_scheme", hello.Value<string>("source"));
                Assert.Equal("text", hello.Value<string>("reply_format"));

                JObject request = await ReadJsonFrameAsync(pipe);
                Assert.Equal("CLI", request.Value<string>("command"));
                Assert.Equal(new[] { "greenshot://recipe/qr?x=1" }, request["argv"].Values<string>().ToArray());

                await WriteExitFrameAsync(pipe, 0);
            });

            Assert.Equal(0, result.ExitCode);
        }

        [ProxyFact(ProxyBinaries.Proxy)]
        public async Task Proxy_OpenWith_AnnouncesOpenWithSource()
        {
            var result = await RunAgainstFakeServerAsync(ProxyBinaries.Proxy, "--file \"C:\\some dir\\capture.png\"", async pipe =>
            {
                JObject hello = await ReadJsonFrameAsync(pipe);
                Assert.Equal("open_with", hello.Value<string>("source"));

                JObject request = await ReadJsonFrameAsync(pipe);
                Assert.Equal(new[] { "--file", "C:\\some dir\\capture.png" }, request["argv"].Values<string>().ToArray());

                await WriteExitFrameAsync(pipe, 0);
            });

            Assert.Equal(0, result.ExitCode);
        }

        [ProxyFact(ProxyBinaries.Proxy)]
        public void Proxy_ExtensionMode_WhenGreenshotOffline_ReturnsOfflineJsonImmediately()
        {
            using (var process = Process.Start(CreateStartInfo(ProxyBinaries.Proxy, "chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik/", redirectInput: true)))
            {
                Assert.NotNull(process);
                var stdout = process.StandardOutput.BaseStream;

                JObject offline;
                try
                {
                    offline = ReadJsonFrameAsync(stdout).GetAwaiter().GetResult();
                }
                finally
                {
                    if (!process.WaitForExit(5000))
                    {
                        KillIfRunning(process);
                    }
                }
                Assert.Equal("unavailable", offline.Value<string>("status"));
                Assert.False(offline.Value<bool>("greenshot_running"));
                Assert.True(offline.Value<bool>("retry"));

                Assert.True(process.WaitForExit(5000), "Proxy should terminate immediately in offline extension mode.");
                Assert.Equal(0, process.ExitCode);
            }
        }

        [ProxyFact(ProxyBinaries.Proxy)]
        public async Task Proxy_ExtensionRelay_SendsHelloThenPumpsFramesBothWays()
        {
            using (var pipeServer = CreatePipeServer())
            {
                var connectTask = pipeServer.WaitForConnectionAsync();
                using (var process = Process.Start(CreateStartInfo(ProxyBinaries.Proxy, "chrome-extension://test-extension-id/ --parent-window=0", redirectInput: true)))
                {
                    Assert.NotNull(process);
                    try
                    {
                        await StepAsync("connect", () => connectTask);

                        // The proxy announces the connection before relaying anything from the extension
                        JObject hello = await StepAsync("read HELLO from pipe", () => ReadJsonFrameAsync(pipeServer));
                        Assert.Equal("HELLO", hello.Value<string>("command"));
                        Assert.Equal("native_messaging", hello.Value<string>("source"));
                        Assert.Equal("json", hello.Value<string>("reply_format"));
                        Assert.Equal("chrome-extension://test-extension-id/", hello.Value<string>("origin"));

                        // Extension -> proxy stdin -> pipe
                        string message = "{\"command\":\"TAB_CHANGED\",\"url\":\"https://example.com/\"}";
                        await StepAsync("write message to proxy stdin", () => Task.Run(() => WriteFrame(process.StandardInput.BaseStream, Encoding.UTF8.GetBytes(message))));
                        byte[] relayed = await StepAsync("read relayed message from pipe", () => ReadFrameAsync(pipeServer));
                        Assert.Equal(message, Encoding.UTF8.GetString(relayed));

                        // Pipe -> proxy stdout -> extension
                        string response = "{\"status\":\"ok\",\"reply\":\"Hello from Greenshot\"}";
                        await StepAsync("write response to pipe", () => WriteFrameAsync(pipeServer, Encoding.UTF8.GetBytes(response)));
                        byte[] received = await StepAsync("read response from proxy stdout", () => Task.Run(() => ReadFrame(process.StandardOutput.BaseStream)));
                        Assert.Equal(response, Encoding.UTF8.GetString(received));

                        await StepAsync("close proxy stdin", () => Task.Run(() => process.StandardInput.Close()));
                        bool exited = await StepAsync("wait for proxy exit", () => Task.Run(() => process.WaitForExit(5000)));
                        Assert.True(exited, "Proxy should exit when the extension closes stdin.");
                    }
                    catch (TimeoutException ex)
                    {
                        string state = process.HasExited ? $"proxy exited with code {process.ExitCode}" : "proxy still running";
                        throw new TimeoutException($"{ex.Message} ({state})", ex);
                    }
                    finally
                    {
                        KillIfRunning(process);
                    }
                }
            }
        }

        /// <summary>Synchronous frame write for the process' anonymous pipes (async I/O on those streams is emulated on .NET Framework).</summary>
        private static void WriteFrame(Stream stream, byte[] payload)
        {
            stream.Write(BitConverter.GetBytes((uint)payload.Length), 0, 4);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        /// <summary>Synchronous frame read for the process' anonymous pipes.</summary>
        private static byte[] ReadFrame(Stream stream)
        {
            byte[] lengthBytes = ReadExact(stream, 4);
            return ReadExact(stream, (int)BitConverter.ToUInt32(lengthBytes, 0));
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int total = 0;
            while (total < count)
            {
                int read = stream.Read(buffer, total, count - total);
                if (read == 0)
                {
                    throw new EndOfStreamException("Stream closed unexpectedly.");
                }
                total += read;
            }
            return buffer;
        }

        /// <summary>
        /// Runs one step of an integration test off the test thread with a time limit, so a blocking call fails the test
        /// with the name of the step instead of hanging the whole test run.
        /// </summary>
        private static async Task StepAsync(string name, Func<Task> action, int timeoutSeconds = 15)
        {
            await StepAsync(name, async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            }, timeoutSeconds);
        }

        private static async Task<T> StepAsync<T>(string name, Func<Task<T>> action, int timeoutSeconds = 15)
        {
            var task = Task.Run(action);
            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds))) != task)
            {
                throw new TimeoutException($"Step '{name}' did not complete within {timeoutSeconds} seconds.");
            }
            return await task;
        }

        /// <summary>Reads one frame; fails the test instead of hanging when nothing arrives.</summary>
        private static async Task<byte[]> ReadFrameAsync(Stream stream, [System.Runtime.CompilerServices.CallerLineNumber] int callerLine = 0)
        {
            var readTask = ReadFrameCoreAsync(stream);
            if (await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(15))) != readTask)
            {
                throw new TimeoutException($"No frame received within 15 seconds (read at line {callerLine}).");
            }
            return await readTask;
        }

        private static async Task<byte[]> ReadFrameCoreAsync(Stream stream)
        {
            byte[] lengthBytes = new byte[4];
            await ReadExactAsync(stream, lengthBytes, 4);
            byte[] payload = new byte[BitConverter.ToUInt32(lengthBytes, 0)];
            await ReadExactAsync(stream, payload, payload.Length);
            return payload;
        }

        private static void KillIfRunning(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static async Task<JObject> ReadJsonFrameAsync(Stream stream, [System.Runtime.CompilerServices.CallerLineNumber] int callerLine = 0)
        {
            return JObject.Parse(Encoding.UTF8.GetString(await ReadFrameAsync(stream, callerLine)));
        }

        private static async Task WriteFrameAsync(Stream stream, byte[] payload)
        {
            byte[] lengthBytes = BitConverter.GetBytes((uint)payload.Length);
            await stream.WriteAsync(lengthBytes, 0, 4);
            await stream.WriteAsync(payload, 0, payload.Length);
            await stream.FlushAsync();
        }

        private static Task WriteTextFrameAsync(Stream stream, char type, string text)
        {
            var payload = new List<byte> { (byte)type };
            payload.AddRange(Encoding.UTF8.GetBytes(text));
            return WriteFrameAsync(stream, payload.ToArray());
        }

        private static Task WriteExitFrameAsync(Stream stream, int exitCode)
        {
            var payload = new List<byte> { (byte)'X' };
            payload.AddRange(BitConverter.GetBytes(exitCode));
            return WriteFrameAsync(stream, payload.ToArray());
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
