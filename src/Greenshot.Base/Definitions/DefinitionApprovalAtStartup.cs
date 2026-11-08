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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using log4net;

namespace Greenshot.Base.Definitions;

/// <summary>
/// The question at the start for the definition files which were added or changed outside Greenshot, like the one for the
/// recipe files: Greenshot starts without them, and once it runs it shows what each one does and asks whether to use it.
/// One which isn't approved stays unused and is asked about at the next start again; the settings page can approve it too.
/// </summary>
public static class DefinitionApprovalAtStartup
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(DefinitionApprovalAtStartup));

    /// <summary>
    /// The stored definitions which can be read but wait for the user's approval
    /// </summary>
    public static IReadOnlyList<StoredDefinition<T>> NotApproved<T>(DefinitionStore<T> store) where T : class =>
        store?.GetAll().Where(stored => stored.Definition != null && !stored.IsApproved && !stored.IsBuiltIn).ToList() ?? new List<StoredDefinition<T>>();

    /// <summary>
    /// Ask about each definition which waits for approval and approve the confirmed ones
    /// </summary>
    /// <param name="store">the definitions of the folder, as read at the start</param>
    /// <param name="confirm">shows what the definition does, true when the user approves it</param>
    /// <param name="approved">called for each approved definition, e.g. to add its destination</param>
    /// <returns>the number of approved definitions</returns>
    public static int Ask<T>(DefinitionStore<T> store, Func<T, bool> confirm, Action<T> approved = null) where T : class
    {
        if (confirm == null) throw new ArgumentNullException(nameof(confirm));
        int count = 0;
        foreach (var stored in NotApproved(store))
        {
            if (!confirm(stored.Definition))
            {
                Log.InfoFormat("The {0} file {1} was not approved, it stays unused", store.Kind.Designation, stored.Id);
                continue;
            }

            if (!store.Approve(stored.Id))
            {
                continue;
            }

            Log.InfoFormat("Approved the {0} file {1}", store.Kind.Designation, stored.Id);
            approved?.Invoke(stored.Definition);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Ask once Greenshot runs, not inside its start: posted to the dispatcher of the calling (UI) thread, for when it is idle.
    /// Nothing is shown when no definition waits for approval.
    /// </summary>
    /// <param name="createStore">the definitions of the folder, read when the question comes</param>
    /// <param name="confirm">shows what the definition does, true when the user approves it</param>
    /// <param name="approved">called for each approved definition</param>
    public static void AskWhenIdle<T>(Func<DefinitionStore<T>> createStore, Func<T, bool> confirm, Action<T> approved = null) where T : class
    {
        if (createStore == null) throw new ArgumentNullException(nameof(createStore));
        _ = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                Ask(createStore(), confirm, approved);
            }
            catch (Exception ex)
            {
                Log.Error("Asking about the changed definition files failed", ex);
            }
        }), DispatcherPriority.ApplicationIdle);
    }
}
