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
using System.Linq;
using Dapplo.Windows.Desktop;

namespace Greenshot.Ai
{
    /// <summary>
    /// Window references for AI tools: list_windows hands out short ids like "w7" instead of window handles, and a recipe argument
    /// of type Window only accepts such an id. So an AI tool can only capture windows Greenshot listed to it:
    /// <list type="bullet">
    /// <item>the ids belong to one AI tool and greenshot-mcp.exe process (another program, or a new session, can't use them),</item>
    /// <item>they expire <see cref="Lifetime"/> after the last list_windows which showed the window,</item>
    /// <item>windows of excluded processes never get an id,</item>
    /// <item>an id stops working when the window is closed (also when Windows reuses the handle for another process).</item>
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

        private static readonly object Lock = new object();
        // Ids are never reused while Greenshot runs, so an old id can't point to another window
        private static long _nextNumber = 1;
        private static readonly Dictionary<string, Scope> Scopes = new Dictionary<string, Scope>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The clock, replaceable for tests
        /// </summary>
        internal static Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>
        /// The process id of a window, 0 when the window doesn't exist (anymore). Replaceable for tests.
        /// </summary>
        internal static Func<IntPtr, int> GetWindowProcessId { get; set; } = handle => InteropWindowFactory.CreateFor(handle).GetProcessId();

        private sealed class Entry
        {
            public string Id;
            public IntPtr Handle;
            public int ProcessId;
            public DateTime Expires;
        }

        private sealed class Scope
        {
            public readonly Dictionary<string, Entry> ById = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<IntPtr, Entry> ByHandle = new Dictionary<IntPtr, Entry>();
        }

        /// <summary>
        /// The id for this window, for this AI tool: the same window keeps its id while it is valid.
        /// </summary>
        /// <param name="client">The AI tool (identified by Greenshot)</param>
        /// <param name="handle">The window</param>
        /// <param name="processId">The process of the window</param>
        public static string Register(AiToolClient client, IntPtr handle, int processId)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            var now = UtcNow();
            lock (Lock)
            {
                PruneScopes(now);
                var scope = GetScope(client, true);
                if (scope.ByHandle.TryGetValue(handle, out var entry) && entry.ProcessId == processId)
                {
                    entry.Expires = now + Lifetime;
                    return entry.Id;
                }
                if (entry != null)
                {
                    // The handle belongs to another process now
                    Remove(scope, entry);
                }

                entry = new Entry
                {
                    Id = "w" + _nextNumber++,
                    Handle = handle,
                    ProcessId = processId,
                    Expires = now + Lifetime
                };
                scope.ById[entry.Id] = entry;
                scope.ByHandle[handle] = entry;

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
        /// Resolves an id from list_windows to the window handle, for the AI tool which got it.
        /// </summary>
        /// <returns>false with an error message for the AI when the id is unknown, expired, of another AI tool, or the window is gone</returns>
        public static bool TryResolve(AiToolClient client, string windowRef, out IntPtr handle, out string error)
        {
            handle = IntPtr.Zero;
            if (client == null)
            {
                error = "Window arguments are only available to AI tools.";
                return false;
            }
            string id = windowRef?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                error = "A window id from list_windows is needed. " + StaleRefMessage;
                return false;
            }

            Entry entry;
            var now = UtcNow();
            lock (Lock)
            {
                var scope = GetScope(client, false);
                if (scope != null)
                {
                    RemoveExpired(scope, now);
                }
                if (scope == null || !scope.ById.TryGetValue(id, out entry))
                {
                    error = $"'{id}' is not a current window id. " + StaleRefMessage;
                    return false;
                }
            }

            int processId = GetWindowProcessId(entry.Handle);
            if (processId == 0 || processId != entry.ProcessId)
            {
                lock (Lock)
                {
                    var scope = GetScope(client, false);
                    if (scope != null)
                    {
                        Remove(scope, entry);
                    }
                }
                error = $"The window '{id}' was closed. " + StaleRefMessage;
                return false;
            }

            handle = entry.Handle;
            error = null;
            return true;
        }

        /// <summary>
        /// Forget all ids (for tests)
        /// </summary>
        internal static void Clear()
        {
            lock (Lock)
            {
                Scopes.Clear();
            }
        }

        private static string ScopeKey(AiToolClient client) => $"{client.ServerProcessId}|{client.ExePath}";

        private static Scope GetScope(AiToolClient client, bool create)
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
        /// Drops the expired ids of all AI tools, and the AI tools (greenshot-mcp.exe sessions) without ids
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
            if (scope.ByHandle.TryGetValue(entry.Handle, out var current) && ReferenceEquals(current, entry))
            {
                scope.ByHandle.Remove(entry.Handle);
            }
        }
    }
}
