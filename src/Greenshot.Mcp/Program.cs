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

using System.Text.Json;
using Greenshot.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

// greenshot-mcp.exe: MCP server over stdio. Stdout carries the protocol, so all logging goes to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Tool parameters and results: the MCP protocol types plus our own (source generated, for Native AOT)
var jsonOptions = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);
jsonOptions.TypeInfoResolverChain.Insert(0, GreenshotMcpJsonContext.Default);

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "greenshot",
            Title = "Greenshot",
            Version = GreenshotConnection.McpServerVersion
        };
        options.ServerInstructions =
            "Greenshot is the screenshot tool running on the user's Windows desktop. " +
            "Use list_windows to see the open windows and displays, then capture_window with a window id to see the exact contents of a window " +
            "(also when it is covered by other windows), capture_region to zoom in on details and capture_screen for all displays; " +
            "ocr=true adds the text. The other tools are Greenshot recipes the user offers to AI tools. " +
            "When the user wants Greenshot to do something new (a hotkey that captures and uploads, a tool for you, ...), write a recipe: " +
            "get_recipe_schema and get_recipe_catalog show what is possible, validate_recipe checks it, propose_recipe or update_recipe " +
            "show it to the user, who decides whether it is saved and which triggers and permissions are switched on. " +
            "The user has to allow each AI tool in Greenshot the first time, and can exclude applications.";
    })
    .WithStdioServerTransport()
    .WithTools<GreenshotTools>(jsonOptions)
    .WithTools<RecipeAuthoringTools>(jsonOptions)
    .WithResources<RecipeAuthoringResources>();

// The user's recipes with an AI tool trigger are the other tools
builder.Services.AddHostedService<RecipeToolSync>();

await builder.Build().RunAsync().ConfigureAwait(false);
