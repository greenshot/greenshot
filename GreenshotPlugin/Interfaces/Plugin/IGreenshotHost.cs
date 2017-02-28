#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.Drawing;
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
		///     List of available plugins with their PluginAttributes
		///     This can be usefull for a plugin manager plugin...
		/// </summary>
		IDictionary<PluginAttribute, IGreenshotPlugin> Plugins { get; }

		/// <summary>
		///     Create a Thumbnail
		/// </summary>
		/// <param name="image">Image of which we need a Thumbnail</param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns>Image with Thumbnail</returns>
		Image GetThumbnail(Image image, int width, int height);

		/// <summary>
		///     Get a destination by it's designation
		/// </summary>
		/// <param name="designation"></param>
		/// <returns>IDestination</returns>
		IDestination GetDestination(string designation);

		/// <summary>
		///     Get a list of all available destinations
		/// </summary>
		/// <returns>List of IDestination</returns>
		List<IDestination> GetAllDestinations();

		/// <summary>
		///     Export a surface to the destination with has the supplied designation
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="designation"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		ExportInformation ExportCapture(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails);

		/// <summary>
		///     Make region capture with specified Handler
		/// </summary>
		/// <param name="captureMouseCursor">
		///     bool false if the mouse should not be captured, true if the configuration should be
		///     checked
		/// </param>
		/// <param name="destination">IDestination destination</param>
		void CaptureRegion(bool captureMouseCursor, IDestination destination);

		/// <summary>
		///     Use the supplied capture, and handle it as if it's captured.
		/// </summary>
		/// <param name="captureToImport">ICapture to import</param>
		void ImportCapture(ICapture captureToImport);

		/// <summary>
		///     Use the supplied image, and ICapture a capture object for it
		/// </summary>
		/// <param name="imageToCapture">Image to create capture for</param>
		/// <returns>ICapture</returns>
		ICapture GetCapture(Image imageToCapture);
	}
}