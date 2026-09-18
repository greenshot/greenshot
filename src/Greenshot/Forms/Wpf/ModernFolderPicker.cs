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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Greenshot.Forms.Wpf
{
    /// <summary>
    /// Invokes the modern Windows Vista/10/11 Explorer folder picker dialog via IFileOpenDialog.
    /// Falls back to System.Windows.Forms.FolderBrowserDialog if COM creation is unavailable.
    /// </summary>
    public static class ModernFolderPicker
    {
        public static string SelectFolder(Window owner, string initialDirectory, string title = null)
        {
            IntPtr ownerHwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

            try
            {
                var dialog = (IFileOpenDialog)new FileOpenDialogRCW();
                try
                {
                    const uint FOS_PICKFOLDERS = 0x00000020;
                    const uint FOS_FORCEFILESYSTEM = 0x00000040;
                    const uint FOS_NOVALIDATE = 0x00000100;
                    const uint FOS_PATHMUSTEXIST = 0x00000800;

                    dialog.GetOptions(out uint options);
                    dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOVALIDATE);

                    if (!string.IsNullOrEmpty(title))
                    {
                        dialog.SetTitle(title);
                    }

                    if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    {
                        int hr = SHCreateItemFromParsingName(initialDirectory, IntPtr.Zero, typeof(IShellItem).GUID, out IShellItem folderItem);
                        if (hr == 0 && folderItem != null)
                        {
                            dialog.SetFolder(folderItem);
                        }
                    }

                    int showResult = dialog.Show(ownerHwnd);
                    if (showResult == 0) // S_OK
                    {
                        dialog.GetResult(out IShellItem resultItem);
                        if (resultItem != null)
                        {
                            resultItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string path);
                            if (!string.IsNullOrEmpty(path))
                            {
                                return path;
                            }
                        }
                    }

                    return null;
                }
                finally
                {
                    Marshal.ReleaseComObject(dialog);
                }
            }
            catch
            {
                // Fallback to WinForms FolderBrowserDialog
                using (var fallback = new System.Windows.Forms.FolderBrowserDialog())
                {
                    if (!string.IsNullOrEmpty(title))
                    {
                        fallback.Description = title;
                    }

                    if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    {
                        fallback.SelectedPath = initialDirectory;
                    }

                    var win32Owner = new Win32WindowWrapper(ownerHwnd);
                    try
                    {
                        if (fallback.ShowDialog(win32Owner) == System.Windows.Forms.DialogResult.OK)
                        {
                            return fallback.SelectedPath;
                        }
                    }
                    finally
                    {
                        win32Owner.Dispose();
                    }
                }

                return null;
            }
        }

        private class Win32WindowWrapper : System.Windows.Forms.IWin32Window, IDisposable
        {
            public IntPtr Handle { get; }

            public Win32WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public void Dispose() { }
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        [ClassInterface(ClassInterfaceType.None)]
        private class FileOpenDialogRCW { }

        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show([In] IntPtr parent);
            void SetFileTypes([In] uint cFileTypes, [In] IntPtr rgFilterSpec);
            void SetFileTypeIndex([In] uint iFileType);
            void GetFileTypeIndex([Out] out uint piFileType);
            void Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, [Out] out uint pdwCookie);
            void Unadvise([In] uint dwCookie);
            void SetOptions([In] uint fos);
            void GetOptions([Out] out uint pfos);
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void GetFolder([Out, MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetCurrentSelection([Out, MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([Out, MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult([Out, MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint fdap);
            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close([In, MarshalAs(UnmanagedType.Error)] int hr);
            void SetClientGuid([In] ref Guid guid);
            void ClearClientData();
            void SetFilter([In, MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler([In] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, [Out] out IntPtr ppv);
            void GetParent([Out, MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetDisplayName([In] SIGDN sigdnName, [Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes([In] uint sfgaoMask, [Out] out uint psfgaoAttribs);
            void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, [Out] out int piOrder);
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    }
}
