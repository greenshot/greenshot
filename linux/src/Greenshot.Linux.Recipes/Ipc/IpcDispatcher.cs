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

using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Ipc;

namespace Greenshot.Linux.Recipes.Ipc;

/// <summary>
/// What the running app does for requests: its windows and the words of the Linux version. The methods are called on
/// any thread, the app moves them to its UI thread.
/// </summary>
public interface IIpcApp
{
    /// <summary>
    /// Completes when the recipes are loaded
    /// </summary>
    Task Ready { get; }

    string Version { get; }

    /// <summary>
    /// Run a command of the Linux version (capture, open, recipe, ...), without waiting for it
    /// </summary>
    void Run(AppCommand command);

    Task ShowSettingsAsync();

    Task ShowAboutAsync();

    Task ShowLogAsync();

    Task OpenRecipeEditorAsync(string? recipeId);

    Task OpenRecipeManagerAsync();

    Task ReloadConfigurationAsync();

    void Exit();
}

/// <summary>
/// The requests to the running Greenshot (the IpcSecurityDispatcher of Windows): a command line is parsed according to
/// its source, every command is checked against the whitelist of the source, then run
/// </summary>
public sealed class IpcDispatcher(IIpcApp app)
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(IpcDispatcher));

    /// <summary>
    /// Commands of other parts (the AI tools), by name
    /// </summary>
    public Dictionary<string, Func<IpcRequestContext, Task>> Extensions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public async Task HandleAsync(IpcRequestContext context)
    {
        // A command line which started Greenshot comes in while it starts
        await Task.WhenAny(app.Ready, Task.Delay(TimeSpan.FromSeconds(60)));
        var envelope = context.Envelope;
        string? command = envelope.Command?.Trim().ToUpperInvariant();
        if (!IpcPolicy.IsCommandAllowedForSource(command, envelope.Source))
        {
            Log.Warn($"[SECURITY] Command '{command}' is not allowed from source '{envelope.Source}'");
            await RecipeCommands.ErrorAsync(context, $"[SECURITY] Command '{command}' is not allowed from source '{envelope.Source}'.");
            return;
        }

        try
        {
            switch (command)
            {
                case IpcSources.CliCommand:
                    await HandleCommandLineAsync(context);
                    break;
                case CliCommandParser.AppCommandName:
                    await HandleAppCommandAsync(context);
                    break;
                case "LIST_RECIPES":
                    await RecipeCommands.ListRecipesAsync(context);
                    break;
                case "DESCRIBE_RECIPE":
                    await RecipeCommands.DescribeRecipeAsync(context);
                    break;
                case "RUN_RECIPE":
                    await RecipeCommands.RunRecipeAsync(context);
                    break;
                case "OPEN_FILE":
                    await HandleOpenFilesAsync(context);
                    break;
                case "URL_SCHEME":
                    await HandleUrlAsync(context);
                    break;
                case "VERSION":
                    await context.ReplyAsync(new { status = "ok", exit_code = 0, version = app.Version, stdout = $"Greenshot version {app.Version}" });
                    break;
                case "SETTINGS":
                    await DoneAsync(context, app.ShowSettingsAsync());
                    break;
                case "ABOUT":
                    await DoneAsync(context, app.ShowAboutAsync());
                    break;
                case "LOG_VIEWER":
                    await DoneAsync(context, app.ShowLogAsync());
                    break;
                case "RECIPE_EDITOR":
                    envelope.Parameters.TryGetValue("recipe", out var recipeId);
                    await DoneAsync(context, app.OpenRecipeEditorAsync(recipeId ?? envelope.Recipe));
                    break;
                case "RECIPE_MANAGER":
                    await DoneAsync(context, app.OpenRecipeManagerAsync());
                    break;
                case "RELOAD_CONFIG":
                    await DoneAsync(context, app.ReloadConfigurationAsync(), "Configuration reloaded.");
                    break;
                case "EXIT":
                    // The answer first, the connection ends with Greenshot
                    await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout = "Greenshot is exiting." });
                    app.Exit();
                    break;
                default:
                    if (command != null && Extensions.TryGetValue(command, out var extension))
                    {
                        await extension(context);
                    }
                    else
                    {
                        await RecipeCommands.ErrorAsync(context, $"Unknown command '{command}'.");
                    }

                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"The request '{command}' failed", ex);
            if (!context.IsReplyCompleted)
            {
                await RecipeCommands.ErrorAsync(context, $"Error: {ex.Message}");
            }
        }

        // A command without an answer still ends the terminal's wait
        await context.CompleteAsync();
    }

    private async Task HandleCommandLineAsync(IpcRequestContext context)
    {
        var result = CliCommandParser.Parse(context.Envelope.Argv ?? [], context.Envelope.Source, context.Envelope.Cwd);
        if (!result.Success)
        {
            Log.Info($"A command line is rejected: {result.Error}");
            context.Envelope.Json = result.Json;
            await context.ReplyAsync(new { status = "error", exit_code = CliCommandParser.UsageExitCode, stderr = result.Error });
            return;
        }

        result.Envelope.Source = context.Envelope.Source;
        await HandleAsync(context.WithEnvelope(result.Envelope));
    }

    /// <summary>
    /// The words of the Linux version (capture region, open, recipe, ...): done by the app, the terminal doesn't wait for it
    /// </summary>
    private async Task HandleAppCommandAsync(IpcRequestContext context)
    {
        var command = AppCommand.ParseWire(context.Envelope.RawInput ?? string.Empty);
        if (command == null)
        {
            await RecipeCommands.ErrorAsync(context, $"Unknown command '{context.Envelope.RawInput}'.", CliCommandParser.UsageExitCode);
            return;
        }

        // Answered first: "exit" ends the connection with Greenshot
        await context.ReplyAsync(new { status = "ok", exit_code = 0 });
        app.Run(command);
    }

    private async Task HandleOpenFilesAsync(IpcRequestContext context)
    {
        var files = new List<string>();
        foreach (var raw in context.Envelope.Files)
        {
            string candidate = raw.StartsWith("file://", StringComparison.Ordinal) ? new Uri(raw).LocalPath : raw;
            if (!PathSanitizer.TrySanitizeAndResolve(candidate, context.Envelope.Cwd, context.Envelope.Source, out var path, out var error))
            {
                await RecipeCommands.ErrorAsync(context, $"[SECURITY] Error: invalid path '{raw}': {error}");
                return;
            }

            if (!File.Exists(path))
            {
                await RecipeCommands.ErrorAsync(context, $"Error: the file '{raw}' doesn't exist.");
                return;
            }

            files.Add(path!);
        }

        foreach (var file in files)
        {
            app.Run(new AppCommand(AppCommandKind.Open, Path: file));
        }
        await context.ReplyAsync(new { status = "ok", exit_code = 0 });
    }

    /// <summary>
    /// A greenshot: link, as command of the source which sent it (a link from a web page only does what links may)
    /// </summary>
    private async Task HandleUrlAsync(IpcRequestContext context)
    {
        string? command = IpcPolicy.ParseUrlSchemeCommand(context.Envelope.RawInput, out var parameters);
        if (command == null)
        {
            await RecipeCommands.ErrorAsync(context, $"Error: '{context.Envelope.RawInput}' is not a Greenshot link.", CliCommandParser.UsageExitCode);
            return;
        }

        var envelope = new IpcEnvelope { Command = command, Source = context.Envelope.Source, Cwd = context.Envelope.Cwd, Json = context.Envelope.Json };
        if (parameters.Remove("recipe", out var recipe))
        {
            envelope.Recipe = recipe;
            envelope.Parameters["recipe"] = recipe;
        }

        foreach (var parameter in parameters)
        {
            envelope.Parameters[parameter.Key] = parameter.Value;
        }

        if (command == "RUN_RECIPE")
        {
            envelope.Parameters.Remove("recipe");
        }

        await HandleAsync(context.WithEnvelope(envelope));
    }

    private static async Task DoneAsync(IpcRequestContext context, Task action, string? stdout = null)
    {
        await action.ConfigureAwait(false);
        await context.ReplyAsync(new { status = "ok", exit_code = 0, stdout }).ConfigureAwait(false);
    }
}
