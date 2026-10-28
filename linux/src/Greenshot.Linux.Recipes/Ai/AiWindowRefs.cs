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

using Greenshot.Linux.Core.Compositor;

namespace Greenshot.Linux.Recipes.Ai;

/// <summary>
/// Window references for AI tools: list_windows hands out short ids like "w7" instead of the compositor's window ids, and a
/// recipe argument of type Window only accepts such an id. So an AI tool can only capture windows Greenshot listed to it:
/// <list type="bullet">
/// <item>the ids belong to one AI tool and greenshot-mcp process (another program, or a new session, can't use them),</item>
/// <item>they expire <see cref="Lifetime"/> after the last list_windows which showed the window,</item>
/// <item>windows of excluded applications never get an id,</item>
/// <item>an id stops working when the window is closed.</item>
/// </list>
/// </summary>
public static class AiWindowRefs
{
    /// <summary>
    /// How long an id stays valid after list_windows showed the window
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The most ids kept per AI tool, the oldest are dropped
    /// </summary>
    private const int MaxRefsPerScope = 2000;

    public const string StaleRefMessage = "Call list_windows again and use a window id from its result.";

    private static readonly Lock Sync = new();
    // Ids are never reused while Greenshot runs, so an old id can't point to another window
    private static long _nextNumber = 1;
    private static readonly Dictionary<string, Scope> Scopes = new(StringComparer.Ordinal);

    /// <summary>
    /// The clock, replaceable for tests
    /// </summary>
    internal static Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

    /// <summary>
    /// The windows there are now, replaceable for tests
    /// </summary>
    internal static Func<Task<IReadOnlyList<WindowInfo>>> CurrentWindows { get; set; } = () =>
        RecipeServices.Compositor?.GetVisibleWindowsAsync() ?? Task.FromResult<IReadOnlyList<WindowInfo>>([]);

    private sealed class Entry(string id, string windowId, int processId)
    {
        public string Id { get; } = id;
        public string WindowId { get; } = windowId;
        public int ProcessId { get; } = processId;
        public DateTime Expires { get; set; }
    }

    private sealed class Scope
    {
        public Dictionary<string, Entry> ById { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Entry> ByWindow { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// The id of this window for this AI tool: the same window keeps its id while it is valid
    /// </summary>
    public static string Register(AiToolClient client, WindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(client);
        string windowId = window.Id ?? throw new ArgumentException("The window has no id", nameof(window));
        var now = UtcNow();
        lock (Sync)
        {
            PruneScopes(now);
            var scope = GetScope(client, true)!;
            if (scope.ByWindow.TryGetValue(windowId, out var entry) && entry.ProcessId == window.ProcessId)
            {
                entry.Expires = now + Lifetime;
                return entry.Id;
            }

            if (entry != null)
            {
                // The compositor reused the id for another process
                Remove(scope, entry);
            }

            entry = new Entry("w" + _nextNumber++, windowId, window.ProcessId) { Expires = now + Lifetime };
            scope.ById[entry.Id] = entry;
            scope.ByWindow[windowId] = entry;
            if (scope.ById.Count > MaxRefsPerScope)
            {
                foreach (var oldest in scope.ById.Values.OrderBy(e => e.Expires).Take(scope.ById.Count - MaxRefsPerScope).ToList())
                {
                    Remove(scope, oldest);
                }
            }

            return entry.Id;
        }
    }

    /// <summary>
    /// The window of an id from list_windows, for the AI tool which got it, with its current place
    /// </summary>
    /// <returns>false with an error message for the AI when the id is unknown, expired, of another AI tool, or the window is gone</returns>
    public static async Task<(WindowInfo? Window, string? Error)> ResolveAsync(AiToolClient? client, string? windowRef)
    {
        if (client == null)
        {
            return (null, "Window arguments are only available to AI tools.");
        }

        string? id = windowRef?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return (null, "A window id from list_windows is needed. " + StaleRefMessage);
        }

        Entry? entry;
        lock (Sync)
        {
            var scope = GetScope(client, false);
            if (scope != null)
            {
                RemoveExpired(scope, UtcNow());
            }

            if (scope == null || !scope.ById.TryGetValue(id, out entry))
            {
                return (null, $"'{id}' is not a current window id. " + StaleRefMessage);
            }
        }

        var window = (await CurrentWindows().ConfigureAwait(false)).FirstOrDefault(w => w.Id == entry.WindowId && w.ProcessId == entry.ProcessId);
        if (window == null)
        {
            lock (Sync)
            {
                if (GetScope(client, false) is { } scope)
                {
                    Remove(scope, entry);
                }
            }

            return (null, $"The window '{id}' was closed or is not visible anymore. " + StaleRefMessage);
        }

        return (window, null);
    }

    internal static void Clear()
    {
        lock (Sync)
        {
            Scopes.Clear();
        }
    }

    private static string ScopeKey(AiToolClient client) => $"{client.ServerProcessId}|{client.Identity}";

    private static Scope? GetScope(AiToolClient client, bool create)
    {
        string key = ScopeKey(client);
        if (!Scopes.TryGetValue(key, out var scope) && create)
        {
            scope = new Scope();
            Scopes[key] = scope;
        }

        return scope;
    }

    /// <summary>
    /// Drops the expired ids of all AI tools, and the AI tools (greenshot-mcp sessions) without ids
    /// </summary>
    private static void PruneScopes(DateTime now)
    {
        foreach (var pair in Scopes.ToList())
        {
            RemoveExpired(pair.Value, now);
            if (pair.Value.ById.Count == 0)
            {
                Scopes.Remove(pair.Key);
            }
        }
    }

    private static void RemoveExpired(Scope scope, DateTime now)
    {
        foreach (var expired in scope.ById.Values.Where(e => e.Expires <= now).ToList())
        {
            Remove(scope, expired);
        }
    }

    private static void Remove(Scope scope, Entry entry)
    {
        scope.ById.Remove(entry.Id);
        if (scope.ByWindow.TryGetValue(entry.WindowId, out var current) && ReferenceEquals(current, entry))
        {
            scope.ByWindow.Remove(entry.WindowId);
        }
    }
}
