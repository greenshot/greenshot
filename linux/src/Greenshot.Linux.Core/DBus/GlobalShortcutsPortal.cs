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

using Tmds.DBus.Protocol;

namespace Greenshot.Linux.Core.DBus;

/// <summary>
/// One shortcut Greenshot asks for: an id, the text the desktop shows and the preferred trigger
/// in the format of the XDG shortcuts specification, e.g. "SHIFT+Print"
/// </summary>
public sealed record ShortcutRequest(string Id, string Description, string? PreferredTrigger);

/// <summary>
/// Global hotkeys on Wayland: an application can't grab keys, it asks the GlobalShortcuts portal (KDE, GNOME 48+, Hyprland).
/// The desktop decides the final keys and may ask the user to confirm them.
/// </summary>
public sealed class GlobalShortcutsPortal : IDisposable
{
    private const string ShortcutsInterface = "org.freedesktop.portal.GlobalShortcuts";

    private readonly DesktopPortal _portal;
    private string? _session;
    private IDisposable? _activatedSubscription;

    public GlobalShortcutsPortal(DesktopPortal portal)
    {
        _portal = portal;
    }

    /// <summary>
    /// Called with the id of a shortcut when it is pressed, not on the UI thread
    /// </summary>
    public event Action<string>? Activated;

    public static async Task<bool> IsAvailableAsync(DesktopPortal portal) =>
        await portal.GetVersionAsync(ShortcutsInterface).ConfigureAwait(false) != null;

    /// <summary>
    /// Create a session and bind the shortcuts, returns the triggers the desktop assigned (id → description of the trigger)
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> BindAsync(IReadOnlyList<ShortcutRequest> shortcuts, CancellationToken cancellationToken = default)
    {
        var sessionToken = DesktopPortal.NewToken();
        var created = await _portal.RequestAsync(ShortcutsInterface, "CreateSession", "a{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteOptions(new Dictionary<string, object>
            {
                ["handle_token"] = token,
                ["session_handle_token"] = sessionToken,
            });
        }, cancellationToken).ConfigureAwait(false);

        if (!created.IsSuccess || !created.Results.TryGetValue("session_handle", out var sessionValue))
        {
            throw new InvalidOperationException($"Creating the global shortcuts session failed with code {created.Code}");
        }

        // The specification says "s", some versions sent an object path
        _session = sessionValue.Type == VariantValueType.ObjectPath ? sessionValue.GetObjectPathAsString() : sessionValue.GetString();

        _activatedSubscription = await _portal.Connection.WatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = ShortcutsInterface,
                Member = "Activated",
                Path = DesktopPortal.ObjectPath,
            },
            static (m, _) =>
            {
                var reader = m.GetBodyReader();
                var session = reader.ReadObjectPathAsString();
                var id = reader.ReadString();
                return (session, id);
            },
            value =>
            {
                if (value.session == _session)
                {
                    Activated?.Invoke(value.id);
                }
            },
            ex => Log.Warn("Listening for global shortcuts failed", ex)).ConfigureAwait(false);

        var bound = await _portal.RequestAsync(ShortcutsInterface, "BindShortcuts", "oa(sa{sv})sa{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteObjectPath(_session);
            var array = writer.WriteArrayStart(DBusType.Struct);
            foreach (var shortcut in shortcuts)
            {
                writer.WriteStructureStart();
                writer.WriteString(shortcut.Id);
                var options = new Dictionary<string, object> { ["description"] = shortcut.Description };
                if (!string.IsNullOrEmpty(shortcut.PreferredTrigger))
                {
                    options["preferred_trigger"] = shortcut.PreferredTrigger;
                }

                writer.WriteOptions(options);
            }

            writer.WriteArrayEnd(array);
            writer.WriteString(string.Empty);
            writer.WriteOptions(new Dictionary<string, object> { ["handle_token"] = token });
        }, cancellationToken).ConfigureAwait(false);

        var assigned = new Dictionary<string, string>();
        if (bound.IsSuccess && bound.Results.TryGetValue("shortcuts", out var boundShortcuts))
        {
            for (int i = 0; i < boundShortcuts.Count; i++)
            {
                var item = boundShortcuts.GetItem(i);
                var id = item.GetItem(0).GetString();
                var properties = item.GetItem(1);
                var trigger = string.Empty;
                for (int p = 0; p < properties.Count; p++)
                {
                    var entry = properties.GetDictionaryEntry(p);
                    if (entry.Key.GetString() == "trigger_description")
                    {
                        trigger = entry.Value.GetVariantValue().GetString();
                    }
                }

                assigned[id] = trigger;
            }
        }

        return assigned;
    }

    /// <summary>
    /// Convert a Greenshot hotkey like "Shift + PrintScreen" to the XDG trigger "SHIFT+Print"
    /// </summary>
    public static string? ToXdgTrigger(string? hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey) || hotkey.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var converted = parts.Select(part => part.ToLowerInvariant() switch
        {
            "ctrl" or "control" => "CTRL",
            "alt" => "ALT",
            "shift" => "SHIFT",
            "win" or "windows" or "super" or "logo" => "LOGO",
            "printscreen" or "print" or "prtsc" => "Print",
            _ => part.Length == 1 ? part.ToLowerInvariant() : part,
        });
        return string.Join('+', converted);
    }

    public void Dispose()
    {
        _activatedSubscription?.Dispose();
        _activatedSubscription = null;
    }
}
