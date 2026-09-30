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

using System.Threading;
using System.Windows.Forms;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// <see cref="IUiDispatcher"/> for the WinForms UI thread. Create it on the UI thread, before <c>Application.Run</c>.
    /// </summary>
    /// <remarks>
    /// Uses the <see cref="WindowsFormsSynchronizationContext"/> captured at creation, this avoids the
    /// "handle not yet created" failure of <c>Control.BeginInvoke</c> during startup and shutdown.
    /// </remarks>
    public sealed class WinFormsUiDispatcher : SynchronizationContextUiDispatcher
    {
        private WinFormsUiDispatcher(SynchronizationContext context) : base(context, Thread.CurrentThread.ManagedThreadId)
        {
        }

        /// <summary>
        /// Create the dispatcher for the calling thread, installs a WindowsFormsSynchronizationContext if there is none yet.
        /// </summary>
        public static WinFormsUiDispatcher CreateForCurrentThread()
        {
#pragma warning disable RS0030 // The one place which is allowed to know the WinForms SynchronizationContext
            if (SynchronizationContext.Current is not WindowsFormsSynchronizationContext context)
            {
                context = new WindowsFormsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(context);
            }
#pragma warning restore RS0030

            var dispatcher = new WinFormsUiDispatcher(context);
            ThreadAssert.Initialize(dispatcher);
            return dispatcher;
        }
    }
}
