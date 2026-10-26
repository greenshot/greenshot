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

using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;

namespace Greenshot.Linux.Core.Plugins;

/// <summary>
/// A plugin of Greenshot: a group of destinations which can be switched on or off together.
/// The names are the ones of the Windows plugins (the part after Greenshot.Plugin.), so the settings mean the same.
/// On Linux the plugins are built in, there is no loading of foreign code.
/// </summary>
public interface IGreenshotPlugin
{
    /// <summary>
    /// The name in the settings, e.g. ExternalCommand, Box, Dropbox
    /// </summary>
    string Name { get; }

    string DisplayName { get; }

    /// <summary>
    /// The destinations of the plugin right now; they can change when the settings change (e.g. a new external command)
    /// </summary>
    IReadOnlyList<IDestination> Destinations { get; }
}

/// <summary>
/// Which plugins and destinations may be used, with the keys of the Windows version
/// (docs/vNext/plugins-and-destinations.md): the user's ExcludePluginNames and ExcludeDestinations in greenshot.ini,
/// and the administrator's BlockedPluginNames, AllowedPluginNames, BlockedDestinations and AllowedDestinations
/// in greenshot-fixed.ini, which the user can't change.
/// </summary>
public sealed class PluginPolicy
{
    private readonly CoreSettings _settings;
    private readonly IniFile _fixed;

    public PluginPolicy(CoreSettings settings, IniFile? fixedSettings = null)
    {
        _settings = settings;
        _fixed = fixedSettings ?? LoadFixed();
    }

    /// <summary>
    /// greenshot-fixed.ini: in /etc/greenshot, or next to the program
    /// </summary>
    public static IReadOnlyList<string> FixedPaths =>
    [
        "/etc/greenshot/greenshot-fixed.ini",
        Path.Combine(AppContext.BaseDirectory, "greenshot-fixed.ini"),
    ];

    public static IniFile LoadFixed()
    {
        var merged = new IniFile();
        foreach (var path in FixedPaths.Where(File.Exists))
        {
            try
            {
                var file = IniFile.Load(path);
                foreach (var key in new[] { "BlockedPluginNames", "AllowedPluginNames", "ExcludePluginNames", "BlockedDestinations", "AllowedDestinations", "ExcludeDestinations" })
                {
                    if (file.Get(CoreSettings.SectionName, key) is { } value)
                    {
                        merged.Set(CoreSettings.SectionName, key, value);
                    }
                }
            }
            catch (IOException ex)
            {
                Log.Warn($"Could not read {path}", ex);
            }
        }

        return merged;
    }

    /// <summary>
    /// Can the user switch plugins (destinations) on and off? Not when the administrator set the user's list
    /// </summary>
    public bool CanUserChangePlugins => _fixed.Get(CoreSettings.SectionName, "ExcludePluginNames") == null;

    public bool CanUserChangeDestinations => _fixed.Get(CoreSettings.SectionName, "ExcludeDestinations") == null;

    public bool IsPluginBlocked(string name) => IsBlocked(name, "BlockedPluginNames", "AllowedPluginNames");

    public bool IsPluginEnabled(string name) => !IsPluginBlocked(name) && !Contains(UserList("ExcludePluginNames"), name);

    public bool IsDestinationBlocked(string designation) => IsBlocked(designation, "BlockedDestinations", "AllowedDestinations");

    public bool IsDestinationEnabled(string designation) =>
        !IsDestinationBlocked(designation) && !Contains(UserList("ExcludeDestinations"), designation);

    public void SetPluginEnabled(string name, bool enabled) => SetUserList("ExcludePluginNames", name, enabled);

    public void SetDestinationEnabled(string designation, bool enabled) => SetUserList("ExcludeDestinations", designation, enabled);

    private bool IsBlocked(string name, string blockedKey, string allowedKey)
    {
        if (Contains(List(_fixed.Get(CoreSettings.SectionName, blockedKey)), name))
        {
            return true;
        }

        // An allowed list, also an empty one, blocks everything which is not on it
        var allowed = _fixed.Get(CoreSettings.SectionName, allowedKey);
        return allowed != null && !Contains(List(allowed), name);
    }

    /// <summary>
    /// The user's list, unless the administrator fixed it in greenshot-fixed.ini
    /// </summary>
    private IReadOnlyList<string> UserList(string key) => List(_fixed.Get(CoreSettings.SectionName, key) ?? _settings.GetValue(CoreSettings.SectionName, key));

    private void SetUserList(string key, string name, bool enabled)
    {
        if (_fixed.Get(CoreSettings.SectionName, key) != null)
        {
            return;
        }

        var list = UserList(key).Where(n => !n.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!enabled)
        {
            list.Add(name);
        }

        _settings.SetValue(CoreSettings.SectionName, key, string.Join(',', list));
    }

    private static IReadOnlyList<string> List(string? value) =>
        (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool Contains(IReadOnlyList<string> list, string name) => list.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
}
