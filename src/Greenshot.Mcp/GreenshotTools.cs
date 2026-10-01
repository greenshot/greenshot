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
using System.Globalization;
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
    /// Source generated JSON metadata for the tool parameters which are not part of the MCP defaults (Native AOT).
    /// </summary>
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal sealed partial class GreenshotMcpJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// The MCP tools; each call is one request to Greenshot over the named pipe.
    /// </summary>
    [McpServerToolType]
    public sealed class GreenshotTools
    {
        /// <summary>
        /// Default for max_size: the longest image side most AI models work with, larger images are scaled down by them anyway.
        /// </summary>
        private const int DefaultMaxSize = 1568;

        [McpServerTool(Name = "list_windows", Title = "List windows", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the open top-level windows (in Z-order, topmost first) with handle, title, process, class, bounds in screen coordinates, " +
                     "minimized and active state, and the displays with their bounds. Use a handle with capture to see a window.")]
        public static async Task<CallToolResult> ListWindowsAsync(McpServer server, CancellationToken cancellationToken)
        {
            var request = new JsonObject { ["command"] = "LIST_WINDOWS" };
            return await CallAsync(server, request, reply => TextResult(Without(reply, "stdout", "status", "exit_code")), cancellationToken).ConfigureAwait(false);
        }

        [McpServerTool(Name = "capture", Title = "Capture a screenshot", ReadOnly = true, OpenWorld = false)]
        [Description("Takes a screenshot with Greenshot and returns the image, with title, process and bounds. " +
                     "target=window captures the exact contents of one window (also when it is covered by other windows), selected by handle " +
                     "(from list_windows), title (part of it) or process name. target=active captures the active window, target=screen all displays " +
                     "(or one, with display), target=region a rectangle in screen coordinates (x, y, width, height), e.g. to zoom in on details at full resolution. " +
                     "Windows of applications the user excluded (e.g. password managers) are never captured, and are blacked out in screen and region captures.")]
        public static async Task<CallToolResult> CaptureAsync(
            McpServer server,
            [Description("window, active, screen or region. Default: window when handle, title or process is given, otherwise active.")] string? target = null,
            [Description("Window handle from list_windows, e.g. 0x1A2B3C (target=window).")] string? handle = null,
            [Description("Part of the window title (target=window).")] string? title = null,
            [Description("Process name without .exe, e.g. notepad (target=window).")] string? process = null,
            [Description("Display index from list_windows (target=screen). Default: all displays.")] int? display = null,
            [Description("Left of the region in screen coordinates (target=region).")] int? x = null,
            [Description("Top of the region in screen coordinates (target=region).")] int? y = null,
            [Description("Width of the region (target=region).")] int? width = null,
            [Description("Height of the region (target=region).")] int? height = null,
            [Description("Also recognize the text in the screenshot (OCR); line bounds are in pixels of the full-size image.")] bool ocr = false,
            [Description("Scale the image down so its longest side is at most this many pixels; 0 keeps the original size. Default 1568.")] int? max_size = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new JsonObject();
            AddParameter(parameters, "target", target);
            AddParameter(parameters, "handle", handle);
            AddParameter(parameters, "title", title);
            AddParameter(parameters, "process", process);
            AddParameter(parameters, "display", display);
            AddParameter(parameters, "x", x);
            AddParameter(parameters, "y", y);
            AddParameter(parameters, "width", width);
            AddParameter(parameters, "height", height);
            AddParameter(parameters, "ocr", ocr ? "true" : null);
            AddParameter(parameters, "max_size", max_size ?? DefaultMaxSize);

            var request = new JsonObject
            {
                ["command"] = "CAPTURE",
                ["parameters"] = parameters
            };
            return await CallAsync(server, request, CaptureResult, cancellationToken).ConfigureAwait(false);
        }

        [McpServerTool(Name = "list_recipes", Title = "List recipes", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the Greenshot recipes which can be run (recipes with an enabled command line trigger), with their arguments.")]
        public static async Task<CallToolResult> ListRecipesAsync(McpServer server, CancellationToken cancellationToken)
        {
            var request = new JsonObject { ["command"] = "LIST_RECIPES" };
            return await CallAsync(server, request, reply => TextResult(Without(reply, "stdout", "status", "exit_code")), cancellationToken).ConfigureAwait(false);
        }

        [McpServerTool(Name = "describe_recipe", Title = "Describe a recipe", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Describes a Greenshot recipe: its triggers, inputs, outputs and steps.")]
        public static async Task<CallToolResult> DescribeRecipeAsync(
            McpServer server,
            [Description("Recipe id, name or command.")] string recipe,
            CancellationToken cancellationToken = default)
        {
            var request = new JsonObject
            {
                ["command"] = "DESCRIBE_RECIPE",
                ["recipe"] = recipe
            };
            return await CallAsync(server, request, reply => TextResult(Without(reply, "stdout", "status", "exit_code")), cancellationToken).ConfigureAwait(false);
        }

        [McpServerTool(Name = "run_recipe", Title = "Run a recipe", ReadOnly = false, Destructive = false, OpenWorld = true)]
        [Description("Runs a Greenshot recipe (see list_recipes) and returns its result. Recipes can capture, save, upload or open files, as the user configured them.")]
        public static async Task<CallToolResult> RunRecipeAsync(
            McpServer server,
            [Description("Recipe command, id or name.")] string recipe,
            [Description("Recipe arguments by name, as declared by list_recipes.")] Dictionary<string, string>? arguments = null,
            CancellationToken cancellationToken = default)
        {
            var parameters = new JsonObject();
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    parameters[argument.Key] = argument.Value;
                }
            }
            var request = new JsonObject
            {
                ["command"] = "RUN_RECIPE",
                ["recipe"] = recipe,
                ["parameters"] = parameters,
                ["json"] = true
            };
            return await CallAsync(server, request, reply => TextResult(reply), cancellationToken).ConfigureAwait(false);
        }

        private static async Task<CallToolResult> CallAsync(McpServer server, JsonObject request, Func<JsonObject, CallToolResult> onSuccess, CancellationToken cancellationToken)
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

        private static CallToolResult CaptureResult(JsonObject reply)
        {
            string? data = (string?)reply["data"];
            if (string.IsNullOrEmpty(data))
            {
                return ErrorResult("Greenshot returned no image.");
            }
            string mimeType = (string?)reply["mime_type"] ?? "image/png";
            var metadata = Without(reply, "data", "stdout", "status", "exit_code");
            return new CallToolResult
            {
                Content =
                [
                    ImageContentBlock.FromBytes(Convert.FromBase64String(data), mimeType),
                    new TextContentBlock { Text = metadata.ToJsonString() }
                ]
            };
        }

        private static string? GetClientName(McpServer server)
        {
            var clientInfo = server.ClientInfo;
            if (clientInfo == null)
            {
                return null;
            }
            string name = string.IsNullOrWhiteSpace(clientInfo.Title) ? clientInfo.Name : clientInfo.Title!;
            return string.IsNullOrWhiteSpace(clientInfo.Version) ? name : $"{name} {clientInfo.Version}";
        }

        private static void AddParameter(JsonObject parameters, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parameters[name] = value;
            }
        }

        private static void AddParameter(JsonObject parameters, string name, int? value)
        {
            if (value.HasValue)
            {
                parameters[name] = value.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static JsonObject Without(JsonObject reply, params string[] names)
        {
            var copy = (JsonObject)reply.DeepClone();
            foreach (var name in names)
            {
                copy.Remove(name);
            }
            return copy;
        }

        private static CallToolResult TextResult(JsonObject reply)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = reply.ToJsonString() }]
            };
        }

        private static CallToolResult ErrorResult(string message)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = message }]
            };
        }
    }
}
