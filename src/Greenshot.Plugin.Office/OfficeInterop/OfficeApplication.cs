// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: https://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using Dapplo.Windows.Com;

namespace Greenshot.Plugin.Office.OfficeInterop
{
    /// <summary>
    /// The Office objects are used through our own small interfaces, which only list the members we call.
    /// They are declared with the IID of IDispatch: casting asks Office only for IDispatch, and every call is
    /// IDispatch.GetIDsOfNames (by member name) and IDispatch.Invoke. No Office type library is loaded, so a broken
    /// type library registration (TYPE_E_CANTLOADLIBRARY, 0x80029C4A) can't break the export, and no interop assemblies are needed.
    /// Collection items must be read with an Item method, an indexer doesn't work this way.
    /// Values Office returns are declared as the plain type it returns (int instead of an enum), enums are only passed in.
    /// </summary>
    internal static class OfficeApplication
    {
        /// <summary>
        /// The IID of IDispatch, used by all the Office interfaces
        /// </summary>
        public const string IDispatchIid = "00020400-0000-0000-C000-000000000046";

        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OfficeApplication));

        /// <summary>
        /// The running instance of the application, null if there is none
        /// </summary>
        /// <param name="progId">e.g. Word.Application</param>
        public static IDisposableCom<T> GetActive<T>(string progId) where T : class
        {
            try
            {
                return OleAut32Api.GetActiveObject<T>(progId);
            }
            catch (Exception ex)
            {
                LOG.Warn($"Unexpected error while getting the {progId} instance.", ex);
                return null;
            }
        }

        /// <summary>
        /// The running instance of the application, or a new one
        /// </summary>
        /// <param name="progId">e.g. Word.Application</param>
        public static IDisposableCom<T> GetOrCreate<T>(string progId) where T : class =>
            GetActive<T>(progId) ?? DisposableCom.Create((T) Activator.CreateInstance(Type.GetTypeFromProgID(progId, true)));
    }

    /// <summary>
    /// MsoTriState, the Office boolean
    /// </summary>
    public enum MsoTriState
    {
        msoTrue = -1,
        msoFalse = 0
    }

    /// <summary>
    /// MsoScaleFrom
    /// </summary>
    public enum MsoScaleFrom
    {
        msoScaleFromTopLeft = 0,
        msoScaleFromMiddle = 1
    }
}
