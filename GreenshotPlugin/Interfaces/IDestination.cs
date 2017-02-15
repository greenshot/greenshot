#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

#endregion

namespace GreenshotPlugin.Interfaces
{
	/// <summary>
	///     Description of IDestination.
	/// </summary>
	public interface IDestination : IDisposable, IComparable
	{
		/// <summary>
		///     Simple "designation" like "File", "Editor" etc, used to store the configuration
		/// </summary>
		string Designation { get; }

		/// <summary>
		///     Description which will be shown in the settings form, destination picker etc
		/// </summary>
		string Description { get; }

		/// <summary>
		///     Priority, used for sorting
		/// </summary>
		int Priority { get; }

		/// <summary>
		///     Gets an icon for the destination
		/// </summary>
		Image DisplayIcon { get; }

		/// <summary>
		///     Returns if the destination is active
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		///     Gets the ShortcutKeys for the Editor
		/// </summary>
		Keys EditorShortcutKeys { get; }

		/// <summary>
		///     Returns true if this destination can be dynamic
		/// </summary>
		bool IsDynamic { get; }

		/// <summary>
		///     Returns if the destination is active
		/// </summary>
		bool UseDynamicsOnly { get; }

		/// <summary>
		///     Returns true if this destination returns a link
		/// </summary>
		bool IsLinkable { get; }

		/// <summary>
		///     Return a menu item
		/// </summary>
		/// <param name="addDynamics">Resolve the dynamic destinations too?</param>
		/// <param name="menu">The menu for which the item is created</param>
		/// <param name="destinationClickHandler">Handler which is called when clicked</param>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem GetMenuItem(bool addDynamics, ContextMenuStrip menu, EventHandler destinationClickHandler);

		/// <summary>
		///     Gets the dynamic destinations
		/// </summary>
		IEnumerable<IDestination> DynamicDestinations();

		/// <summary>
		///     If a capture is made, and the destination is enabled, this method is called.
		/// </summary>
		/// <param name="manuallyInitiated">
		///     true if the user selected this destination from a GUI, false if it was called as part
		///     of a process
		/// </param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>DestinationExportInformation with information, like if the destination has "exported" the capture</returns>
		ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);
	}
}