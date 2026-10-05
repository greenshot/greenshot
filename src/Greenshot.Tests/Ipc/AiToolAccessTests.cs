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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Ai;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Ipc;
using Greenshot.Ipc.Cli;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// The rules for AI tools (greenshot-mcp.exe): whitelist, consent, excluded processes, window ids and the LIST_WINDOWS /
    /// LIST_AI_TOOLS / RUN_AI_TOOL replies.
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
        [InlineData("LIST_AI_TOOLS", true)]
        [InlineData("RUN_AI_TOOL", true)]
        [InlineData("CAPTURE", false)]
        [InlineData("LIST_RECIPES", false)]
        [InlineData("DESCRIBE_RECIPE", false)]
        [InlineData("RUN_RECIPE", false)]
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
        public void Whitelist_OnlyMcp_CanUseTheAiToolCommands(string source)
        {
            Assert.False(IpcSecurityDispatcher.IsCommandAllowedForSource("LIST_WINDOWS", source));
            Assert.False(IpcSecurityDispatcher.IsCommandAllowedForSource("LIST_AI_TOOLS", source));
            Assert.False(IpcSecurityDispatcher.IsCommandAllowedForSource("RUN_AI_TOOL", source));
        }

        [Theory]
        [InlineData("RUN_AI_TOOL", "cli", true)]
        [InlineData("LIST_WINDOWS", "cli", true)]
        [InlineData("RUN_AI_TOOL", "mcp", true)]
        [InlineData("LIST_WINDOWS", "mcp", true)]
        [InlineData("LIST_AI_TOOLS", "mcp", false)]
        [InlineData("VERSION", "mcp", false)]
        [InlineData("RUN_RECIPE", "cli", false)]
        [InlineData("LIST_RECIPES", "native_messaging", false)]
        public void Consent_IsRequired(string command, string source, bool required)
        {
            Assert.Equal(required, IpcSecurityDispatcher.RequiresAiToolConsent(command, source));
        }

        private static readonly AiToolClient TestClient = new AiToolClient
        {
            ExePath = @"C:\Test\AiTool.exe",
            DisplayName = "Test AI tool"
        };

        /// <summary>
        /// Runs the test with the allowed programs and consent prompt set, and restores them afterwards.
        /// </summary>
        private static async Task WithConsentAsync(List<string> allowedClients, Func<AiToolClient, CancellationToken, Task<bool>> prompt, Func<ICoreConfiguration, Task> test)
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var previousAllowed = config.AiToolsAllowedClients;
            var previousPrompt = AiToolAccess.ConsentPrompt;
            try
            {
                config.AiToolsAllowedClients = allowedClients;
                AiToolAccess.ResetDeniedClients();
                AiToolAccess.ConsentPrompt = prompt;
                await test(config);
            }
            finally
            {
                AiToolAccess.ConsentPrompt = previousPrompt;
                AiToolAccess.ResetDeniedClients();
                config.AiToolsAllowedClients = previousAllowed;
            }
        }

        [Fact]
        public async Task RunAiTool_WhenTheUserDenies_IsRejected_AndNotAskedAgain()
        {
            var askedClients = new List<string>();
            await WithConsentAsync(new List<string>(), (client, cancellationToken) =>
            {
                askedClients.Add(client.ExePath);
                return Task.FromResult(false);
            }, async config =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var reply = await DispatchAsync("RUN_AI_TOOL", TestClient, recipe: "capture_screen");
                    Assert.Equal("error", reply.Value<string>("status"));
                    Assert.Equal(AiToolAccess.NotAllowedMessage, reply.Value<string>("stderr"));
                    Assert.Null(reply["image"]);
                }

                Assert.Equal(new[] { TestClient.ExePath }, askedClients);
                Assert.Empty(config.AiToolsAllowedClients);
            });
        }

        [Fact]
        public async Task DeniedClient_IsListed_AndAskedAgainAfterAskAgain()
        {
            int asked = 0;
            await WithConsentAsync(new List<string>(), (client, cancellationToken) =>
            {
                asked++;
                return Task.FromResult(false);
            }, async config =>
            {
                await DispatchAsync("LIST_WINDOWS", TestClient);
                Assert.Equal(new[] { TestClient.ExePath }, AiToolAccess.GetDeniedClients());

                // "Ask again" in the settings
                AiToolAccess.ForgetDenied(TestClient.ExePath);
                Assert.Empty(AiToolAccess.GetDeniedClients());
                await DispatchAsync("LIST_WINDOWS", TestClient);
                Assert.Equal(2, asked);
            });
        }

        /// <summary>
        /// OCR of a fixed text, the way Windows OCR reports it
        /// </summary>
        private sealed class FakeOcrProvider : Greenshot.Base.Interfaces.Ocr.IOcrProvider
        {
            public Task<List<IOcrLineFeature>> DoOcrAsync(System.Drawing.Image image, string languageTag = null) => Task.FromResult(Lines());

            public Task<List<IOcrLineFeature>> DoOcrAsync(Greenshot.Base.Interfaces.ISurface surface, string languageTag = null) => Task.FromResult(Lines());

            private static List<IOcrLineFeature> Lines() => new List<IOcrLineFeature>
            {
                new DetectedOcrLine(new NativeRect(5, 6, 70, 12), "Hello OCR", new List<OcrWordInfo>())
            };
        }

        [Theory]
        [InlineData(Greenshot.Recipes.RecipeManager.RecipeIdAiCaptureWindow)]
        [InlineData(Greenshot.Recipes.RecipeManager.RecipeIdAiCaptureRegion)]
        [InlineData(Greenshot.Recipes.RecipeManager.RecipeIdAiCaptureScreen)]
        public async Task AiCapture_WithOcr_ReturnsTheImage_TheText_AndTheLines(string recipeId)
        {
            // The surface factory of the tests
            TestEnvironment.EnsureInitialized();
            var recipe = Greenshot.Recipes.RecipeManager.Instance.GetBuiltInRecipe(recipeId);
            var ocrNode = recipe.FindNode("ocr");
            Assert.NotNull(ocrNode);

            var ocrProvider = new FakeOcrProvider();
            Greenshot.Base.Core.SimpleServiceProvider.Current.AddService<Greenshot.Base.Interfaces.Ocr.IOcrProvider>(ocrProvider);
            try
            {
                using var bitmap = new System.Drawing.Bitmap(120, 80);
                using var flowContext = new CaptureFlowContext(recipe)
                {
                    Payload = new CapturePayload(new Capture((System.Drawing.Image)bitmap.Clone()))
                };

                // The recipe's own OCR step: it makes a surface, which takes the image from the capture
                await new Greenshot.Recipes.Steps.ProcessorExecutionStep(ocrNode).ExecuteAsync(flowContext);
                Assert.NotNull(flowContext.Payload.Surface);

                var result = await AiToolIpcHandler.CollectResultAsync(flowContext, 0, CancellationToken.None);
                Assert.NotNull(result);
                Assert.NotNull(result.Png);
                Assert.Equal(120, result.OriginalWidth);
                Assert.Equal(80, result.OriginalHeight);
                Assert.Equal("Hello OCR", result.Text);
                Assert.Single(result.OcrLines);
            }
            finally
            {
                Greenshot.Base.Core.SimpleServiceProvider.Current.RemoveService<Greenshot.Base.Interfaces.Ocr.IOcrProvider>(ocrProvider);
            }
        }

        [Fact]
        public void HelperExecutable_IsNamedAfterItsApplication()
        {
            string root = Path.Combine(Path.GetTempPath(), "GreenshotTest-" + Guid.NewGuid().ToString("N"), "Antigravity");
            string bin = Path.Combine(root, "resources", "bin");
            Directory.CreateDirectory(bin);
            try
            {
                string helper = Path.Combine(bin, "language_server.exe");
                File.WriteAllBytes(helper, new byte[] { 0 });
                Assert.Null(AiToolCaller.FindApplicationName(helper));

                File.WriteAllBytes(Path.Combine(root, "Antigravity.exe"), new byte[] { 0 });
                // No version information: the directory's name
                Assert.Equal("Antigravity", AiToolCaller.FindApplicationName(helper));
                Assert.Equal("Antigravity (language_server)", AiToolCaller.Describe(helper).DisplayName);
                Assert.Equal(helper, AiToolCaller.Describe(helper).ExePath);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(root), true);
            }
        }

        [Fact]
        public async Task UnidentifiedClient_IsRejected_WithoutAsking()
        {
            bool asked = false;
            await WithConsentAsync(new List<string> { TestClient.ExePath }, (client, cancellationToken) =>
            {
                asked = true;
                return Task.FromResult(true);
            }, async config =>
            {
                var reply = await DispatchAsync("LIST_WINDOWS", null);
                Assert.Equal("error", reply.Value<string>("status"));
                Assert.False(asked);
            });
        }

        [Fact]
        public async Task OtherProgram_IsAsked_EvenWhenOneIsAllowed()
        {
            var otherClient = new AiToolClient { ExePath = @"C:\Other\Tool.exe", DisplayName = "Other tool" };
            var askedClients = new List<string>();
            await WithConsentAsync(new List<string> { TestClient.ExePath }, (client, cancellationToken) =>
            {
                askedClients.Add(client.ExePath);
                return Task.FromResult(false);
            }, async config =>
            {
                var reply = await DispatchAsync("LIST_WINDOWS", otherClient);
                Assert.Equal("error", reply.Value<string>("status"));
                Assert.Equal(new[] { otherClient.ExePath }, askedClients);
            });
        }

        [Fact]
        public async Task Version_FromMcp_NeedsNoConsent()
        {
            bool asked = false;
            await WithConsentAsync(new List<string>(), (client, cancellationToken) =>
            {
                asked = true;
                return Task.FromResult(false);
            }, async config =>
            {
                var reply = await DispatchAsync("VERSION", TestClient);
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.False(asked);
            });
        }

        [Fact]
        public async Task ListWindows_WhenAllowed_ReturnsWindowsAndDisplays()
        {
            await WithConsentAsync(new List<string> { TestClient.ExePath.ToUpperInvariant() }, (client, cancellationToken) => Task.FromResult(false), async config =>
            {
                var reply = await DispatchAsync("LIST_WINDOWS", TestClient);
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.IsType<JArray>(reply["windows"]);
                var displays = Assert.IsType<JArray>(reply["displays"]);
                Assert.NotEmpty(displays);
                foreach (var window in reply["windows"])
                {
                    // Ids, not window handles
                    Assert.Matches("^w[0-9]+$", window.Value<string>("id"));
                    Assert.Null(window["handle"]);
                    Assert.False(AiToolAccess.IsProcessExcluded(window.Value<string>("process")));
                }
            });
        }

        [Theory]
        [InlineData("w999999999")]
        [InlineData("0x1A2B3C")]
        [InlineData("Notepad")]
        public async Task CaptureWindow_OnlyAcceptsCurrentWindowIds(string window)
        {
            await WithConsentAsync(new List<string> { TestClient.ExePath }, (client, cancellationToken) => Task.FromResult(false), async config =>
            {
                var reply = await DispatchAsync("RUN_AI_TOOL", TestClient, new Dictionary<string, string> { ["window"] = window }, "capture_window");
                Assert.Equal("error", reply.Value<string>("status"));
                Assert.Contains("list_windows", reply.Value<string>("stderr"));
                Assert.Null(reply["image"]);
            });
        }

        [Fact]
        public async Task RunAiTool_UnknownTool_IsAnError()
        {
            await WithConsentAsync(new List<string> { TestClient.ExePath }, (client, cancellationToken) => Task.FromResult(false), async config =>
            {
                var reply = await DispatchAsync("RUN_AI_TOOL", TestClient, recipe: "no_such_tool");
                Assert.Equal("error", reply.Value<string>("status"));
                Assert.Contains("no_such_tool", reply.Value<string>("stderr"));
            });
        }

        [Fact]
        public async Task ListAiTools_NeedsNoConsent_AndListsTheBuiltInTools()
        {
            bool asked = false;
            await WithConsentAsync(new List<string>(), (client, cancellationToken) =>
            {
                asked = true;
                return Task.FromResult(false);
            }, async config =>
            {
                var reply = await DispatchAsync("LIST_AI_TOOLS", TestClient);
                Assert.Equal("ok", reply.Value<string>("status"));
                Assert.False(asked);
                var tools = Assert.IsType<JArray>(reply["tools"]);
                var captureWindow = tools.Single(t => t.Value<string>("name") == "capture_window");
                Assert.True(captureWindow.Value<bool>("read_only"));
                var window = captureWindow["arguments"].Single(a => a.Value<string>("name") == "window");
                Assert.Equal("Window", window.Value<string>("type"));
                Assert.True(window.Value<bool>("required"));
                Assert.Contains(tools, t => t.Value<string>("name") == "capture_region");
                Assert.Contains(tools, t => t.Value<string>("name") == "capture_screen");
                // greenshot-mcp's own tool name can't be taken by a recipe
                Assert.DoesNotContain(tools, t => t.Value<string>("name") == "list_windows");
            });
        }

        [Fact]
        public async Task AiToolRecipes_CannotBeRunFromTheCommandLine()
        {
            var envelope = new IpcEnvelope { Command = "RUN_RECIPE", Source = IpcSources.Cli, Recipe = Greenshot.Recipes.RecipeManager.RecipeIdAiCaptureScreen };
            var reply = await DispatchAsync(envelope, null);
            Assert.Equal("error", reply.Value<string>("status"));
            Assert.Contains("CommandlineTrigger", reply.Value<string>("stderr"));
        }

        /// <summary>
        /// Runs the test with a fake clock and fake windows (handle -> process id) for the window ids, restores them afterwards.
        /// </summary>
        private static void WithFakeWindows(Dictionary<long, int> windows, Action<Func<DateTime>, Action<TimeSpan>> test)
        {
            var previousClock = AiWindowRefs.UtcNow;
            var previousProcess = AiWindowRefs.GetWindowProcessId;
            var now = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            try
            {
                AiWindowRefs.Clear();
                AiWindowRefs.UtcNow = () => now;
                AiWindowRefs.GetWindowProcessId = handle => windows.TryGetValue(handle.ToInt64(), out int processId) ? processId : 0;
                test(() => now, timeSpan => now += timeSpan);
            }
            finally
            {
                AiWindowRefs.UtcNow = previousClock;
                AiWindowRefs.GetWindowProcessId = previousProcess;
                AiWindowRefs.Clear();
            }
        }

        [Fact]
        public void WindowIds_BelongToTheAiToolAndSession()
        {
            WithFakeWindows(new Dictionary<long, int> { [0x100] = 42 }, (now, advance) =>
            {
                var session = new AiToolClient { ExePath = @"C:\Test\AiTool.exe", ServerProcessId = 1 };
                string id = AiWindowRefs.Register(session, new IntPtr(0x100), 42);
                Assert.Equal(id, AiWindowRefs.Register(session, new IntPtr(0x100), 42));

                Assert.True(AiWindowRefs.TryResolve(session, id, out var handle, out _));
                Assert.Equal(0x100, handle.ToInt64());

                var otherTool = new AiToolClient { ExePath = @"C:\Other\Tool.exe", ServerProcessId = 1 };
                Assert.False(AiWindowRefs.TryResolve(otherTool, id, out _, out string error));
                Assert.Contains("list_windows", error);

                var otherSession = new AiToolClient { ExePath = @"C:\Test\AiTool.exe", ServerProcessId = 2 };
                Assert.False(AiWindowRefs.TryResolve(otherSession, id, out _, out _));
                Assert.False(AiWindowRefs.TryResolve(null, id, out _, out _));
            });
        }

        [Fact]
        public void WindowIds_Expire_AndAreNeverReused()
        {
            WithFakeWindows(new Dictionary<long, int> { [0x100] = 42, [0x200] = 43 }, (now, advance) =>
            {
                var client = new AiToolClient { ExePath = @"C:\Test\AiTool.exe", ServerProcessId = 1 };
                string id = AiWindowRefs.Register(client, new IntPtr(0x100), 42);
                advance(AiWindowRefs.Lifetime - TimeSpan.FromSeconds(1));
                Assert.True(AiWindowRefs.TryResolve(client, id, out _, out _));

                advance(TimeSpan.FromSeconds(2));
                Assert.False(AiWindowRefs.TryResolve(client, id, out _, out string error));
                Assert.Contains("list_windows", error);

                string newId = AiWindowRefs.Register(client, new IntPtr(0x200), 43);
                Assert.NotEqual(id, newId);
                Assert.NotEqual(id, AiWindowRefs.Register(client, new IntPtr(0x100), 42));
            });
        }

        [Fact]
        public void WindowIds_StopWorking_WhenTheWindowIsClosed()
        {
            var windows = new Dictionary<long, int> { [0x100] = 42 };
            WithFakeWindows(windows, (now, advance) =>
            {
                var client = new AiToolClient { ExePath = @"C:\Test\AiTool.exe", ServerProcessId = 1 };
                string id = AiWindowRefs.Register(client, new IntPtr(0x100), 42);

                // Windows reused the handle for a window of another process
                windows[0x100] = 99;
                Assert.False(AiWindowRefs.TryResolve(client, id, out _, out string error));
                Assert.Contains("closed", error);

                windows.Remove(0x100);
                string again = AiWindowRefs.Register(client, new IntPtr(0x100), 99);
                Assert.False(AiWindowRefs.TryResolve(client, again, out _, out _));
            });
        }

        [Theory]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-mcp.exe", @"C:\Program Files\Greenshot\", true)]
        [InlineData(@"C:\Program Files\Greenshot\GREENSHOT-MCP.EXE", @"C:\Program Files\Greenshot", true)]
        [InlineData(@"C:\Users\me\Downloads\greenshot-mcp.exe", @"C:\Program Files\Greenshot\", false)]
        [InlineData(@"C:\Program Files\Greenshot\other.exe", @"C:\Program Files\Greenshot\", false)]
        [InlineData(@"D:\code\greenshot\publish\greenshot-mcp.exe", @"C:\Program Files\Greenshot\", true)]
        [InlineData(null, @"C:\Program Files\Greenshot\", false)]
        public void McpServer_MustBeInGreenshotsDirectory(string serverPath, string greenshotDirectory, bool trusted)
        {
            var additional = new List<string> { @"D:\code\greenshot\publish" };
            Assert.Equal(trusted, AiToolCaller.IsTrustedMcpServer(serverPath, greenshotDirectory, additional));
        }

        [Theory]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-cli.exe", "cli", true)]
        [InlineData(@"C:\Program Files\Greenshot\Greenshot.exe", "cli", true)]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-proxy.exe", "url_scheme", true)]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-proxy.exe", "native_messaging", true)]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-proxy.exe", "open_with", true)]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-proxy.exe", "cli", false)]
        [InlineData(@"C:\Program Files\Greenshot\greenshot-cli.exe", "native_messaging", false)]
        [InlineData(@"C:\Users\me\Downloads\greenshot-cli.exe", "cli", false)]
        [InlineData(@"C:\Program Files\Greenshot\evil.exe", "cli", false)]
        [InlineData(null, "cli", false)]
        public void PipeClients_MustBeGreenshotsOwnPrograms(string clientPath, string source, bool allowed)
        {
            Assert.Equal(allowed, IpcClientVerifier.IsAllowed(clientPath, source, @"C:\Program Files\Greenshot\", out string error));
            Assert.Equal(allowed, error == null);
        }

        [Theory]
        [InlineData(@"C:\Windows\System32\cmd.exe", true)]
        [InlineData(@"C:\Program Files\PowerShell\7\pwsh.exe", true)]
        [InlineData(@"C:\Users\me\AppData\Local\AnthropicClaude\claude.exe", false)]
        [InlineData(@"C:\Program Files\Microsoft VS Code\Code.exe", false)]
        public void Launchers_AreSkipped(string exePath, bool launcher)
        {
            Assert.Equal(launcher, AiToolCaller.IsLauncher(exePath));
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

        [Fact]
        public void WindowArguments_AreOnlyForAiTools()
        {
            var declared = new List<CommandlineArgument> { new CommandlineArgument { Name = "window", Type = ContractDataType.Window } };
            var supplied = new Dictionary<string, string> { ["window"] = "0x1A2B3C" };

            var fromCli = CommandlineArgumentBinder.Bind(declared, supplied, "test", Path.GetTempPath(), IpcSources.Cli);
            Assert.False(fromCli.Success);
            Assert.Contains("only available to AI tools", fromCli.Error);

            var handleFromAi = CommandlineArgumentBinder.Bind(declared, supplied, "test", null, IpcSources.Mcp, TestClient);
            Assert.False(handleFromAi.Success);
            Assert.Contains("list_windows", handleFromAi.Error);
        }

        [Theory]
        [InlineData("10,20,300,400", true)]
        [InlineData(" -1920 , 0 , 1920 , 1080 ", true)]
        [InlineData("10;20;300;400", true)]
        [InlineData("10,20,0,400", false)]
        [InlineData("10,20,300", false)]
        [InlineData("a,b,c,d", false)]
        public void RegionArguments_AreParsed(string text, bool valid)
        {
            var declared = new List<CommandlineArgument> { new CommandlineArgument { Name = "region", Variable = "PreSuppliedRegion", Type = ContractDataType.Region } };
            var result = CommandlineArgumentBinder.Bind(declared, new Dictionary<string, string> { ["region"] = text }, "test", null, IpcSources.Mcp, TestClient);
            Assert.Equal(valid, result.Success);
            if (valid)
            {
                var region = Assert.IsType<NativeRect>(result.Variables["PreSuppliedRegion"]);
                Assert.True(region.Width > 0 && region.Height > 0);
            }
        }

        [Theory]
        [InlineData("capture_window", true)]
        [InlineData("my-tool_2", true)]
        [InlineData("", false)]
        [InlineData("capture window", false)]
        [InlineData("fenêtre", false)]
        public void AiToolTrigger_NeedsAValidToolName(string toolName, bool valid)
        {
            var recipe = new CaptureRecipe("ai_tool_name_test", "AI tool name test")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.FullScreen))
                .AddTrigger(TriggerConfig.CreateAiTool(toolName, "Test tool"));
            recipe.Flow = new RecipeFlowConfig("acquire");
            var result = RecipeValidator.Validate(recipe);
            Assert.Equal(valid, !result.Errors.Any(e => e.Contains("ToolName")));
        }

        [Fact]
        public void AiToolTrigger_AddsNoDestination()
        {
            var recipe = new CaptureRecipe("ai_tool_destination_test", "AI tool destination test")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.FullScreen));
            recipe.Flow = new RecipeFlowConfig("acquire");

            var forAiTool = TriggerRecipePreparer.Prepare(recipe, new AiToolTrigger("t", "t", recipe.Id, "tool"));
            Assert.False(forAiTool.HasDestinationStep());

            var forCommandline = TriggerRecipePreparer.Prepare(recipe, new CommandlineTrigger("c", "c", recipe.Id));
            Assert.True(forCommandline.HasDestinationStep());
        }

        [Theory]
        [InlineData("0x1A2B", 0x1A2B)]
        [InlineData("0X00ff", 0xFF)]
        [InlineData("6699", 6699)]
        public void Handles_AreParsed(string text, long expected)
        {
            Assert.True(AiToolCapture.TryParseHandle(text, out var handle));
            Assert.Equal(expected, handle.ToInt64());
        }

        [Theory]
        [InlineData("")]
        [InlineData("0x")]
        [InlineData("0")]
        [InlineData("window")]
        public void Handles_InvalidAreRejected(string text)
        {
            Assert.False(AiToolCapture.TryParseHandle(text, out _));
        }

        [Fact]
        public void AiTools_AreOptIn_AndRecipeProposalsCanBeSwitchedOff()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            bool previousEnabled = config.AiToolsEnabled;
            bool previousProposals = config.AiToolsAllowRecipeProposals;
            try
            {
                // Off: only greenshot-mcp's version passes, nobody is asked
                config.AiToolsEnabled = false;
                Assert.Null(IpcSecurityDispatcher.GetAiToolsOptInError("VERSION", IpcSources.Mcp));
                Assert.Equal(AiToolAccess.DisabledMessage, IpcSecurityDispatcher.GetAiToolsOptInError("LIST_WINDOWS", IpcSources.Mcp));
                Assert.Equal(AiToolAccess.DisabledMessage, IpcSecurityDispatcher.GetAiToolsOptInError("LIST_AI_TOOLS", IpcSources.Mcp));
                Assert.Equal(AiToolAccess.DisabledMessage, IpcSecurityDispatcher.GetAiToolsOptInError("PROPOSE_RECIPE", IpcSources.Mcp));
                // Other sources aren't AI tools
                Assert.Null(IpcSecurityDispatcher.GetAiToolsOptInError("CAPTURE", IpcSources.Cli));

                config.AiToolsEnabled = true;
                Assert.Null(IpcSecurityDispatcher.GetAiToolsOptInError("LIST_WINDOWS", IpcSources.Mcp));
                Assert.Null(IpcSecurityDispatcher.GetAiToolsOptInError("PROPOSE_RECIPE", IpcSources.Mcp));

                config.AiToolsAllowRecipeProposals = false;
                Assert.Equal(AiToolAccess.ProposalsDisabledMessage, IpcSecurityDispatcher.GetAiToolsOptInError("PROPOSE_RECIPE", IpcSources.Mcp));
                Assert.Null(IpcSecurityDispatcher.GetAiToolsOptInError("RUN_AI_TOOL", IpcSources.Mcp));
            }
            finally
            {
                config.AiToolsEnabled = previousEnabled;
                config.AiToolsAllowRecipeProposals = previousProposals;
            }
        }

        private static Task<JObject> DispatchAsync(string command, AiToolClient client, Dictionary<string, string> parameters = null, string recipe = null)
        {
            var envelope = new IpcEnvelope
            {
                Command = command,
                Source = IpcSources.Mcp,
                Recipe = recipe
            };
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    envelope.Parameters[parameter.Key] = parameter.Value;
                }
            }
            return DispatchAsync(envelope, client);
        }

        private static async Task<JObject> DispatchAsync(IpcEnvelope envelope, AiToolClient client)
        {
            // AI tools are opt-in: these tests are about what happens once they are switched on
            IniConfigRegistry.GetSection<ICoreConfiguration>().AiToolsEnabled = true;
            using var stream = new MemoryStream();
            var context = new IpcRequestContext(envelope, stream)
            {
                ConnectionOrigin = "test-client",
                AiClient = client
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
