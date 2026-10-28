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

using Greenshot.Linux.Core.Capturing;

namespace Greenshot.Linux.Core.Compositor;

/// <summary>
/// A window of another application, as far as the compositor tells: its process, and the id the compositor gives it
/// (Sway's container id, Hyprland's address), which stays the same while the window is open
/// </summary>
public sealed record WindowInfo(string Title, string? AppId, LayoutRect Bounds, int ProcessId = 0, string? Id = null, bool Focused = false);

/// <summary>
/// What Greenshot can learn from the compositor. Wayland doesn't let applications see each other's windows,
/// only some compositors tell it over their own IPC (Sway, Hyprland). Elsewhere window capture needs the portal picker.
/// </summary>
public interface ICompositor
{
    string Name { get; }

    /// <summary>
    /// Does the compositor tell where windows are, so window capture works like on Windows?
    /// </summary>
    bool KnowsWindows { get; }

    /// <summary>
    /// The outputs and their place in the layout, null when the compositor doesn't tell
    /// </summary>
    Task<ScreenLayout?> GetLayoutAsync();

    Task<WindowInfo?> GetActiveWindowAsync();

    /// <summary>
    /// The windows on the visible workspaces, the top most first
    /// </summary>
    Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync();
}

public static class CompositorDetector
{
    public static ICompositor Detect()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SWAYSOCK")) && ProcessRunner.IsOnPath("swaymsg"))
        {
            return new SwayCompositor();
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE")) && ProcessRunner.IsOnPath("hyprctl"))
        {
            return new HyprlandCompositor();
        }

        return new GenericCompositor(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "unknown");
    }
}

/// <summary>
/// GNOME, KDE and others: no window information, the layout comes from the UI toolkit
/// </summary>
public sealed class GenericCompositor(string name) : ICompositor
{
    public string Name { get; } = name;

    public bool KnowsWindows => false;

    public Task<ScreenLayout?> GetLayoutAsync() => Task.FromResult<ScreenLayout?>(null);

    public Task<WindowInfo?> GetActiveWindowAsync() => Task.FromResult<WindowInfo?>(null);

    public Task<IReadOnlyList<WindowInfo>> GetVisibleWindowsAsync() => Task.FromResult<IReadOnlyList<WindowInfo>>(Array.Empty<WindowInfo>());
}
