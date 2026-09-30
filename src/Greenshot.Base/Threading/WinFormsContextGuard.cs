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
using System.Threading;
using System.Windows.Forms;

namespace Greenshot.Base.Threading
{
    /// <summary>
    /// Bridge until the imaging roadmap's document/view split: the pipeline still creates WinForms controls (the Surface, the text box
    /// of a TextContainer) while it owns them on a pool thread. Creating a control installs a WindowsFormsSynchronizationContext on the
    /// creating thread; on a pool thread without message loop, anything awaited there without ConfigureAwait(false) would never continue.
    /// This guard keeps pool threads free of that context. A control created on the pool must not get a handle there.
    /// </summary>
    public static class WinFormsContextGuard
    {
        /// <summary>
        /// Create the object (e.g. a Surface) without installing a WinForms SynchronizationContext on the current (pool) thread.
        /// </summary>
        public static T CreateWithoutContext<T>(Func<T> create)
        {
            if (create == null) throw new ArgumentNullException(nameof(create));
#pragma warning disable RS0030 // The guard needs to switch the WinForms auto install off
            bool previous = WindowsFormsSynchronizationContext.AutoInstall;
            WindowsFormsSynchronizationContext.AutoInstall = false;
            try
            {
                return create();
            }
            finally
            {
                WindowsFormsSynchronizationContext.AutoInstall = previous;
            }
#pragma warning restore RS0030
        }

        /// <summary>
        /// For a pool thread which runs pipeline code: never auto install a WinForms SynchronizationContext on it (AutoInstall is per thread),
        /// and remove one which leaked onto it. Does nothing on the UI thread.
        /// </summary>
        public static void ProtectPoolThread(IUiDispatcher ui)
        {
            if (ui == null || ui.CheckAccess() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return;
            }

#pragma warning disable RS0030 // The guard needs to switch the WinForms auto install off
            WindowsFormsSynchronizationContext.AutoInstall = false;
            if (SynchronizationContext.Current is WindowsFormsSynchronizationContext)
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
#pragma warning restore RS0030
        }
    }
}
