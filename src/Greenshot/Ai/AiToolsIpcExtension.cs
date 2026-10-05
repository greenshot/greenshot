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
using System.Threading.Tasks;
using Greenshot.Ai;
using Greenshot.Helpers;
using Greenshot.Ipc;
using log4net;
using Greenshot.Base.Interfaces;

[assembly: GreenshotModule(typeof(AiToolsIpcExtension), 20)]

namespace Greenshot.Ai
{
    /// <summary>
    /// The IPC commands of AI tools (greenshot-mcp.exe): only for the "mcp" source, opt-in, and except LIST_AI_TOOLS
    /// they need the user's consent for the AI tool (see <see cref="AiToolAccess"/>).
    /// </summary>
    internal sealed class AiToolsIpcExtension : IIpcCommandExtension
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolsIpcExtension));

        /// <summary>
        /// The commands of AI tools: only allowed for the "mcp" source
        /// </summary>
        internal static readonly HashSet<string> AiToolCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LIST_WINDOWS",
            "LIST_AI_TOOLS",
            "RUN_AI_TOOL",
            "RECIPE_CATALOG",
            "VALIDATE_RECIPE",
            "PROPOSE_RECIPE"
        };

        public IEnumerable<string> Commands => AiToolCommands;

        public IReadOnlyDictionary<string, IEnumerable<string>> SourceCommands { get; } = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // greenshot-mcp.exe: what an AI tool may do, after the user allowed it. Everything it captures goes through a recipe
            // with an AI tool trigger (RUN_AI_TOOL), not through the command line recipes (RUN_RECIPE).
            [IpcSources.Mcp] = new[]
            {
                "VERSION",
                "LIST_WINDOWS",
                "LIST_AI_TOOLS",
                "RUN_AI_TOOL",
                "RECIPE_CATALOG",
                "VALIDATE_RECIPE",
                "PROPOSE_RECIPE"
            }
        };

        /// <summary>
        /// AI tool commands only for greenshot-mcp.exe: not for the command line, a web page or the browser extension
        /// </summary>
        public bool IsAllowedForSource(string command, string source)
        {
            return !AiToolCommands.Contains(command) || string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> CheckAccessAsync(string command, IpcRequestContext context)
        {
            string source = context.Envelope.Source;
            // AI tools are opt-in: switched off, greenshot-mcp gets nothing but its version, and nobody is asked
            string optInError = GetAiToolsOptInError(command, source);
            if (optInError != null)
            {
                Log.Warn($"[SECURITY] IPC command rejected: '{command}' from source '{source}': {optInError}");
                return optInError;
            }

            // AI tools (and anything reading the screen contents) need the user's consent
            if (RequiresAiToolConsent(command, source) &&
                !await AiToolAccess.EnsureAllowedAsync(context.AiClient).ConfigureAwait(false))
            {
                Log.Warn($"[SECURITY] IPC command rejected: '{command}' from source '{source}', the user did not allow {context.AiClient?.ToString() ?? "an unidentified program"}.");
                return AiToolAccess.NotAllowedMessage;
            }
            return null;
        }

        /// <summary>
        /// Why an AI tool request is refused by the opt-in switches, null when it isn't. Only greenshot-mcp's own version
        /// passes while AI tools are switched off.
        /// </summary>
        internal static string GetAiToolsOptInError(string command, string source)
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
        /// True when the command needs the user's consent for AI tools: every command from greenshot-mcp.exe except VERSION and
        /// LIST_AI_TOOLS (the tool names and descriptions, so the AI tool can show its tools before the user is asked),
        /// and the AI tool commands from any source. The recipe commands (catalog, validate, propose) need the consent too; a
        /// proposed recipe additionally needs the user's approval in the recipe approval window.
        /// </summary>
        internal static bool RequiresAiToolConsent(string command, string source)
        {
            if (string.Equals(command, "VERSION", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "LIST_AI_TOOLS", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return AiToolCommands.Contains(command) || string.Equals(source, IpcSources.Mcp, StringComparison.OrdinalIgnoreCase);
        }

        public Task HandleAsync(string command, IpcRequestContext context, IGreenshotShell shell)
        {
            switch (command.ToUpperInvariant())
            {
                case "LIST_WINDOWS":
                    return AiToolIpcHandler.HandleListWindowsAsync(context);
                case "LIST_AI_TOOLS":
                    return AiToolIpcHandler.HandleListAiToolsAsync(context);
                case "RUN_AI_TOOL":
                    return AiToolIpcHandler.HandleRunAiToolAsync(context);
                case "RECIPE_CATALOG":
                    return AiRecipeIpcHandler.HandleRecipeCatalogAsync(context);
                case "VALIDATE_RECIPE":
                    return AiRecipeIpcHandler.HandleValidateRecipeAsync(context);
                case "PROPOSE_RECIPE":
                    return AiRecipeIpcHandler.HandleProposeRecipeAsync(context);
                default:
                    Log.Warn($"Unhandled AI tool command: {command}");
                    return Task.CompletedTask;
            }
        }
    }
}
