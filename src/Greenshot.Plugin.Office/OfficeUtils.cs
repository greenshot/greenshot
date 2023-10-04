using System.Linq;
using Microsoft.Win32;

namespace Greenshot.Plugin.Office
{

    /// <summary>
    /// A small utility class for helping with office
    /// </summary>
    internal static class OfficeUtils
    {
        private static readonly string[] OfficeRootKeys = { @"SOFTWARE\Microsoft\Office", @"SOFTWARE\WOW6432Node\Microsoft\Office" };

        /// <summary>
        /// Get the path to the office exe
        /// </summary>
        /// <param name="exeName">Name of the office executable</param>
        public static string GetOfficeExePath(string exeName)
        {
            string strKeyName = exeName switch
            {
                "WINWORD.EXE" => "Word",
                "EXCEL.EXE" => "Excel",
                "POWERPNT.EXE" => "PowerPoint",
                "OUTLOOK.EXE" => "Outlook",
                "ONENOTE.EXE" => "OneNote",
                _ => ""
            };

            foreach (string strRootKey in OfficeRootKeys)
            {
                using RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(strRootKey);
                if (rootKey is null) continue;

                foreach (string officeVersion in rootKey.GetSubKeyNames().Where(r => r.Contains(".")).Reverse())
                {
                    using RegistryKey installRootKey = Registry.LocalMachine.OpenSubKey($@"{strRootKey}\{officeVersion}\{strKeyName}\InstallRoot");
                    if (installRootKey == null) continue;
                    return $@"{installRootKey.GetValue("Path")}\{exeName}";
                }
            }
            return null;
        }
    }
}