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
/// Sway (and other i3 IPC compatible compositors) through swaymsg
/// </summary>
public sealed class SwayCompositor : ICompositor
{
    public string Name => "sway";

    public bool KnowsWindows => true;

    public async Task<ScreenLayout?> GetLayoutAsync()
    {
        using var json = await QueryAsync("get_outputs").ConfigureAwait(false);
        return json == null ? null : ParseOutputs(json.RootElement);
    }

    public async Task<WindowInfo?> GetActiveWindowAsync()
    {
        using var json = await QueryAsync("get_tree").ConfigureAwait(false);
        return json == null ? null : ParseTree(json.RootElement).FirstOrDefault(w => w.Focused)?.Info;
    }

    public async Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync()
    {
        using var json = await QueryAsync("get_tree").ConfigureAwait(false);
        return json == null ? Array.Empty<WindowInfo>() : ParseTree(json.RootElement).Select(w => w.Info).ToList();
    }

    internal static ScreenLayout? ParseOutputs(JsonElement outputs)
    {
        var screens = new List<ScreenInfo>();
        foreach (var output in outputs.EnumerateArray())
        {
            if (!output.TryGetProperty("active", out var active) || !active.GetBoolean())
            {
                continue;
            }

            var scale = output.TryGetProperty("scale", out var scaleValue) ? scaleValue.GetDouble() : 1.0;
            var focused = output.TryGetProperty("focused", out var focusedValue) && focusedValue.GetBoolean();
            screens.Add(new ScreenInfo(output.GetProperty("name").GetString() ?? "?", ReadRect(output.GetProperty("rect")), scale, focused));
        }

        return screens.Count == 0 ? null : new ScreenLayout(screens);
    }

    internal sealed record TreeWindow(WindowInfo Info, bool Focused);

    /// <summary>
    /// The visible windows, the top most first: fullscreen, then floating (the last one is on top), then tiled
    /// </summary>
    internal static IReadOnlyList<TreeWindow> ParseTree(JsonElement root)
    {
        var fullscreen = new List<TreeWindow>();
        var floating = new List<TreeWindow>();
        var tiled = new List<TreeWindow>();
        Walk(root, false);
        floating.Reverse();
        return fullscreen.Concat(floating).Concat(tiled).ToList();

        void Walk(JsonElement node, bool isFloating)
        {
            var hasChildren = false;
            if (node.TryGetProperty("nodes", out var nodes))
            {
                foreach (var child in nodes.EnumerateArray())
                {
                    hasChildren = true;
                    Walk(child, isFloating);
                }
            }

            if (node.TryGetProperty("floating_nodes", out var floatingNodes))
            {
                foreach (var child in floatingNodes.EnumerateArray())
                {
                    hasChildren = true;
                    Walk(child, true);
                }
            }

            var type = node.TryGetProperty("type", out var typeValue) ? typeValue.GetString() : null;
            if (hasChildren || (type != "con" && type != "floating_con"))
            {
                return;
            }

            if (!node.TryGetProperty("visible", out var visible) || !visible.GetBoolean())
            {
                return;
            }

            var title = node.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? string.Empty : string.Empty;
            string? appId = null;
            if (node.TryGetProperty("app_id", out var appIdValue) && appIdValue.ValueKind == JsonValueKind.String)
            {
                appId = appIdValue.GetString();
            }
            else if (node.TryGetProperty("window_properties", out var properties) && properties.TryGetProperty("class", out var windowClass))
            {
                appId = windowClass.GetString();
            }

            var window = new TreeWindow(new WindowInfo(title, appId, ReadRect(node.GetProperty("rect"))),
                node.TryGetProperty("focused", out var focused) && focused.GetBoolean());
            var isFullscreen = node.TryGetProperty("fullscreen_mode", out var mode) && mode.GetInt32() > 0;
            (isFullscreen ? fullscreen : isFloating ? floating : tiled).Add(window);
        }
    }

    private static LayoutRect ReadRect(JsonElement rect) => new(
        rect.GetProperty("x").GetInt32(),
        rect.GetProperty("y").GetInt32(),
        rect.GetProperty("width").GetInt32(),
        rect.GetProperty("height").GetInt32());

    private static async Task<JsonDocument?> QueryAsync(string type)
    {
        try
        {
            var result = await ProcessRunner.RunAsync("swaymsg", ["-r", "-t", type], timeout: TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                return JsonDocument.Parse(result.Output);
            }

            Log.Warn($"swaymsg -t {type} failed: {result.Error.Trim()}");
        }
        catch (Exception ex)
        {
            Log.Warn($"swaymsg -t {type} failed", ex);
        }

        return null;
    }
}
