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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Helpers.Ipc;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// The rules for AI tools (greenshot-mcp.exe): whitelist, consent, excluded processes and the LIST_WINDOWS / CAPTURE replies.
    /// Shares the collection with the other dispatcher tests, as it changes the core configuration.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class AiToolAccessTests
    {
        [Fact]
        public void IpcSources_Mcp_IsKnown()
        {
            Assert.True(IpcSources.IsKnown("mcp"));
            Assert.True(IpcSources.IsKnown("MCP"));
        }

        [Theory]
        [InlineData("VERSION", true)]
        [InlineData("LIST_WINDOWS", true)]
        [InlineData("CAPTURE", true)]
        [InlineData("LIST_RECIPES", true)]
        [InlineData("DESCRIBE_RECIPE", true)]
        [InlineData("RUN_RECIPE", true)]
        [InlineData("EXIT", false)]
        [InlineData("RELOAD_CONFIG", false)]
        [InlineData("OPEN_FILE", false)]
        [InlineData("CLI", false)]
        [InlineData("SETTINGS", false)]
        [InlineData("IMPORT_CAPTURE", false)]
        public void Whitelist_McpSource(string command, bool allowed)
        {
            Assert.Equal(allowed, IpcSecurityDispatcher.IsCommandAllowedForSource(command, IpcSources.Mcp));
        }

        [Theory]
        [InlineData("native_messaging")]
        [InlineData("url_scheme")]
        [InlineData("open_with")]
        [InlineData("cli")]
        [InlineData("")]
        [InlineData(null)]
        public void Whitelist_OnlyMcp_CanCapture(string source)
        {
            Assert.False(IpcSecurityDispatcher.IsCommandAllowedForSource("CAPTURE", source));
            Assert.False(IpcSecurityDispatcher.IsCommandAllowedForSource("LIST_WINDOWS", source));
        }

        [Theory]
        [InlineData("CAPTURE", "cli", true)]
        [InlineData("LIST_WINDOWS", "cli", true)]
        [InlineData("RUN_RECIPE", "mcp", true)]
        [InlineData("LIST_RECIPES", "mcp", true)]
        [InlineData("VERSION", "mcp", false)]
        [InlineData("RUN_RECIPE", "cli", false)]
        [InlineData("LIST_RECIPES", "native_messaging", false)]
        public void Consent_IsRequired(string command, string source, bool required)
        {
            Assert.Equal(required, IpcSecurityDispatcher.RequiresAiToolConsent(command, source));
        }

        [Fact]
        public async Task Capture_WhenTheUserDenies_IsRejected_AndNotAskedAgain()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            bool previousAllow = config.AllowAiTools;
            var previousPrompt = AiToolAccess.ConsentPrompt;
            var askedClients = new List<string>();
            try
            {
                config.AllowAiTools = false;
                AiToolAccess.ResetDeniedClients();
                AiToolAccess.ConsentPrompt = (client, cancellationToken) =>
                {
                    askedClients.Add(client);
                    return Task.FromResult(false);
                };

                for (int i = 0; i < 2; i++)
                {
                    var reply = await DispatchAsync("CAPTURE", "Test AI tool", new Dictionary<string, string> { ["target"] = "screen" });
                    Assert.Equal("error", reply.Value<string>("status"));
                    Assert.Equal(AiToolAccess.NotAllowedMessage, reply.Value<string>("stderr"));
                    Assert.Null(reply["data"]);
                }

                Assert.Equal(new[] { "Test AI tool" }, askedClients);
                Assert.False(config.AllowAiTools);
            }
            finally
            {
                AiToolAccess.ConsentPrompt = previousPrompt;
                AiToolAccess.ResetDeniedClients();
                config.AllowAiTools = previousAllow;
            }
        }

        [Fact]
        public async Task Version_FromMcp_NeedsNoConsent()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            bool previousAllow = config.AllowAiTools;
            var previousPrompt = AiToolAccess.ConsentPrompt;
            bool asked = false;
            try
            {
                config.AllowAiTools = false;
                AiToolAccess.ResetDeniedClients();
                AiToolAccess.ConsentPrompt = (client, cancellationToken) =>
                {
                    asked = true;
                    return Task.FromResult(false);
                };

                var reply = await DispatchAsync("VERSION", "Test AI tool");
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.False(asked);
            }
            finally
            {
                AiToolAccess.ConsentPrompt = previousPrompt;
                AiToolAccess.ResetDeniedClients();
                config.AllowAiTools = previousAllow;
            }
        }

        [Fact]
        public async Task ListWindows_WhenAllowed_ReturnsWindowsAndDisplays()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            bool previousAllow = config.AllowAiTools;
            try
            {
                config.AllowAiTools = true;

                var reply = await DispatchAsync("LIST_WINDOWS", "Test AI tool");
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.IsType<JArray>(reply["windows"]);
                var displays = Assert.IsType<JArray>(reply["displays"]);
                Assert.NotEmpty(displays);
                foreach (var window in reply["windows"])
                {
                    Assert.StartsWith("0x", window.Value<string>("handle"));
                    Assert.False(AiToolAccess.IsProcessExcluded(window.Value<string>("process")));
                }
            }
            finally
            {
                config.AllowAiTools = previousAllow;
            }
        }

        [Fact]
        public async Task Capture_UnknownHandle_IsAnError()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            bool previousAllow = config.AllowAiTools;
            try
            {
                config.AllowAiTools = true;

                var reply = await DispatchAsync("CAPTURE", "Test AI tool", new Dictionary<string, string> { ["handle"] = "not-a-handle" });
                Assert.Equal("error", reply.Value<string>("status"));
                Assert.Contains("not a window handle", reply.Value<string>("stderr"));
            }
            finally
            {
                config.AllowAiTools = previousAllow;
            }
        }

        [Theory]
        [InlineData("KeePass", true)]
        [InlineData("keepass.exe", true)]
        [InlineData("1Password.exe", true)]
        [InlineData("notepad", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ExcludedProcesses_DefaultList(string processName, bool excluded)
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var previous = config.AiToolsExcludedProcesses;
            try
            {
                config.AiToolsExcludedProcesses = new List<string> { "KeePass", "KeePassXC", "1Password", "Bitwarden" };
                Assert.Equal(excluded, AiToolAccess.IsProcessExcluded(processName));
            }
            finally
            {
                config.AiToolsExcludedProcesses = previous;
            }
        }

        [Theory]
        [InlineData("0x1A2B", 0x1A2B)]
        [InlineData("0X00ff", 0xFF)]
        [InlineData("6699", 6699)]
        public void Handles_AreParsed(string text, long expected)
        {
            Assert.True(AiToolIpcHandler.TryParseHandle(text, out var handle));
            Assert.Equal(expected, handle.ToInt64());
            Assert.Equal("0x" + expected.ToString("X"), AiToolIpcHandler.FormatHandle(handle));
        }

        [Theory]
        [InlineData("")]
        [InlineData("0x")]
        [InlineData("0")]
        [InlineData("window")]
        public void Handles_InvalidAreRejected(string text)
        {
            Assert.False(AiToolIpcHandler.TryParseHandle(text, out _));
        }

        private static async Task<JObject> DispatchAsync(string command, string origin, Dictionary<string, string> parameters = null)
        {
            var envelope = new IpcEnvelope
            {
                Command = command,
                Source = IpcSources.Mcp
            };
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    envelope.Parameters[parameter.Key] = parameter.Value;
                }
            }

            using var stream = new MemoryStream();
            var context = new IpcRequestContext(envelope, stream)
            {
                ConnectionOrigin = origin
            };
            await IpcSecurityDispatcher.DispatchAsync(context, null, null, null, null, null);

            stream.Position = 0;
            byte[] lengthBytes = new byte[4];
            Assert.Equal(4, stream.Read(lengthBytes, 0, 4));
            int length = BitConverter.ToInt32(lengthBytes, 0);
            byte[] payload = new byte[length];
            Assert.Equal(length, stream.Read(payload, 0, length));
            return JObject.Parse(Encoding.UTF8.GetString(payload));
        }
    }
}
