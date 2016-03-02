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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Greenshot.Addon.Interfaces
{

	/// <summary>
	/// Description of IDestination.
	/// </summary>
	public interface ILegacyDestination : IDisposable, IComparable
	{
		/// <summary>
		/// Simple "designation" like "File", "Editor" etc, used to store the configuration
		/// </summary>
		string Designation
		{
			get;
		}

		/// <summary>
		/// Description which will be shown in the settings form, destination picker etc
		/// </summary>
		string Description
		{
			get;
		}

		/// <summary>
		/// Priority, used for sorting
		/// </summary>
		int Priority
		{
			get;
		}

		/// <summary>
		/// Gets an icon for the destination
		/// </summary>
		Image DisplayIcon
		{
			get;
		}

		/// <summary>
		/// Returns if the destination is active
		/// </summary>
		bool IsActive
		{
			get;
		}

		/// <summary>
		/// Return a menu item
		/// </summary>
		/// <param name="addDynamics">Resolve the dynamic destinations too?</param>
		/// <param name="destinationClickHandler">Handler which is called when clicked</param>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem CreateMenuItem(bool addDynamics, EventHandler destinationClickHandler);

		/// <summary>
		/// Gets the ShortcutKeys for the Editor
		/// </summary>
		Keys EditorShortcutKeys
		{
			get;
		}

		/// <summary>
		/// Gets the dynamic destinations
		/// </summary>
		IEnumerable<ILegacyDestination> DynamicDestinations();

		/// <summary>
		/// Returns true if this destination can be dynamic
		/// </summary>
		bool IsDynamic
		{
			get;
		}

		/// <summary>
		/// Async version of the ExportCapture
		/// </summary>
		/// <param name="manuallyInitiated">true if the user selected this destination from a GUI, false if it was called as part of a process</param>
		/// <param name="capture">ICapture</param>
		/// <param name="token"></param>
		/// <returns>DestinationExportInformation with information, like if the destination has "exported" the capture</returns>
		Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ICapture capture, CancellationToken token = default(CancellationToken));
	}
}