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
using System.Windows.Threading;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// <see cref="IUiDispatcher"/> for the WPF UI thread, the thread of the WPF Dispatcher which runs the message loop.
    /// Create it on the UI thread, before the message loop runs (e.g. before <c>Application.Run</c>).
    /// </summary>
    /// <remarks>
    /// Posts with the <see cref="DispatcherSynchronizationContext"/> of the thread's Dispatcher, captured at creation. WinForms forms
    /// on this thread (the editor) install their own SynchronizationContext when they are created, that doesn't change this one.
    /// </remarks>
    public sealed class WpfUiDispatcher : SynchronizationContextUiDispatcher
    {
        private WpfUiDispatcher(SynchronizationContext context, Dispatcher dispatcher) : base(context, dispatcher.Thread.ManagedThreadId)
        {
            Dispatcher = dispatcher;
        }

        /// <summary>
        /// The WPF Dispatcher of the UI thread
        /// </summary>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Create the dispatcher for the calling thread, installs a DispatcherSynchronizationContext if there is none yet.
        /// </summary>
        public static WpfUiDispatcher CreateForCurrentThread()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
#pragma warning disable RS0030 // The one place which is allowed to know the WPF SynchronizationContext
            if (SynchronizationContext.Current is not DispatcherSynchronizationContext context)
            {
                context = new DispatcherSynchronizationContext(dispatcher);
                SynchronizationContext.SetSynchronizationContext(context);
            }
#pragma warning restore RS0030

            var uiDispatcher = new WpfUiDispatcher(context, dispatcher);
            ThreadAssert.Initialize(uiDispatcher);
            return uiDispatcher;
        }
    }
}
