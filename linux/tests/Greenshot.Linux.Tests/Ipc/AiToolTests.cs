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

using System.Text;
using System.Text.Json.Nodes;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Compositor;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Ai;
using Approval = Greenshot.Linux.Recipes.Approval;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Ipc;
using Greenshot.Linux.Tests.Recipes;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Ipc;

/// <summary>
/// AI tools through greenshot-mcp: who is on the other end (/proc), consent, exclusions, window ids, blacking out and the
/// commands of the mcp source, like the AiToolAccessTests of Windows
/// </summary>
[Collection(TestCollections.RecipeManager)]
public sealed class AiToolTests : IDisposable
{
    private const string GreenshotDirectory = "/opt/greenshot";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gs-ai-" + Guid.NewGuid().ToString("N"));
    private readonly CoreSettings _settings;
    private readonly Func<CoreSettings?> _previousConfiguration = AiToolAccess.ConfigurationProvider;
    private readonly Func<AiToolClient, CancellationToken, Task<bool>>? _previousPrompt = AiToolAccess.ConsentPrompt;
    private readonly string _previousProcRoot = AiToolCaller.ProcRoot;
    private readonly Func<Task<IReadOnlyList<WindowInfo>>> _previousWindows = AiWindowRefs.CurrentWindows;
    private readonly Func<string> _previousDirectory = AiToolsIpc.GreenshotDirectory;
    private readonly ICompositor _previousCompositor = RecipeServices.Compositor;
    private readonly ICaptureFlowRunner _previousRunner = RecipeServices.Runner;
    private readonly IUiDispatcher _previousUi = RecipeServices.Ui;
    private int _prompts;

    public AiToolTests()
    {
        TestEnvironment.EnsureInitialized();
        // Registers the built-in steps, like the app does at its start
        _ = CapturePipeline.Instance;
        Directory.CreateDirectory(_folder);
        _settings = CoreSettings.Load(Path.Combine(_folder, "greenshot.ini"));
        _settings.AiToolsEnabled = true;
        AiToolAccess.ConfigurationProvider = () => _settings;
        AiToolAccess.ResetDeniedClients();
        AiToolAccess.ConsentPrompt = (_, _) =>
        {
            _prompts++;
            return Task.FromResult(false);
        };
        AiToolCaller.ProcRoot = Path.Combine(_folder, "proc");
        AiToolsIpc.GreenshotDirectory = () => GreenshotDirectory;
        AiWindowRefs.Clear();
    }

    public void Dispose()
    {
        AiToolAccess.ConfigurationProvider = _previousConfiguration;
        AiToolAccess.ConsentPrompt = _previousPrompt;
        AiToolAccess.ResetDeniedClients();
        AiToolCaller.ProcRoot = _previousProcRoot;
        AiWindowRefs.CurrentWindows = _previousWindows;
        AiWindowRefs.UtcNow = () => DateTime.UtcNow;
        AiWindowRefs.Clear();
        AiToolsIpc.GreenshotDirectory = _previousDirectory;
        RecipeServices.Compositor = _previousCompositor;
        RecipeServices.Runner = _previousRunner;
        RecipeServices.Ui = _previousUi;
        Directory.Delete(_folder, true);
    }

    private static AiToolClient Client(string identity = "/usr/bin/claude", int server = 4242) =>
        new() { Identity = identity, ExePath = identity.Split(' ')[0], DisplayName = Path.GetFileName(identity), ServerProcessId = server };

    #region /proc

    /// <summary>
    /// A process in the fake /proc: its executable, parent, command line and working directory
    /// </summary>
    private void Process(int pid, int parent, string exe, string[]? commandLine = null, string cwd = "/home/user", long startTime = 100)
    {
        string folder = Path.Combine(_folder, "proc", pid.ToString());
        Directory.CreateDirectory(folder);
        File.CreateSymbolicLink(Path.Combine(folder, "exe"), exe);
        File.CreateSymbolicLink(Path.Combine(folder, "cwd"), cwd);
        string name = Path.GetFileName(exe);
        // pid (comm) state ppid ... the start time is field 22
        var fields = new List<string> { "S", parent.ToString() };
        fields.AddRange(Enumerable.Repeat("0", 17));
        fields.Add(startTime.ToString());
        fields.AddRange(Enumerable.Repeat("0", 5));
        File.WriteAllText(Path.Combine(folder, "stat"), $"{pid} ({name}) {string.Join(' ', fields)}");
        File.WriteAllText(Path.Combine(folder, "comm"), name + "\n");
        File.WriteAllText(Path.Combine(folder, "cmdline"), string.Join('\0', commandLine ?? [exe]) + "\0");
    }

    [Fact]
    public void Caller_IsTheProgramWhichStartedTheServer_SkippingShells()
    {
        Process(1000, 1, "/usr/bin/claude", startTime: 10);
        Process(1001, 1000, "/usr/bin/bash", startTime: 20);
        Process(1002, 1001, GreenshotDirectory + "/greenshot-mcp", startTime: 30);

        Assert.True(AiToolCaller.TryIdentify(1002, GreenshotDirectory + "/", null, out var client, out var error), error);
        Assert.Equal("/usr/bin/claude", client!.Identity);
        Assert.Equal("claude", client.DisplayName);
        Assert.Equal(1002, client.ServerProcessId);
    }

    [Fact]
    public void Caller_WithAnInterpreter_IsIdentifiedWithItsScript()
    {
        const string script = "/usr/lib/node_modules/@anthropic-ai/claude-code/cli.js";
        Process(1000, 1, "/usr/bin/node", ["node", "--no-warnings", script, "mcp"], startTime: 10);
        Process(1002, 1000, GreenshotDirectory + "/greenshot-mcp", startTime: 30);

        Assert.True(AiToolCaller.TryIdentify(1002, GreenshotDirectory, null, out var client, out var error), error);
        Assert.Equal("/usr/bin/node " + script, client!.Identity);
        Assert.Equal("claude-code (node)", client.DisplayName);
    }

    [Theory]
    [InlineData("/tmp/greenshot-mcp", false)]
    [InlineData(GreenshotDirectory + "/greenshot-mcp", true)]
    [InlineData(GreenshotDirectory + "/other-server", false)]
    public void Server_MustBeGreenshotMcpFromGreenshotsFolder(string serverPath, bool trusted)
    {
        Process(1000, 1, "/usr/bin/claude", startTime: 10);
        Process(1002, 1000, serverPath, startTime: 30);

        Assert.Equal(trusted, AiToolCaller.TryIdentify(1002, GreenshotDirectory, null, out _, out var error));
        Assert.Equal(trusted, error == null);
    }

    [Fact]
    public void Server_OfADevelopmentBuild_IsTheDllDotnetRuns()
    {
        Process(1000, 1, "/usr/bin/claude", startTime: 10);
        Process(1002, 1000, "/usr/lib64/dotnet/dotnet", ["dotnet", GreenshotDirectory + "/greenshot-mcp.dll"], startTime: 30);
        Process(1003, 1000, "/usr/lib64/dotnet/dotnet", ["dotnet", "/tmp/greenshot-mcp.dll"], startTime: 30);

        Assert.True(AiToolCaller.TryIdentify(1002, GreenshotDirectory, null, out var client, out _));
        Assert.Equal("/usr/bin/claude", client!.Identity);
        Assert.False(AiToolCaller.TryIdentify(1003, GreenshotDirectory, null, out _, out _));
        Assert.True(AiToolCaller.TryIdentify(1003, GreenshotDirectory, ["/tmp"], out _, out _));
    }

    [Fact]
    public void Caller_AParentStartedAfterTheServer_IsAReusedProcessId()
    {
        Process(1000, 1, "/usr/bin/claude", startTime: 50);
        Process(1002, 1000, GreenshotDirectory + "/greenshot-mcp", startTime: 30);

        Assert.False(AiToolCaller.TryIdentify(1002, GreenshotDirectory, null, out _, out var error));
        Assert.Contains("Could not identify", error);
    }

    [Fact]
    public void Caller_UnknownProcess_IsRejected()
    {
        Assert.False(AiToolCaller.TryIdentify(0, GreenshotDirectory, null, out _, out _));
        Assert.False(AiToolCaller.TryIdentify(999999, GreenshotDirectory, null, out _, out _));
    }

    #endregion

    #region Consent and exclusions

    [Fact]
    public async Task Consent_Denied_IsNotAskedAgain_UntilAskAgain()
    {
        var client = Client();
        Assert.False(await AiToolAccess.EnsureAllowedAsync(client));
        Assert.False(await AiToolAccess.EnsureAllowedAsync(client));
        Assert.Equal(1, _prompts);
        Assert.Contains(client.Identity, AiToolAccess.GetDeniedClients());

        AiToolAccess.ForgetDenied(client.Identity);
        Assert.False(await AiToolAccess.EnsureAllowedAsync(client));
        Assert.Equal(2, _prompts);
    }

    [Fact]
    public async Task Consent_Allowed_IsSaved_AndOtherProgramsAreStillAsked()
    {
        AiToolAccess.ConsentPrompt = (_, _) =>
        {
            _prompts++;
            return Task.FromResult(true);
        };
        Assert.True(await AiToolAccess.EnsureAllowedAsync(Client()));
        Assert.True(await AiToolAccess.EnsureAllowedAsync(Client()));
        Assert.Equal(1, _prompts);
        Assert.Contains("/usr/bin/claude", CoreSettings.Load(_settings.FilePath).AiToolsAllowedClients);

        Assert.True(await AiToolAccess.EnsureAllowedAsync(Client("/usr/bin/node /opt/other/cli.js")));
        Assert.Equal(2, _prompts);
        Assert.False(await AiToolAccess.EnsureAllowedAsync(null));
    }

    [Theory]
    [InlineData("KeePassXC", null, true)]
    [InlineData("keepassxc", null, true)]
    [InlineData("other", "org.keepassxc.KeePassXC", true)]
    [InlineData("other", "Bitwarden", true)]
    [InlineData("firefox", "org.mozilla.firefox", false)]
    public void Exclusions_MatchTheProcessOrTheAppId(string processName, string? appId, bool excluded)
    {
        Process(2000, 1, "/usr/bin/" + processName);
        Assert.Equal(excluded, AiToolAccess.IsWindowExcluded(new WindowInfo("Title", appId, new LayoutRect(0, 0, 10, 10), 2000, "1")));
    }

    [Fact]
    public void AiTools_AreOptIn_AndProposalsCanBeSwitchedOff()
    {
        Assert.Null(AiToolsIpc.GetAiToolsOptInError("LIST_WINDOWS", IpcSources.Mcp));
        _settings.AiToolsEnabled = false;
        Assert.Equal(AiToolAccess.DisabledMessage, AiToolsIpc.GetAiToolsOptInError("LIST_WINDOWS", IpcSources.Mcp));
        Assert.Null(AiToolsIpc.GetAiToolsOptInError("VERSION", IpcSources.Mcp));

        _settings.AiToolsEnabled = true;
        _settings.AiToolsAllowRecipeProposals = false;
        Assert.Equal(AiToolAccess.ProposalsDisabledMessage, AiToolsIpc.GetAiToolsOptInError("PROPOSE_RECIPE", IpcSources.Mcp));
        Assert.Null(AiToolsIpc.GetAiToolsOptInError("VALIDATE_RECIPE", IpcSources.Mcp));
    }

    [Theory]
    [InlineData("VERSION", false)]
    [InlineData("LIST_AI_TOOLS", false)]
    [InlineData("LIST_WINDOWS", true)]
    [InlineData("RUN_AI_TOOL", true)]
    [InlineData("PROPOSE_RECIPE", true)]
    public void Consent_IsNeededForEverythingButTheVersionAndTheToolNames(string command, bool required) =>
        Assert.Equal(required, AiToolsIpc.RequiresAiToolConsent(command, IpcSources.Mcp));

    #endregion

    #region Window ids

    private static WindowInfo Window(string id, int pid = 3000, string title = "Editor", string? appId = "org.gnome.TextEditor") =>
        new(title, appId, new LayoutRect(10, 10, 40, 20), pid, id);

    [Fact]
    public async Task WindowIds_BelongToTheAiToolAndItsSession()
    {
        var window = Window("17");
        AiWindowRefs.CurrentWindows = () => Task.FromResult<IReadOnlyList<WindowInfo>>([window]);
        string id = AiWindowRefs.Register(Client(), window);
        Assert.StartsWith("w", id);
        Assert.Equal(id, AiWindowRefs.Register(Client(), window));

        Assert.Equal(window, (await AiWindowRefs.ResolveAsync(Client(), id)).Window);
        Assert.Null((await AiWindowRefs.ResolveAsync(Client(server: 5000), id)).Window);
        Assert.Null((await AiWindowRefs.ResolveAsync(Client("/usr/bin/other"), id)).Window);
        Assert.Null((await AiWindowRefs.ResolveAsync(null, id)).Window);
        Assert.Contains("list_windows", (await AiWindowRefs.ResolveAsync(Client(), "17")).Error);
    }

    [Fact]
    public async Task WindowIds_Expire_AndStopWorkingWhenTheWindowIsGone()
    {
        var now = DateTime.UtcNow;
        AiWindowRefs.UtcNow = () => now;
        var window = Window("17");
        var windows = new List<WindowInfo> { window };
        AiWindowRefs.CurrentWindows = () => Task.FromResult<IReadOnlyList<WindowInfo>>(windows.ToList());
        string first = AiWindowRefs.Register(Client(), window);

        now += AiWindowRefs.Lifetime + TimeSpan.FromSeconds(1);
        Assert.Null((await AiWindowRefs.ResolveAsync(Client(), first)).Window);
        string second = AiWindowRefs.Register(Client(), window);
        Assert.NotEqual(first, second);

        // The compositor gave the id to a window of another process
        windows[0] = Window("17", pid: 3001);
        Assert.Contains("closed", (await AiWindowRefs.ResolveAsync(Client(), second)).Error);
    }

    #endregion

    #region Capturing

    private sealed class FakeCompositor(bool knowsWindows, params WindowInfo[] windows) : ICompositor
    {
        public string Name => knowsWindows ? "sway" : "GNOME";

        public bool KnowsWindows => knowsWindows;

        public Task<ScreenLayout?> GetLayoutAsync() =>
            Task.FromResult<ScreenLayout?>(new ScreenLayout([new ScreenInfo("DP-1", new LayoutRect(0, 0, 100, 50), 2, true)]));

        public Task<WindowInfo?> GetActiveWindowAsync() => Task.FromResult(windows.FirstOrDefault(w => w.Focused));

        public Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync() => Task.FromResult<IReadOnlyList<WindowInfo>>(windows);
    }

    private static ScreenSnapshot Snapshot()
    {
        var image = new SKBitmap(200, 100);
        image.Erase(SKColors.White);
        return new ScreenSnapshot(image, new ScreenLayout([new ScreenInfo("DP-1", new LayoutRect(0, 0, 100, 50), 2, true)]), DateTime.Now);
    }

    [Fact]
    public async Task Redaction_BlacksOutExcludedWindows_InImagePixels()
    {
        var vault = new WindowInfo("Passwords", "org.keepassxc.KeePassXC", new LayoutRect(10, 10, 20, 10), 0, "5");
        var editor = Window("6");
        using var snapshot = Snapshot();

        Assert.Equal(1, await AiToolCapture.RedactExcludedWindowsAsync(snapshot, new FakeCompositor(true, vault, editor)));
        Assert.Equal(SKColors.Black, snapshot.Image.GetPixel(20, 20));
        Assert.Equal(SKColors.Black, snapshot.Image.GetPixel(59, 39));
        Assert.Equal(SKColors.White, snapshot.Image.GetPixel(60, 40));
        Assert.Equal(SKColors.White, snapshot.Image.GetPixel(19, 19));
    }

    [Fact]
    public async Task Redaction_WithoutWindowInformation_RefusesWhileThereAreExclusions()
    {
        using var snapshot = Snapshot();
        await Assert.ThrowsAsync<CaptureException>(() => AiToolCapture.RedactExcludedWindowsAsync(snapshot, new FakeCompositor(false)));

        _settings.AiToolsExcludedProcesses = [];
        Assert.Equal(0, await AiToolCapture.RedactExcludedWindowsAsync(snapshot, new FakeCompositor(false)));
    }

    [Fact]
    public void Png_IsScaledToTheLongestSide()
    {
        using var image = new SKBitmap(3000, 1500);
        var png = AiToolCapture.EncodePng(image, 1568, out int width, out int height);
        Assert.Equal((1568, 784), (width, height));
        using var decoded = SKBitmap.Decode(png);
        Assert.Equal(1568, decoded.Width);

        AiToolCapture.EncodePng(image, 0, out width, out _);
        Assert.Equal(3000, width);
    }

    #endregion

    #region Commands

    private static async Task<JsonObject> DispatchAsync(string command, AiToolClient? client, IpcPeer? peer = null, string? recipe = null,
        Dictionary<string, string>? parameters = null)
    {
        var dispatcher = new IpcDispatcher(new FakeIpcApp());
        AiToolsIpc.Register(dispatcher);
        var envelope = new IpcEnvelope { Command = command, Source = IpcSources.Mcp, Recipe = recipe };
        foreach (var parameter in parameters ?? [])
        {
            envelope.Parameters[parameter.Key] = parameter.Value;
        }

        using var stream = new MemoryStream();
        await dispatcher.HandleAsync(new IpcRequestContext(envelope, stream, peer ?? IpcPeer.Unknown, false) { AiClient = client });
        stream.Position = 0;
        var frame = await IpcFraming.ReadFrameAsync(stream);
        return JsonNode.Parse(Encoding.UTF8.GetString(frame!))!.AsObject();
    }

    [Fact]
    public void Policy_AiCommandsAreOnlyForTheMcpSource()
    {
        AiToolsIpc.Register(new IpcDispatcher(new FakeIpcApp()));
        Assert.True(IpcPolicy.IsCommandAllowedForSource("RUN_AI_TOOL", IpcSources.Mcp));
        Assert.True(IpcPolicy.IsCommandAllowedForSource("VERSION", IpcSources.Mcp));
        Assert.False(IpcPolicy.IsCommandAllowedForSource("RUN_RECIPE", IpcSources.Mcp));
        Assert.False(IpcPolicy.IsCommandAllowedForSource("RUN_AI_TOOL", IpcSources.Cli));
        Assert.False(IpcPolicy.IsCommandAllowedForSource("LIST_WINDOWS", IpcSources.UrlScheme));
        // The terminal keeps its own commands
        Assert.True(IpcPolicy.IsCommandAllowedForSource("VERSION", IpcSources.Cli));
    }

    [Fact]
    public async Task Command_WhileSwitchedOff_IsRefused()
    {
        _settings.AiToolsEnabled = false;
        var reply = await DispatchAsync("LIST_WINDOWS", Client());
        Assert.Equal("error", reply.Value<string>("status"));
        Assert.Equal(AiToolAccess.DisabledMessage, reply.Value<string>("stderr"));
        Assert.Equal(0, _prompts);
    }

    [Fact]
    public async Task Command_FromAnUnidentifiedProcess_IsRefusedWithoutAsking()
    {
        var reply = await DispatchAsync("LIST_WINDOWS", null);
        Assert.StartsWith("[SECURITY]", reply.Value<string>("stderr"));
        Assert.Equal(0, _prompts);
    }

    [Fact]
    public async Task ListWindows_LeavesOutExcludedWindows()
    {
        _settings.AiToolsAllowedClients = ["/usr/bin/claude"];
        var vault = new WindowInfo("Passwords", "org.keepassxc.KeePassXC", new LayoutRect(10, 10, 20, 10), 0, "5");
        RecipeServices.Compositor = new FakeCompositor(true, vault, Window("6") with { Focused = true });

        var reply = await DispatchAsync("LIST_WINDOWS", Client());

        Assert.Equal("ok", reply.Value<string>("status"));
        var window = Assert.Single(reply["windows"]!.AsArray())!;
        Assert.Equal("Editor", window.Value<string>("title"));
        Assert.Equal("org.gnome.TextEditor", window.Value<string>("process"));
        Assert.True(window.Value<bool>("active"));
        Assert.Equal(1, reply.Value<int>("excluded_windows"));
        Assert.Single(reply["displays"]!.AsArray());
    }

    private void UseAiToolRecipes()
    {
        _settings.AiToolsAllowedClients = ["/usr/bin/claude"];
        foreach (var recipe in new AiToolRecipes().CreateRecipes())
        {
            RecipeManager.Instance.RegisterRecipe(recipe);
        }

        RecipeServices.Ui = InlineUiDispatcher.Instance;
        RecipeServices.Runner = new CaptureFlowRunner(CapturePipeline.Instance, InlineUiDispatcher.Instance);
    }

    [Fact]
    public async Task ListAiTools_NeedsNoConsent_AndListsTheBuiltInTools()
    {
        UseAiToolRecipes();
        _settings.AiToolsAllowedClients = [];

        var reply = await DispatchAsync("LIST_AI_TOOLS", Client());

        var names = reply["tools"]!.AsArray().Select(t => t.Value<string>("name")).ToList();
        Assert.Contains("capture_window", names);
        Assert.Contains("capture_region", names);
        Assert.Contains("capture_screen", names);
        Assert.Equal(0, _prompts);
    }

    [Fact]
    public async Task CaptureRegion_ReturnsThePng_AndTheTextWithOcr()
    {
        UseAiToolRecipes();
        using var scope = new HostScope();
        scope.Host.OcrLines = [new OcrLine("Hello", new SKRectI(1, 2, 31, 12), [])];

        var reply = await DispatchAsync("RUN_AI_TOOL", Client(), recipe: "capture_region",
            parameters: new() { ["region"] = "10,5,50,20", ["ocr"] = "true" });

        Assert.Equal("ok", reply.Value<string>("status"));
        Assert.Equal(50, reply["image"]!.Value<int>("width"));
        Assert.Equal(20, reply["image"]!.Value<int>("height"));
        Assert.NotEmpty(Convert.FromBase64String(reply["image"]!.Value<string>("data")));
        Assert.Equal("Hello", reply.Value<string>("text"));
        Assert.Equal(30, Assert.Single(reply["ocr_lines"]!.AsArray())!.Value<int>("width"));
    }

    [Fact]
    public async Task CaptureWindow_OnlyAcceptsCurrentWindowIds_AndNotExcludedWindows()
    {
        UseAiToolRecipes();
        using var scope = new HostScope();
        var editor = Window("6");
        Process(3100, 1, "/usr/bin/keepassxc");
        var vault = new WindowInfo("Passwords", null, new LayoutRect(0, 0, 30, 30), 3100, "7");
        AiWindowRefs.CurrentWindows = () => Task.FromResult<IReadOnlyList<WindowInfo>>([editor, vault]);
        string id = AiWindowRefs.Register(Client(), editor);

        var reply = await DispatchAsync("RUN_AI_TOOL", Client(), recipe: "capture_window", parameters: new() { ["window"] = id });
        Assert.Equal("ok", reply.Value<string>("status"));
        Assert.Equal(40, reply["image"]!.Value<int>("width"));

        reply = await DispatchAsync("RUN_AI_TOOL", Client(), recipe: "capture_window", parameters: new() { ["window"] = "0x1234" });
        Assert.Equal("error", reply.Value<string>("status"));
        Assert.Contains("list_windows", reply.Value<string>("stderr"));

        // An id of an excluded window can't be had from list_windows; even so it isn't captured
        string vaultId = AiWindowRefs.Register(Client(), vault);
        reply = await DispatchAsync("RUN_AI_TOOL", Client(), recipe: "capture_window", parameters: new() { ["window"] = vaultId });
        Assert.Equal("error", reply.Value<string>("status"));
        Assert.Contains("excluded", reply.Value<string>("stderr"));
    }

    [Fact]
    public async Task AiToolRecipes_CannotBeRunFromTheCommandLine()
    {
        UseAiToolRecipes();
        var envelope = new IpcEnvelope { Command = "RUN_RECIPE", Source = IpcSources.Cli, Recipe = RecipeManager.RecipeIdAiCaptureScreen };
        using var stream = new MemoryStream();
        await new IpcDispatcher(new FakeIpcApp()).HandleAsync(new IpcRequestContext(envelope, stream, IpcPeer.Unknown, false));
        stream.Position = 0;
        var reply = JsonNode.Parse(Encoding.UTF8.GetString((await IpcFraming.ReadFrameAsync(stream))!))!.AsObject();
        Assert.Equal("error", reply.Value<string>("status"));
    }

    #endregion

    #region Recipe proposals

    private const string ProposedRecipe = """
        {
          "version": "2.0",
          "id": "ai_proposed_clipboard",
          "name": "Proposed by an AI tool",
          "triggers": [ { "triggerType": "Manual", "name": "Manual", "enabled": true } ],
          "nodes": [ { "id": "s1", "stepType": "Source", "parameters": { "SourceType": "Clipboard" } } ]
        }
        """;

    private async Task<JsonObject> ProposeAsync(Func<Approval.RecipeApprovalRequest, Task<Approval.ApprovalResult>> answer)
    {
        _settings.AiToolsAllowedClients = ["/usr/bin/claude"];
        var previousPrompt = AiRecipeIpcHandler.ApprovalPrompt;
        var previousDirectory = AiRecipeIpcHandler.AiRecipeDirectoryProvider;
        AiRecipeIpcHandler.ApprovalPrompt = (request, _) => answer(request);
        AiRecipeIpcHandler.AiRecipeDirectoryProvider = () => Path.Combine(_folder, "Recipes", "AI");
        try
        {
            return await DispatchAsync("PROPOSE_RECIPE", Client(), parameters: new() { ["recipe"] = ProposedRecipe, ["explanation"] = "Because" });
        }
        finally
        {
            AiRecipeIpcHandler.ApprovalPrompt = previousPrompt;
            AiRecipeIpcHandler.AiRecipeDirectoryProvider = previousDirectory;
        }
    }

    [Fact]
    public async Task Catalog_ListsTheStepsAndTheRecipes()
    {
        _settings.AiToolsAllowedClients = ["/usr/bin/claude"];
        var reply = await DispatchAsync("RECIPE_CATALOG", Client());

        Assert.Equal("ok", reply.Value<string>("status"));
        var steps = reply["steps"]!.AsArray().Select(s => s.Value<string>("step_type")).ToList();
        Assert.Contains("Source", steps);
        Assert.Contains("Ocr", steps);
        Assert.NotEmpty(reply["trigger_types"]!.AsArray());
    }

    [Fact]
    public async Task Validate_ReportsErrors_WithoutSavingAnything()
    {
        _settings.AiToolsAllowedClients = ["/usr/bin/claude"];
        var reply = await DispatchAsync("VALIDATE_RECIPE", Client(), parameters: new() { ["recipe"] = ProposedRecipe });
        Assert.True(reply.Value<bool>("valid"), reply.ToJsonString());

        reply = await DispatchAsync("VALIDATE_RECIPE", Client(), parameters: new() { ["recipe"] = "{ not json" });
        Assert.Equal("error", reply.Value<string>("status"));
        Assert.False(Directory.Exists(Path.Combine(_folder, "Recipes")));
    }

    [Fact]
    public async Task Proposal_Rejected_IsNotShownAgain()
    {
        AiRecipeIpcHandler.ResetRejectedProposals();
        int shown = 0;
        var reply = await ProposeAsync(request =>
        {
            shown++;
            Assert.Equal("claude", request.ProposedByName);
            Assert.Equal("Because", request.AiExplanation);
            Assert.True(request.StartSwitchedOff);
            return Task.FromResult(new Approval.ApprovalResult(null, false));
        });
        Assert.Equal("rejected", reply.Value<string>("decision"));

        reply = await ProposeAsync(_ => throw new InvalidOperationException("asked again"));
        Assert.Equal("error", reply.Value<string>("status"));
        Assert.Contains("already rejected", reply.Value<string>("stderr"));
        Assert.Equal(1, shown);
        Assert.Null(RecipeManager.Instance.GetRecipeById("ai_proposed_clipboard"));
    }

    [Fact]
    public async Task Proposal_Approved_IsSavedAndLoaded()
    {
        AiRecipeIpcHandler.ResetRejectedProposals();
        var reply = await ProposeAsync(_ => Task.FromResult(new Approval.ApprovalResult(new Approval.RecipeApproval(), false)));

        Assert.Equal("approved", reply.Value<string>("decision"));
        string file = reply.Value<string>("file");
        Assert.StartsWith(Path.Combine(_folder, "Recipes", "AI"), file);
        Assert.True(File.Exists(file));
        Assert.NotNull(RecipeManager.Instance.GetRecipeById("ai_proposed_clipboard"));
        RecipeManager.Instance.UnregisterRecipe("ai_proposed_clipboard");
    }

    [Fact]
    public async Task Proposal_WhileSwitchedOff_IsRefused()
    {
        _settings.AiToolsAllowRecipeProposals = false;
        var reply = await ProposeAsync(_ => throw new InvalidOperationException("shown"));
        Assert.Equal(AiToolAccess.ProposalsDisabledMessage, reply.Value<string>("stderr"));
    }

    #endregion
}
