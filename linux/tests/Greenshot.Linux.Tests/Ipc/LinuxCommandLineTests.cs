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

#nullable disable

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Recipes.Ipc;

namespace Greenshot.Linux.Tests.Ipc;

/// <summary>
/// The command line of the Linux version next to the one of greenshot-cli: its words, links and what each source may do
/// </summary>
[Collection(TestCollections.RecipeManager)]
public sealed class LinuxCommandLineTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gs-cli-" + Guid.NewGuid().ToString("N")[..8]);

    public LinuxCommandLineTests()
    {
        TestEnvironment.EnsureInitialized();
    }

    [Theory]
    [InlineData("capture region", new[] { "capture", "region" })]
    [InlineData("capture fullscreen", new[] { "capture", "fullscreen" })]
    [InlineData("settings", new[] { "settings" })]
    [InlineData("recipe-editor recipe_region", new[] { "recipe-editor", "recipe_region" })]
    [InlineData("recipe recipe_region", new[] { "recipe", "recipe_region" })]
    public void The_words_of_the_Linux_version_are_app_commands(string wire, string[] argv)
    {
        var result = CliCommandParser.Parse(argv, IpcSources.Cli, "/tmp");
        Assert.True(result.Success, result.Error);
        Assert.Equal(CliCommandParser.AppCommandName, result.Envelope.Command);
        Assert.Equal(wire, result.Envelope.RawInput);
    }

    [Fact]
    public void Dashes_are_the_options_of_greenshot_cli()
    {
        // "recipe <id>" starts any recipe (for key bindings), "-r <id>" runs one with a command line trigger and waits
        Assert.Equal("RUN_RECIPE", CliCommandParser.Parse(["-r", "ocr"], IpcSources.Cli, "/tmp").Envelope.Command);
        Assert.Equal("RUN_RECIPE", CliCommandParser.Parse(["--recipe", "ocr"], IpcSources.Cli, "/tmp").Envelope.Command);
        Assert.Null(AppCommand.Parse(["-r", "ocr"]));
        Assert.Null(AppCommand.Parse(["--list-recipes"]));
        Assert.Equal(AppCommandKind.Exit, AppCommand.Parse(["--exit"]).Kind);
    }

    [Fact]
    public void Files_are_relative_to_the_folder_of_the_caller()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllBytes(Path.Combine(_folder, "my shot.png"), [0]);

        var open = CliCommandParser.Parse(["open", "my shot.png"], IpcSources.Cli, _folder);
        Assert.Equal("open " + Path.Combine(_folder, "my shot.png"), open.Envelope.RawInput);

        var bare = CliCommandParser.Parse(["my shot.png"], IpcSources.Cli, _folder);
        Assert.Equal(CliCommandParser.AppCommandName, bare.Envelope.Command);
        Assert.Equal("open " + Path.Combine(_folder, "my shot.png"), bare.Envelope.RawInput);
    }

    [Theory]
    [InlineData("greenshot://recipe/ocr?lang=deu", "RUN_RECIPE")]
    [InlineData("greenshot:settings", "SETTINGS")]
    [InlineData("greenshot://recipe-editor", "RECIPE_EDITOR")]
    [InlineData("greenshot://exit", "EXIT")]
    [InlineData("greenshot://cli", null)]
    [InlineData("https://example.com", null)]
    public void Links_are_parsed(string url, string command)
    {
        Assert.Equal(command, IpcPolicy.ParseUrlSchemeCommand(url, out var parameters));
        if (command == "RUN_RECIPE")
        {
            Assert.Equal("ocr", parameters["recipe"]);
            Assert.Equal("deu", parameters["lang"]);
        }
    }

    [Theory]
    [InlineData("EXIT", IpcSources.Cli, true)]
    [InlineData("EXIT", IpcSources.UrlScheme, false)]
    [InlineData("RELOAD_CONFIG", IpcSources.UrlScheme, false)]
    [InlineData("SETTINGS", IpcSources.UrlScheme, true)]
    [InlineData("OPEN_FILE", IpcSources.OpenWith, true)]
    [InlineData("RUN_RECIPE", IpcSources.OpenWith, false)]
    [InlineData("APP", IpcSources.UrlScheme, false)]
    [InlineData("LIST_RECIPES", IpcSources.Mcp, false)]
    [InlineData("SHELL", IpcSources.Cli, false)]
    public void Each_source_may_do_only_its_commands(string command, string source, bool allowed)
    {
        Assert.Equal(allowed, IpcPolicy.IsCommandAllowedForSource(command, source));
    }

    [Fact]
    public async Task The_app_runs_the_words_and_the_links_open_windows()
    {
        var app = new FakeIpcApp();
        var dispatcher = new IpcDispatcher(app);
        foreach (var argv in new[] { new[] { "capture", "region" }, ["greenshot://recipe-manager"], ["--reload"], ["--exit"] })
        {
            using var stream = new MemoryStream();
            var context = new IpcRequestContext(IpcEnvelope.CreateCli(argv, IpcSources.Cli, "/tmp"), stream, IpcPeer.Unknown, true);
            await dispatcher.HandleAsync(context);
            Assert.True(context.IsReplyCompleted);
        }

        Assert.Equal(["run capture region", "manager", "reload", "exit"], app.Calls);
    }

    [Fact]
    public async Task A_link_can_not_reach_exit()
    {
        var app = new FakeIpcApp();
        using var stream = new MemoryStream();
        var context = new IpcRequestContext(IpcEnvelope.CreateCli(["greenshot://exit"], IpcSources.UrlScheme, null), stream, IpcPeer.Unknown, true);
        await new IpcDispatcher(app).HandleAsync(context);
        Assert.Empty(app.Calls);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
