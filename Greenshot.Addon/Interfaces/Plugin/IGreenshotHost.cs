/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Addon.Interfaces.Plugin
{
	/// <summary>
	/// This interface is the GreenshotPluginHost, that which "Hosts" the plugin.
	/// For Greenshot this is implmented in the PluginHelper
	/// </summary>
	public interface IGreenshotHost
	{
		ContextMenuStrip MainMenu
		{
			get;
		}

		// This is a reference to the MainForm, can be used for Invoking on the UI thread.
		Form GreenshotForm
		{
			get;
		}

		NotifyIcon NotifyIcon
		{
			get;
		}

		/// <summary>
		/// List of available plugins with their IGreenshotPluginMetadata
		/// This can be usefull for a plugin manager plugin...
		/// </summary>
		IEnumerable<Lazy<IGreenshotPlugin, IGreenshotPluginMetadata>> Plugins
		{
			get;
		}

		/// <summary>
		/// Get a destination by it's designation
		/// </summary>
		/// <param name="destination"></param>
		/// <returns>IDestination</returns>
		ILegacyDestination GetDestination(string designation);

		/// <summary>
		/// Get a list of all available destinations
		/// </summary>
		/// <returns>List<IDestination></returns>
		List<ILegacyDestination> GetAllDestinations();

		/// <summary>
		/// Use the supplied capture, and handle it as if it's captured.
		/// </summary>
		/// <param name="captureToImport">ICapture to import</param>
		void ImportCapture(ICapture captureToImport);

		/// <summary>
		/// Use the supplied image, and ICapture a capture object for it
		/// </summary>
		/// <param name="imageToCapture">Image to create capture for</param>
		/// <returns>ICapture</returns>
		ICapture GetCapture(Image imageToCapture);

		/// <summary>
		/// Show the Greenshot About
		/// </summary>
		void ShowAbout();

		/// <summary>
		/// Show the settings
		/// </summary>
		void ShowSettings();
	}
}