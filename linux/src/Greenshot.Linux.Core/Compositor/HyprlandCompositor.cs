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

using System.Text.Json;
using Greenshot.Linux.Core.Capturing;

namespace Greenshot.Linux.Core.Compositor;

/// <summary>
/// Hyprland through hyprctl
/// </summary>
public sealed class HyprlandCompositor : ICompositor
{
    public string Name => "Hyprland";

    public bool KnowsWindows => true;

    public async Task<ScreenLayout?> GetLayoutAsync()
    {
        using var json = await QueryAsync("monitors").ConfigureAwait(false);
        return json == null ? null : ParseMonitors(json.RootElement);
    }

    public async Task<WindowInfo?> GetActiveWindowAsync()
    {
        using var json = await QueryAsync("activewindow").ConfigureAwait(false);
        if (json == null || json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("at", out _))
        {
            return null;
        }

        return ReadClient(json.RootElement);
    }

    public async Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync()
    {
        using var monitors = await QueryAsync("monitors").ConfigureAwait(false);
        using var clients = await QueryAsync("clients").ConfigureAwait(false);
        if (monitors == null || clients == null)
        {
            return Array.Empty<WindowInfo>();
        }

        return ParseVisibleClients(monitors.RootElement, clients.RootElement);
    }

    internal static ScreenLayout? ParseMonitors(JsonElement monitors)
    {
        var screens = new List<ScreenInfo>();
        foreach (var monitor in monitors.EnumerateArray())
        {
            if (monitor.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean())
            {
                continue;
            }

            var scale = monitor.TryGetProperty("scale", out var scaleValue) ? scaleValue.GetDouble() : 1.0;
            // width and height are in physical pixels, the layout uses logical ones
            var transform = monitor.TryGetProperty("transform", out var transformValue) ? transformValue.GetInt32() : 0;
            var width = monitor.GetProperty("width").GetInt32();
            var height = monitor.GetProperty("height").GetInt32();
            if (transform % 2 == 1)
            {
                (width, height) = (height, width);
            }

            var bounds = new LayoutRect(monitor.GetProperty("x").GetInt32(), monitor.GetProperty("y").GetInt32(),
                (int)Math.Round(width / scale), (int)Math.Round(height / scale));
            var focused = monitor.TryGetProperty("focused", out var focusedValue) && focusedValue.GetBoolean();
            screens.Add(new ScreenInfo(monitor.GetProperty("name").GetString() ?? "?", bounds, scale, focused));
        }

        return screens.Count == 0 ? null : new ScreenLayout(screens);
    }

    internal static IReadOnlyList<WindowInfo> ParseVisibleClients(JsonElement monitors, JsonElement clients)
    {
        var visibleWorkspaces = new HashSet<int>();
        foreach (var monitor in monitors.EnumerateArray())
        {
            foreach (var property in new[] { "activeWorkspace", "specialWorkspace" })
            {
                if (monitor.TryGetProperty(property, out var workspace) && workspace.TryGetProperty("id", out var id) && id.GetInt32() != 0)
                {
                    visibleWorkspaces.Add(id.GetInt32());
                }
            }
        }

        return clients.EnumerateArray()
            .Where(c => IsTrue(c, "mapped") && !IsTrue(c, "hidden")
                        && c.TryGetProperty("workspace", out var ws) && visibleWorkspaces.Contains(ws.GetProperty("id").GetInt32()))
            // Fullscreen and floating windows are above tiled ones, then the most recently focused first
            .OrderByDescending(c => c.TryGetProperty("fullscreen", out var fs) && (fs.ValueKind == JsonValueKind.True || (fs.ValueKind == JsonValueKind.Number && fs.GetInt32() > 0)))
            .ThenByDescending(c => IsTrue(c, "floating"))
            .ThenBy(c => c.TryGetProperty("focusHistoryID", out var history) ? history.GetInt32() : int.MaxValue)
            .Select(ReadClient)
            .ToList();
    }

    private static bool IsTrue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    private static WindowInfo ReadClient(JsonElement client)
    {
        var at = client.GetProperty("at");
        var size = client.GetProperty("size");
        var bounds = new LayoutRect(at[0].GetInt32(), at[1].GetInt32(), size[0].GetInt32(), size[1].GetInt32());
        return new WindowInfo(client.GetProperty("title").GetString() ?? string.Empty, client.GetProperty("class").GetString(), bounds);
    }

    private static async Task<JsonDocument?> QueryAsync(string command)
    {
        try
        {
            var result = await ProcessRunner.RunAsync("hyprctl", ["-j", command], timeout: TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                return JsonDocument.Parse(result.Output);
            }

            Log.Warn($"hyprctl -j {command} failed: {result.Error.Trim()}");
        }
        catch (Exception ex)
        {
            Log.Warn($"hyprctl -j {command} failed", ex);
        }

        return null;
    }
}
