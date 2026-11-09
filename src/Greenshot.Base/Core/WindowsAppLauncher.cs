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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using log4net;
using Windows.Management.Deployment;

namespace Greenshot.Base.Core;

/// <summary>
/// Opens a file with a Windows App (MSIX/Store) by its app id (AppUserModelID), the way Explorer's "Open with" does. For an app
/// without a command-line name (App Execution Alias), whose program lies in a folder which changes with each update of the app.
/// The program of such a command is written <c>shell:AppsFolder\&lt;app id&gt;</c>, like a shortcut to the app.
/// </summary>
public static class WindowsAppLauncher
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsAppLauncher));

    /// <summary>
    /// The start of a program which is a Windows App
    /// </summary>
    public const string AppsFolderPrefix = @"shell:AppsFolder\";

    /// <summary>
    /// True when the program is a Windows App: shell:AppsFolder\ and an app id (package family name!application id)
    /// </summary>
    public static bool IsAppProgram(string program) => AppIdOf(program) != null;

    /// <summary>
    /// The app id of the program, null when it isn't a Windows App
    /// </summary>
    public static string AppIdOf(string program)
    {
        if (program == null || !program.StartsWith(AppsFolderPrefix, StringComparison.OrdinalIgnoreCase)) return null;
        string appId = program.Substring(AppsFolderPrefix.Length).Trim();
        int separator = appId.IndexOf('!');
        return separator > 0 && separator < appId.Length - 1 && appId.IndexOfAny(new[] { '\\', '/', '"', ' ' }) < 0 ? appId : null;
    }

    /// <summary>
    /// The program for the app id
    /// </summary>
    public static string ProgramOf(string appId) => appId != null && AppIdOf(AppsFolderPrefix + appId) != null ? AppsFolderPrefix + appId : null;

    /// <summary>
    /// True when the package of the app is installed for the user
    /// </summary>
    public static bool IsInstalled(string appId)
    {
        if (string.IsNullOrEmpty(appId)) return false;
        string family = appId.Substring(0, appId.IndexOf('!') is var index && index > 0 ? index : appId.Length);
        try
        {
            return new PackageManager().FindPackagesForUser(string.Empty, family).Any();
        }
        catch (Exception ex)
        {
            Log.Debug($"Can't look for the package {family}", ex);
            return false;
        }
    }

    /// <summary>
    /// Open the file with the app, it isn't waited for
    /// </summary>
    /// <returns>the id of the process of the app</returns>
    /// <exception cref="Win32Exception">the app can't be started with the file, e.g. it isn't installed</exception>
    public static uint OpenFile(string appId, string file)
    {
        if (string.IsNullOrEmpty(appId)) throw new ArgumentNullException(nameof(appId));
        if (string.IsNullOrEmpty(file)) throw new ArgumentNullException(nameof(file));
        IntPtr item = IntPtr.Zero;
        IntPtr items = IntPtr.Zero;
        try
        {
            var shellItemId = typeof(IShellItem).GUID;
            Check(SHCreateItemFromParsingName(file, IntPtr.Zero, ref shellItemId, out item), file);
            var arrayId = typeof(IShellItemArray).GUID;
            Check(SHCreateShellItemArrayFromShellItem(item, ref arrayId, out items), file);
            var manager = (IApplicationActivationManager)new ApplicationActivationManager();
            try
            {
                // "open": the verb of a file type association of every app
                Check(manager.ActivateForFile(appId, items, "open", out uint processId), appId);
                Log.InfoFormat("Opened {0} with the Windows App {1} (process {2})", file, appId, processId);
                return processId;
            }
            finally
            {
                Marshal.ReleaseComObject(manager);
            }
        }
        finally
        {
            if (items != IntPtr.Zero) Marshal.Release(items);
            if (item != IntPtr.Zero) Marshal.Release(item);
        }
    }

    private static void Check(int hresult, string what)
    {
        if (hresult < 0)
        {
            throw new Win32Exception(hresult, $"{what}: {Marshal.GetExceptionForHR(hresult)?.Message}");
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(string path, IntPtr bindContext, ref Guid interfaceId, out IntPtr item);

    [DllImport("shell32.dll")]
    private static extern int SHCreateShellItemArrayFromShellItem(IntPtr item, ref Guid interfaceId, out IntPtr items);

    [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
    }

    [ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
    }

    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId, [MarshalAs(UnmanagedType.LPWStr)] string arguments, int options, out uint processId);

        [PreserveSig]
        int ActivateForFile([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId, IntPtr itemArray, [MarshalAs(UnmanagedType.LPWStr)] string verb, out uint processId);

        [PreserveSig]
        int ActivateForProtocol([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId, IntPtr itemArray, out uint processId);
    }

    [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    private class ApplicationActivationManager
    {
    }
}
