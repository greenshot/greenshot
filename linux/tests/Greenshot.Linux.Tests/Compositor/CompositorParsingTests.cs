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
using Greenshot.Linux.Core.Compositor;

namespace Greenshot.Linux.Tests.Compositor;

public class CompositorParsingTests
{
    [Fact]
    public void Sway_outputs_skip_inactive_ones()
    {
        using var json = JsonDocument.Parse("""
            [
              {"name":"eDP-1","active":true,"focused":true,"scale":1.5,"rect":{"x":0,"y":0,"width":1280,"height":800}},
              {"name":"HDMI-A-1","active":false,"rect":{"x":0,"y":0,"width":0,"height":0}},
              {"name":"DP-2","active":true,"focused":false,"scale":1.0,"rect":{"x":1280,"y":0,"width":2560,"height":1440}}
            ]
            """);
        var layout = SwayCompositor.ParseOutputs(json.RootElement)!;
        Assert.Equal(["eDP-1", "DP-2"], layout.Screens.Select(s => s.Name));
        Assert.True(layout.Screens[0].IsPrimary);
        Assert.Equal(1.5, layout.Screens[0].Scale);
        Assert.Equal(new LayoutRect(0, 0, 3840, 1440), layout.Bounds);
    }

    [Fact]
    public void Sway_tree_lists_visible_windows_top_most_first()
    {
        using var json = JsonDocument.Parse("""
            {"type":"root","nodes":[
              {"type":"output","name":"eDP-1","nodes":[
                {"type":"workspace","name":"1","nodes":[
                  {"type":"con","name":"Editor","app_id":"code","visible":true,"focused":false,"rect":{"x":0,"y":0,"width":640,"height":800},"nodes":[],"floating_nodes":[]},
                  {"type":"con","name":"Terminal","app_id":"foot","visible":true,"focused":true,"rect":{"x":640,"y":0,"width":640,"height":800},"nodes":[],"floating_nodes":[]}
                ],"floating_nodes":[
                  {"type":"floating_con","name":"Calculator","app_id":"calc","visible":true,"focused":false,"rect":{"x":100,"y":100,"width":200,"height":300},"nodes":[],"floating_nodes":[]}
                ]},
                {"type":"workspace","name":"2","nodes":[
                  {"type":"con","name":"Hidden","app_id":"x","visible":false,"rect":{"x":0,"y":0,"width":1280,"height":800},"nodes":[],"floating_nodes":[]}
                ],"floating_nodes":[]}
              ]}
            ]}
            """);
        var windows = SwayCompositor.ParseTree(json.RootElement);
        Assert.Equal(["Calculator", "Editor", "Terminal"], windows.Select(w => w.Info.Title));
        Assert.Equal("Terminal", windows.Single(w => w.Focused).Info.Title);
        Assert.Equal(new LayoutRect(100, 100, 200, 300), windows[0].Info.Bounds);
    }

    [Fact]
    public void Sway_xwayland_windows_use_the_class()
    {
        using var json = JsonDocument.Parse("""
            {"type":"con","name":"Old app","app_id":null,"window_properties":{"class":"OldApp"},"visible":true,"rect":{"x":0,"y":0,"width":10,"height":10},"nodes":[],"floating_nodes":[]}
            """);
        Assert.Equal("OldApp", SwayCompositor.ParseTree(json.RootElement).Single().Info.AppId);
    }

    [Fact]
    public void Hyprland_monitors_are_in_logical_pixels()
    {
        using var json = JsonDocument.Parse("""
            [
              {"name":"eDP-1","x":0,"y":0,"width":2880,"height":1800,"scale":2.0,"transform":0,"focused":true,"disabled":false},
              {"name":"DP-1","x":1440,"y":0,"width":1920,"height":1080,"scale":1.0,"transform":1,"focused":false,"disabled":false}
            ]
            """);
        var layout = HyprlandCompositor.ParseMonitors(json.RootElement)!;
        Assert.Equal(new LayoutRect(0, 0, 1440, 900), layout.Screens[0].Bounds);
        // Rotated by 90 degrees
        Assert.Equal(new LayoutRect(1440, 0, 1080, 1920), layout.Screens[1].Bounds);
    }

    [Fact]
    public void Hyprland_clients_on_hidden_workspaces_are_skipped()
    {
        using var monitors = JsonDocument.Parse("""
            [{"name":"eDP-1","activeWorkspace":{"id":1},"specialWorkspace":{"id":0}}]
            """);
        using var clients = JsonDocument.Parse("""
            [
              {"title":"Tiled","class":"a","at":[0,0],"size":[700,900],"workspace":{"id":1},"mapped":true,"hidden":false,"floating":false,"focusHistoryID":0},
              {"title":"Float","class":"b","at":[50,50],"size":[200,200],"workspace":{"id":1},"mapped":true,"hidden":false,"floating":true,"focusHistoryID":1},
              {"title":"Elsewhere","class":"c","at":[0,0],"size":[100,100],"workspace":{"id":2},"mapped":true,"hidden":false,"floating":false,"focusHistoryID":2}
            ]
            """);
        var windows = HyprlandCompositor.ParseVisibleClients(monitors.RootElement, clients.RootElement);
        Assert.Equal(["Float", "Tiled"], windows.Select(w => w.Title));
    }
}
