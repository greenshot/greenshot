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
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Greenshot.Mcp
{
    /// <summary>
    /// Keeps the tools in sync with the recipes the user offers to AI tools (recipes with an AI tool trigger): asks Greenshot
    /// at the start, after each tool call and every <see cref="Interval"/>. Adding or removing a tool makes the MCP server send
    /// notifications/tools/list_changed, so the AI tool sees new or changed recipes without a restart.
    /// Background requests never start Greenshot, list_windows and the recipe tools do.
    /// </summary>
    public sealed class RecipeToolSync : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
        private static readonly SemaphoreSlim SyncRequested = new SemaphoreSlim(0, 1);

        private readonly McpServerPrimitiveCollection<McpServerTool>? _tools;
        private readonly ILogger<RecipeToolSync> _logger;

        public RecipeToolSync(IOptions<McpServerOptions> options, ILogger<RecipeToolSync> logger)
        {
            _tools = options.Value.ToolCollection;
            _logger = logger;
        }

        /// <summary>
        /// Sync soon (e.g. Greenshot was just started by a tool call)
        /// </summary>
        public static void RequestSync()
        {
            try
            {
                SyncRequested.Release();
            }
            catch (SemaphoreFullException)
            {
                // Already requested
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_tools == null)
            {
                _logger.LogWarning("There is no tool collection, the recipe tools are not available.");
                return;
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                await SyncAsync(stoppingToken).ConfigureAwait(false);
                try
                {
                    await SyncRequested.WaitAsync(Interval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SyncAsync(CancellationToken cancellationToken)
        {
            JsonObject reply;
            try
            {
                reply = await GreenshotConnection.SendAsync(new JsonObject { ["command"] = "LIST_AI_TOOLS" }, null, cancellationToken, startGreenshot: false).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is GreenshotConnectionException || ex is IOException || ex is UnauthorizedAccessException || ex is TimeoutException)
            {
                // Greenshot isn't running: keep the tools, calling one starts Greenshot
                _logger.LogDebug(ex, "Could not get the AI tools from Greenshot.");
                return;
            }

            if (!string.Equals((string?)reply["status"], "ok", StringComparison.OrdinalIgnoreCase) || reply["tools"] is not JsonArray entries)
            {
                _logger.LogWarning("Greenshot did not list its AI tools: {Error}", (string?)reply["stderr"]);
                return;
            }

            var wanted = new Dictionary<string, RecipeTool>(StringComparer.Ordinal);
            foreach (var entry in entries.OfType<JsonObject>())
            {
                var tool = RecipeTool.Create(entry);
                if (tool != null)
                {
                    wanted.TryAdd(tool.ProtocolTool.Name, tool);
                }
            }
            Apply(_tools!, wanted);
        }

        /// <summary>
        /// Replaces the recipe tools in the collection with the wanted ones; unchanged tools stay, so nothing is sent when nothing changed.
        /// </summary>
        internal static bool Apply(McpServerPrimitiveCollection<McpServerTool> tools, IReadOnlyDictionary<string, RecipeTool> wanted)
        {
            var current = tools.OfType<RecipeTool>().ToList();
            var toRemove = current.Where(t => !wanted.TryGetValue(t.ProtocolTool.Name, out var w) || w.Signature != t.Signature).ToList();
            var toAdd = wanted.Values.Where(w => !current.Any(t => t.ProtocolTool.Name == w.ProtocolTool.Name && t.Signature == w.Signature)).ToList();
            if (toRemove.Count == 0 && toAdd.Count == 0)
            {
                return false;
            }

            // One list_changed notification for all changes
            using (tools.DeferChangedEvents())
            {
                foreach (var tool in toRemove)
                {
                    tools.Remove(tool);
                }
                foreach (var tool in toAdd)
                {
                    // A built-in tool (list_windows) keeps its name
                    tools.TryAdd(tool);
                }
            }
            return true;
        }
    }
}
