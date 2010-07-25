/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Capturing;
using Greenshot.Drawing;
using Greenshot.Forms;

namespace Greenshot.Plugin {
	[Serializable]
	[AttributeUsageAttribute(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	sealed public class PluginAttribute : Attribute {
		public string Name {
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
	}

	#region EventArgs
	[Serializable]
	public class ImageEditorOpenEventArgs : EventArgs {  
		private readonly IImageEditor imageEditor;
		public IImageEditor ImageEditor {     
			get { return imageEditor;}      
		}
		public ImageEditorOpenEventArgs(IImageEditor imageEditor) {
			this.imageEditor = imageEditor;
		}
	}

	[Serializable]
	public class CaptureTakenEventArgs : EventArgs {
		private readonly ICapture capture;
		public ICapture Capture {     
			get { return capture;}      
		}
		public CaptureTakenEventArgs(ICapture capture) {
			this.capture = capture;
		}
	}

	[Serializable]
	public class SurfaceFromCaptureEventArgs : EventArgs {
		private readonly ICapture capture;
		public ICapture Capture {     
			get { return capture;}      
		}
		private readonly ISurface surface;
		public ISurface Surface {     
			get { return surface;}      
		}
		public SurfaceFromCaptureEventArgs(ICapture capture, ISurface surface) {
			this.capture = capture;
			this.surface = surface;
		}
	}

	[Serializable]
	public class ImageOutputEventArgs : EventArgs {
		private readonly string fullPath;
		public string FullPath {     
			get { return fullPath;}      
		}

		private readonly Image image;
		public Image Image {     
			get { return image;}      
		}

		private readonly ICaptureDetails captureDetails;
		public ICaptureDetails CaptureDetails {     
			get { return captureDetails;}      
		}

		public ImageOutputEventArgs(string fullPath, Image image, ICaptureDetails captureDetails) {
			this.image = image;
			this.fullPath = fullPath;
			this.captureDetails = captureDetails;
		}
	}	
	#endregion

	// Delegates for hooking up events.
	public delegate void OnImageEditorOpenHandler(object sender, ImageEditorOpenEventArgs e);
	public delegate void OnCaptureTakenHandler(object sender, CaptureTakenEventArgs e);
	public delegate void OnSurfaceFromCaptureHandler(object sender, SurfaceFromCaptureEventArgs e);
	public delegate void OnImageOutputHandler(object sender, ImageOutputEventArgs e);
	public delegate void HotKeyHandler();

	/// <summary>
	/// This interface is the GreenshotPluginHost, that which "Hosts" the plugin.
	/// For Greenshot this is implmented in the PluginHelper
	/// </summary>
	public interface IGreenshotPluginHost {
		/// The Plugin can register to be called after every a newly opened ImageEditor
		/// and will be passed the IImageEditor interface so it can register itself e.g. in the Menu
		event OnImageEditorOpenHandler OnImageEditorOpen;

		/// The Plugin can register to be called after every take Capture
		/// and will be passed the ICapture interface so it can do something with it
		event OnCaptureTakenHandler OnCaptureTaken;

		/// The Plugin can register to be called after a Surface is created from a Capture
		/// and will be passed the ICapture and ISurface interfaces so it can do something with it
		event OnSurfaceFromCaptureHandler OnSurfaceFromCapture;

		/// The Plugin can register to be called when an image is written to a file
		/// and will be passed the full path to the file
		event OnImageOutputHandler OnImageOutput;		

		/// <summary>
		/// Return the location of the configuration, if any
		/// </summary>
		string ConfigurationPath {
			get;
		}
		
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
		void SaveToStream(Image image, Stream stream, string format, int quality);
		
		/// <summary>
		/// Return a filename for the current image format (png,jpg etc) with the default file pattern
		/// that is specified in the configuration
		/// </summary>
		/// <param name="format">A string with the format</param>
		/// <returns>The filename which should be used to save the image</returns>
		string GetFilename(string format, ICaptureDetails captureDetails);
		
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
		Dictionary<PluginAttribute, IGreenshotPlugin> Plugins {
			get;
		}
		
		/// <summary>
		/// Register Hotkey handler!
		/// </summary>
		/// <param name="modifierKeyCode"></param>
		/// <param name="virtualKeyCode"></param>
		/// <param name="handler"></param>
		/// <returns>bool true if all went okay</returns>
		bool RegisterHotKey(int modifierKeyCode, int virtualKeyCode, HotKeyHandler handler);
	}

	public interface IGreenshotPlugin {
		/// <summary>
		/// Is called after the plugin is instanciated, the Plugin should keep a copy of the host and pluginAttribute.
		/// </summary>
		/// <param name="host">The IPluginHost that will be hosting the plugin</param>
		/// <param name="pluginAttribute">The PluginAttribute for the actual plugin</param>
		void Initialize(IGreenshotPluginHost host, ICaptureHost captureHost, PluginAttribute pluginAttribute);

		/// <summary>
		/// Unload of the plugin
		/// </summary>
		void Shutdown();

		/// <summary>
		/// Open the Configuration Form, will/should not be called before handshaking is done
		/// </summary>
		void Configure();
	}
}