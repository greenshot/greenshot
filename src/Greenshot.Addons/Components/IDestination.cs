// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Dpi;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addons.Components
{
	/// <summary>
	///     Description of IDestination.
	/// </summary>
	public interface IDestination : IDisposable
	{
	    /// <summary>
	    ///     Designation to uniquely identify the destination
	    /// </summary>
	    string Designation { get; }

        /// <summary>
        ///     Description which will be shown in the settings form, destination picker etc
        /// </summary>
        string Description { get; }

		/// <summary>
		///     Gets an icon for the destination
		/// </summary>
        IBitmapWithNativeSupport DisplayIcon { get; }

	    /// <summary>
	    ///     Gets an icon for the destination
	    /// </summary>
	    BitmapSource DisplayIconWpf { get; }

        /// <summary>
        ///     Gets an icon for the destination, optionally it's already scaled
        /// </summary>
        IBitmapWithNativeSupport GetDisplayIcon(double dpi);

		/// <summary>
		/// Returns if there is a displayIcon
		/// </summary>
		bool HasDisplayIcon { get; }

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
		/// <param name="bitmapScaleHandler">BitmapScaleHandler can be used for scaling icons</param>
		/// <returns>ToolStripMenuItem</returns>
		ToolStripMenuItem GetMenuItem(bool addDynamics, ContextMenuStrip menu, EventHandler destinationClickHandler, BitmapScaleHandler<IDestination, IBitmapWithNativeSupport> bitmapScaleHandler);

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
		Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);
	}
}