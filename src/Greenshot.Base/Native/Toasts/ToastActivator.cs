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
using System.Runtime.InteropServices;
using log4net;

namespace Greenshot.Base.Native.Toasts
{
    /// <summary>
    /// Receives the clicks on Greenshot's toasts. Windows calls it through COM: in the running Greenshot, or it starts
    /// Greenshot with the command line registered for the CLSID (see <see cref="DesktopToasts.Register"/>) and calls it there.
    /// </summary>
    [ComVisible(true)]
    [Guid(Clsid)]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class ToastActivator : INotificationActivationCallback
    {
        public const string Clsid = "DD8B2638-A274-4988-9CC7-23A6D40BACEB";

        private static readonly ILog Log = LogManager.GetLogger(typeof(ToastActivator));

        /// <summary>
        /// Raised with the arguments of the clicked toast or button, on a COM thread
        /// </summary>
        public static event Action<string> Activated;

        /// <summary>
        /// Makes this process the one Windows calls for the clicks, until it exits
        /// </summary>
        public static void RegisterForClicks()
        {
            new RegistrationServices().RegisterTypeForComClients(typeof(ToastActivator), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse);
        }

        public void Activate(string appUserModelId, string invokedArgs, NotificationUserInputData[] data, uint count)
        {
            Log.DebugFormat("Toast activated with '{0}'", invokedArgs);
            Activated?.Invoke(invokedArgs);
        }
    }

    /// <summary>
    /// A value the user entered in the toast (Greenshot's toasts have no inputs)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NotificationUserInputData
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Key;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Value;
    }

    /// <summary>
    /// The COM interface Windows calls for a toast click
    /// </summary>
    [ComImport]
    [Guid("53E31837-6600-4A81-9395-75CFFE746F94")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INotificationActivationCallback
    {
        void Activate(
            [In, MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [In, MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] NotificationUserInputData[] data,
            [In, MarshalAs(UnmanagedType.U4)] uint count);
    }
}
