/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System.IO;
using System.Windows.Forms;

using GreenshotPlugin.Core;

namespace Greenshot.Plugin {
	[Serializable]
	[AttributeUsageAttribute(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	sealed public class PluginAttribute : Attribute, IComparable {
		public string Name {
			get;
			set;
		}
		public string CreatedBy {
			get;
			set;
		}
		public string Version {
			get;
			set;
		}
		public string EntryType {
			get;
			private set;
		}
		public bool Configurable {
			get;
			private set;
		}
		
		public string DllFile {
			get;
			set;
		}

		public PluginAttribute(string entryType, bool configurable) {
			this.EntryType = entryType;
			this.Configurable = configurable;
		}
		
		public int CompareTo(object obj) {
			PluginAttribute other = obj as PluginAttribute;
			if (other != null) {
				return Name.CompareTo(other.Name);
			}
			throw new ArgumentException("object is not a PluginAttribute");
		}
	}

	// Delegates for hooking up events.
	public delegate void HotKeyHandler();

	/// <summary>
	/// This interface is the GreenshotPluginHost, that which "Hosts" the plugin.
	/// For Greenshot this is implmented in the PluginHelper
	/// </summary>
	public interface IGreenshotHost {
		ContextMenuStrip MainMenu {
			get;
		}
		
		/// <summary>
		/// Saves the image to the supplied stream using the specified extension as the format
		/// </summary>
		/// <param name="image">The Image to save</param>
		/// <param name="stream">The Stream to save to</param>
		/// <param name="format">The format to save with (png, jpg etc)</param>
		/// <param name="quality">Jpeg quality</param>
		/// <param name="reduceColors">reduce the amount of colors to 256</param>
		void SaveToStream(Image image, Stream stream, OutputFormat format, int quality, bool reduceColors);

		/// <summary>
		/// Saves the image to a temp file (random name) using the specified outputformat
		/// </summary>
		/// <param name="image">The Image to save</param>
		/// <param name="format">The format to save with (png, jpg etc)</param>
		/// <param name="quality">Jpeg quality</param>
		/// <param name="reduceColors">reduce the amount of colors to 256</param>
		string SaveToTmpFile(Image image, OutputFormat outputFormat, int quality, bool reduceColors);

		/// <summary>
		/// Saves the image to a temp file, but the name is build with the capture details & pattern
		/// </summary>
		/// <param name="image">The Image to save</param>
		/// <param name="captureDetails">captureDetails with the information to build the filename</param>
		/// <param name="outputformat">The format to save with (png, jpg etc)</param>
		/// <param name="quality">Jpeg quality</param>
		/// <param name="reduceColors">reduce the amount of colors to 256</param>
		string SaveNamedTmpFile(Image image, ICaptureDetails captureDetails, OutputFormat outputFormat, int quality, bool reduceColors);

		/// <summary>
		/// Return a filename for the current image format (png,jpg etc) with the default file pattern
		/// that is specified in the configuration
		/// </summary>
		/// <param name="format">A string with the format</param>
		/// <returns>The filename which should be used to save the image</returns>
		string GetFilename(OutputFormat format, ICaptureDetails captureDetails);
		
		/// <summary>
		/// Create a Thumbnail
		/// </summary>
		/// <param name="image">Image of which we need a Thumbnail</param>
		/// <returns>Image with Thumbnail</returns>
		Image GetThumbnail(Image image, int width, int height);
		
		/// <summary>
		/// List of available plugins with their PluginAttributes
		/// This can be usefull for a plugin manager plugin...
		/// </summary>
		IDictionary<PluginAttribute, IGreenshotPlugin> Plugins {
			get;
		}
		
		/// <summary>
		/// Make region capture with specified Handler
		/// </summary>
		/// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
		/// <param name="destination">IDestination destination</param>
		void CaptureRegion(bool captureMouseCursor, IDestination destination);

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
	}

	public interface IGreenshotPlugin {
		/// <summary>
		/// Is called after the plugin is instanciated, the Plugin should keep a copy of the host and pluginAttribute.
		/// </summary>
		/// <param name="host">The IPluginHost that will be hosting the plugin</param>
		/// <param name="pluginAttribute">The PluginAttribute for the actual plugin</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		bool Initialize(IGreenshotHost host, PluginAttribute pluginAttribute);

		/// <summary>
		/// Unload of the plugin
		/// </summary>
		void Shutdown();

		/// <summary>
		/// Open the Configuration Form, will/should not be called before handshaking is done
		/// </summary>
		void Configure();
		
		/// <summary>
		/// Return IDestination's, if the plugin wants to
		/// </summary>
		IEnumerable<IDestination> Destinations();

		/// <summary>
		/// Return IProcessor's, if the plugin wants to
		/// </summary>
		IEnumerable<IProcessor> Processors();
	}
}