#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
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
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System.Windows.Forms;
using Dapplo.Windows.Dpi;

#endregion

namespace GreenshotPlugin.Interfaces.Plugin
{
	/// <summary>
	///     This interface is the GreenshotPluginHost, that which "Hosts" the plugin.
	///     For Greenshot this is implmented in the PluginHelper
	/// </summary>
	public interface IGreenshotHost
	{
		ContextMenuStrip MainMenu { get; }

		// This is a reference to the MainForm, can be used for Invoking on the UI thread.
		Form GreenshotForm { get; }

		NotifyIcon NotifyIcon { get; }

		/// <summary>
		/// The DPI handler for the context menu, which should be used for the plugins too
		/// </summary>
		DpiHandler ContextMenuDpiHandler { get; }

        /// <summary>
        /// Initialize the form
        /// </summary>
	    void Initialize();
	}
}