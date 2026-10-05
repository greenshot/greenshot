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


using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Greenshot.Ipc
{
    /// <summary>
    /// IPC commands of an optional part of Greenshot (browser extension, AI tools), registered with
    /// <see cref="Greenshot.Helpers.GreenshotModuleAttribute"/>. <see cref="IpcSecurityDispatcher"/> adds the commands to its whitelists
    /// and asks every extension about every request, so an extension can also restrict commands of other parts.
    /// </summary>
    internal interface IIpcCommandExtension
    {
        /// <summary>
        /// The commands this extension handles, added to the global whitelist
        /// </summary>
        IEnumerable<string> Commands { get; }

        /// <summary>
        /// Whitelists for the sources this extension brings (e.g. "native_messaging"): source to its allowed commands
        /// </summary>
        IReadOnlyDictionary<string, IEnumerable<string>> SourceCommands { get; }

        /// <summary>
        /// A veto after the global whitelist: false rejects the command for the source
        /// </summary>
        bool IsAllowedForSource(string command, string source);

        /// <summary>
        /// Checks after the whitelists (opt-in switches, the user's consent). Returns null when the request may run,
        /// otherwise the error for the client; the extension logs why itself.
        /// </summary>
        Task<string> CheckAccessAsync(string command, IpcRequestContext context);

        /// <summary>
        /// Handles one of <see cref="Commands"/>
        /// </summary>
        Task HandleAsync(string command, IpcRequestContext context, Form mainForm);
    }
}
