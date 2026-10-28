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

using Greenshot.Linux.Core.Ipc;
using Greenshot.Linux.Recipes.Ipc;

namespace Greenshot.Linux.Recipes.Ai;

/// <summary>
/// The IPC commands of AI tools (greenshot-mcp): only for the "mcp" source, opt-in, and except LIST_AI_TOOLS they need the
/// user's consent for the AI tool (see <see cref="AiToolAccess"/>). The AiToolsIpcExtension of Windows.
/// </summary>
public static class AiToolsIpc
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolsIpc));

    /// <summary>
    /// The commands of AI tools: only allowed for the "mcp" source
    /// </summary>
    public static readonly IReadOnlySet<string> AiToolCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "LIST_WINDOWS",
        "LIST_AI_TOOLS",
        "RUN_AI_TOOL",
        "RECIPE_CATALOG",
        "VALIDATE_RECIPE",
        "PROPOSE_RECIPE",
    };

    /// <summary>
    /// Where greenshot-mcp must be: Greenshot's own folder, replaceable for tests
    /// </summary>
    internal static Func<string> GreenshotDirectory { get; set; } = () => AppContext.BaseDirectory;

    /// <summary>
    /// greenshot-mcp may do what an AI tool may do, after the user allowed it. Everything it captures goes through a recipe
    /// with an AI tool trigger (RUN_AI_TOOL), not through the command line recipes (RUN_RECIPE).
    /// </summary>
    public static void Register(IpcDispatcher dispatcher)
    {
        IpcPolicy.AddSourceCommands(IpcSources.Mcp, AiToolCommands.Append("VERSION"));
        // A Window argument is an id from list_windows; resolved on a request thread, the compositor answers over its socket
        CommandlineArgumentBinder.WindowResolver = (client, raw) =>
        {
            var (window, error) = AiWindowRefs.ResolveAsync(client as AiToolClient, raw).GetAwaiter().GetResult();
            return (window != null, window, error);
        };
        Add(dispatcher, "LIST_WINDOWS", c => AiToolIpcHandler.HandleListWindowsAsync(c));
        Add(dispatcher, "LIST_AI_TOOLS", c => AiToolIpcHandler.HandleListAiToolsAsync(c));
        Add(dispatcher, "RUN_AI_TOOL", c => AiToolIpcHandler.HandleRunAiToolAsync(c));
        Add(dispatcher, "RECIPE_CATALOG", c => AiRecipeIpcHandler.HandleRecipeCatalogAsync(c));
        Add(dispatcher, "VALIDATE_RECIPE", c => AiRecipeIpcHandler.HandleValidateRecipeAsync(c));
        Add(dispatcher, "PROPOSE_RECIPE", c => AiRecipeIpcHandler.HandleProposeRecipeAsync(c));
    }

    private static void Add(IpcDispatcher dispatcher, string command, Func<IpcRequestContext, Task> handler) =>
        dispatcher.Extensions[command] = async context =>
        {
            string? error = await CheckAccessAsync(command, context).ConfigureAwait(false);
            if (error != null)
            {
                await RecipeCommands.ErrorAsync(context, error).ConfigureAwait(false);
                return;
            }

            await handler(context).ConfigureAwait(false);
        };

    /// <summary>
    /// Why the request is refused, null when it may run: the source, the opt-in switches, the program on the other end and the
    /// user's consent
    /// </summary>
    public static async Task<string?> CheckAccessAsync(string command, IpcRequestContext context)
    {
        string? source = context.Envelope.Source;
        if (!string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase))
        {
            Log.Warn($"[SECURITY] AI tool command '{command}' rejected from source '{source}'.");
            return $"[SECURITY] Command '{command}' is only for AI tools.";
        }

        // AI tools are opt-in: switched off, greenshot-mcp gets nothing but its version, and nobody is asked
        string? optInError = GetAiToolsOptInError(command, source);
        if (optInError != null)
        {
            Log.Warn($"[SECURITY] IPC command rejected: '{command}' from source '{source}': {optInError}");
            return optInError;
        }

        // Who is on the other end is decided by Greenshot, once per connection
        if (context.AiClient is not AiToolClient)
        {
            if (!AiToolCaller.TryIdentify(context.Peer.ProcessId, GreenshotDirectory(), AdditionalServerPaths(), out var client, out var identifyError))
            {
                Log.Warn($"[SECURITY] IPC command rejected: '{command}': {identifyError}");
                return "[SECURITY] " + identifyError;
            }

            Log.Info($"The AI tool on the connection of process {context.Peer.ProcessId} is {client}.");
            context.AiClient = client;
        }

        if (RequiresAiToolConsent(command, source) && !await AiToolAccess.EnsureAllowedAsync(context.AiClient as AiToolClient).ConfigureAwait(false))
        {
            Log.Warn($"[SECURITY] IPC command rejected: '{command}' from source '{source}', the user did not allow {context.AiClient}.");
            return AiToolAccess.NotAllowedMessage;
        }

        return null;
    }

    /// <summary>
    /// AiToolsMcpServerPaths, for development builds only: in a release build greenshot.ini must not be able to add a server location
    /// </summary>
    private static IEnumerable<string>? AdditionalServerPaths()
    {
#if DEBUG
        return RecipeServices.Settings?.AiToolsMcpServerPaths;
#else
        return null;
#endif
    }

    /// <summary>
    /// Why an AI tool request is refused by the opt-in switches, null when it isn't. Only greenshot-mcp's own version
    /// passes while AI tools are switched off.
    /// </summary>
    internal static string? GetAiToolsOptInError(string command, string? source)
    {
        if (!string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!AiToolAccess.IsEnabled)
        {
            return string.Equals(command, "VERSION", StringComparison.OrdinalIgnoreCase) ? null : AiToolAccess.DisabledMessage;
        }

        if (string.Equals(command, "PROPOSE_RECIPE", StringComparison.OrdinalIgnoreCase) && !AiToolAccess.AreRecipeProposalsAllowed)
        {
            return AiToolAccess.ProposalsDisabledMessage;
        }

        return null;
    }

    /// <summary>
    /// True when the command needs the user's consent: everything from greenshot-mcp except VERSION and LIST_AI_TOOLS (the tool
    /// names and descriptions, so the AI tool can show its tools before the user is asked). A proposed recipe additionally needs
    /// the user's approval in the recipe approval window.
    /// </summary>
    internal static bool RequiresAiToolConsent(string command, string? source)
    {
        if (string.Equals(command, "VERSION", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "LIST_AI_TOOLS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return AiToolCommands.Contains(command) || string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase);
    }
}
