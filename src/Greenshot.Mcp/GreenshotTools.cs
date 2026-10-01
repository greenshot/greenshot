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
using System.ComponentModel;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Greenshot.Mcp
{
    /// <summary>
    /// Source generated JSON metadata for the types which are not part of the MCP defaults (Native AOT).
    /// </summary>
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal sealed partial class GreenshotMcpJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// The built-in tool: list_windows. Everything else (capturing included) are the recipes the user offers to AI tools,
    /// see <see cref="RecipeTool"/>.
    /// </summary>
    [McpServerToolType]
    public sealed class GreenshotTools
    {
        [McpServerTool(Name = "list_windows", Title = "List windows", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the open top-level windows (in Z-order, topmost first) with an id, title, process, class, bounds in screen coordinates, " +
                     "minimized and active state, and the displays with their bounds. Pass a window id (e.g. w7) to tools with a window argument, " +
                     "such as capture_window. Ids only work in this session and expire after some minutes, then call list_windows again.")]
        public static async Task<CallToolResult> ListWindowsAsync(McpServer server, CancellationToken cancellationToken)
        {
            var request = new JsonObject { ["command"] = "LIST_WINDOWS" };
            var result = await ToolCalls.CallAsync(server, request, reply => ToolCalls.TextResult(ToolCalls.Without(reply, "stdout", "status", "exit_code")), cancellationToken).ConfigureAwait(false);
            // Greenshot may have been started by this call: get its tools now
            RecipeToolSync.RequestSync();
            return result;
        }
    }

    /// <summary>
    /// Calling Greenshot and turning its replies into tool results.
    /// </summary>
    internal static class ToolCalls
    {
        public static async Task<CallToolResult> CallAsync(McpServer server, JsonObject request, Func<JsonObject, CallToolResult> onSuccess, CancellationToken cancellationToken)
        {
            JsonObject reply;
            try
            {
                reply = await GreenshotConnection.SendAsync(request, GetClientName(server), cancellationToken).ConfigureAwait(false);
            }
            catch (GreenshotConnectionException ex)
            {
                return ErrorResult(ex.Message);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return ErrorResult($"Could not talk to Greenshot: {ex.Message}");
            }

            if (!string.Equals((string?)reply["status"], "ok", StringComparison.OrdinalIgnoreCase))
            {
                string message = (string?)reply["stderr"] ?? (string?)reply["streamed_stderr"] ?? "Greenshot reported an error.";
                return ErrorResult(message);
            }
            return onSuccess(reply);
        }

        /// <summary>
        /// The result of a recipe: its image (when it produced one) and the rest of the reply as JSON text.
        /// </summary>
        public static CallToolResult RecipeResult(JsonObject reply)
        {
            var content = new List<ContentBlock>();
            if (reply["image"] is JsonObject image && (string?)image["data"] is { Length: > 0 } data)
            {
                content.Add(ImageContentBlock.FromBytes(Convert.FromBase64String(data), (string?)image["mime_type"] ?? "image/png"));
                image.Remove("data");
            }
            var details = Without(reply, "status", "exit_code");
            foreach (var pair in reply)
            {
                // Leave out what is empty
                if (pair.Value == null || pair.Value is JsonObject { Count: 0 } || pair.Value is JsonArray { Count: 0 })
                {
                    details.Remove(pair.Key);
                }
            }
            content.Add(new TextContentBlock { Text = details.ToJsonString() });
            return new CallToolResult { Content = content };
        }

        public static string? GetClientName(McpServer server)
        {
            var clientInfo = server.ClientInfo;
            if (clientInfo == null)
            {
                return null;
            }
            string name = string.IsNullOrWhiteSpace(clientInfo.Title) ? clientInfo.Name : clientInfo.Title!;
            return string.IsNullOrWhiteSpace(clientInfo.Version) ? name : $"{name} {clientInfo.Version}";
        }

        public static JsonObject Without(JsonObject reply, params string[] names)
        {
            var copy = (JsonObject)reply.DeepClone();
            foreach (var name in names)
            {
                copy.Remove(name);
            }
            return copy;
        }

        public static CallToolResult TextResult(JsonObject reply)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = reply.ToJsonString() }]
            };
        }

        public static CallToolResult ErrorResult(string message)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = message }]
            };
        }
    }
}
