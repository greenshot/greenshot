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
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Plugin;

namespace PluginExample {
	/// <summary>
	/// An example Plugin so developers can see how they can develop their own plugin
	/// </summary>
	public class PluginExample : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PluginExample));
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private ExampleConfiguration config;

		public PluginExample() {
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public virtual void Initialize(IGreenshotPluginHost pluginHost, ICaptureHost captureHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			
			this.host = (IGreenshotPluginHost)pluginHost;
			this.captureHost = captureHost;
			this.myAttributes = myAttributes;

			this.host.OnImageEditorOpen += new OnImageEditorOpenHandler(ImageEditorOpened);
			this.host.OnSurfaceFromCapture += new OnSurfaceFromCaptureHandler(SurfaceFromCapture);
			this.host.OnImageOutput += new OnImageOutputHandler(ImageOutput);
			
			this.config = IniConfig.GetIniSection<ExampleConfiguration>();
		}

		public virtual void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
			this.host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
			this.host.OnSurfaceFromCapture -= new OnSurfaceFromCaptureHandler(SurfaceFromCapture);
			this.host.OnImageOutput -= new OnImageOutputHandler(ImageOutput);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			LOG.Debug("Configure called");
			SettingsForm settingsForm = new SettingsForm(config.AddAnnotations);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				config.AddAnnotations = settingsForm.TestAnnotations;
			}
			settingsForm.Dispose();
		}
		
		/// <summary>
		/// Implementation of the OnImageEditorOpen event
		/// Using the ImageEditor interface to register in the plugin menu
		/// </summary>
		private void ImageEditorOpened(object sender, ImageEditorOpenEventArgs eventArgs) {
			LOG.Debug("ImageEditorOpened called");
			ToolStripMenuItem toolStripMenuItem = eventArgs.ImageEditor.GetPluginMenuItem();
			ToolStripMenuItem saveItem = new ToolStripMenuItem();
			saveItem.Text = "Save with Plugin";
			saveItem.Tag = eventArgs.ImageEditor;
			saveItem.Click += new System.EventHandler(EditMenuClick);
			toolStripMenuItem.DropDownItems.Add(saveItem);

			ToolStripMenuItem grayscaleItem = new ToolStripMenuItem();
			grayscaleItem.Text = "Turn bitmap into gray-scales";
			grayscaleItem.Tag = eventArgs.ImageEditor;
			grayscaleItem.Click += new System.EventHandler(GrayScaleMenuClick);
			toolStripMenuItem.DropDownItems.Add(grayscaleItem);
		}
		
		private void MainMenuClick(object sender, EventArgs e) {
			LOG.Debug("Sympathy for the Devil!");
			Bitmap bitmap = new Bitmap(100, 100, PixelFormat.Format16bppRgb555);
			captureHost.HandleCapture(bitmap);
			bitmap.Dispose();
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// Just an example...
		/// </summary>
		private void EditMenuClick(object sender, EventArgs e) {
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			IImageEditor imageEditor = (IImageEditor)item.Tag;
			
			string file = host.GetFilename(OutputFormat.Png, null);
			string filePath = Path.Combine(host.ConfigurationPath,file);
			using (FileStream stream = new FileStream(filePath, FileMode.Create)) {
				imageEditor.SaveToStream(stream, OutputFormat.Png, 100);
			}
			LOG.Debug("Saved test file to: " + filePath);
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// Just an example...
		/// </summary>
		private void GrayScaleMenuClick(object sender, EventArgs e) {
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			IImageEditor imageEditor = (IImageEditor)item.Tag;
			ISurface surface = imageEditor.Surface;
			// copy & do something with the image
			Image image = (Image)surface.OriginalImage.Clone();

			using (BitmapBuffer bbb = new BitmapBuffer(image as Bitmap)) {
				// Image is copied, dispose it!
				image.Dispose();
				
				bbb.Lock();
				for(int y=0;y<bbb.Height; y++) {
					for(int x=0;x<bbb.Width; x++) {
						Color color = bbb.GetColorAt(x, y);
						int luma  = (int)((0.3*color.R) + (0.59*color.G) + (0.11*color.B));
						color = Color.FromArgb(luma, luma, luma);
						bbb.SetColorAt(x, y, color);
					}
				}
				surface.Image = bbb.Bitmap;
			}
		}

	
		/// <summary>
		/// Handling of the OnImageOutputHandler event from the IGreenshotPlugin
		/// </summary>
		/// <param name="ImageOutputEventArgs">Has the FullPath to the image</param>
		private void ImageOutput(object sender, ImageOutputEventArgs eventArgs) {
			LOG.Debug("ImageOutput called with full path: " + eventArgs.FullPath);
		}

		
		/// <summary>
		/// Handling of the OnSurfaceFromCapture event from the IGreenshotPlugin
		/// </summary>
		/// <param name="SurfaceFromCaptureEventArgs">Has the ICapture and ISurface</param>
		public void SurfaceFromCapture(object sender, SurfaceFromCaptureEventArgs eventArgs) {
			LOG.Debug("SurfaceFromCapture called");
			if (!config.AddAnnotations) {
				return;
			}
			ISurface surface = eventArgs.Surface;
			
			// Example, add a Cursor
			//surface.SelectElement(surface.AddCursorContainer(Cursors.Hand, 100, 100));

			DateTime captureTaken = DateTime.Now;
			// Use default application name for title
			string title = Application.ProductName;

			// Check if we have capture details
			if ( eventArgs.Capture.CaptureDetails != null) {
				captureTaken = eventArgs.Capture.CaptureDetails.DateTime;
				if (eventArgs.Capture.CaptureDetails.Title != null) {
					title = eventArgs.Capture.CaptureDetails.Title;
				}
			}
			foreach(Location location in config.annotations.Keys) {
				string pattern = config.annotations[location];
				pattern = pattern.Replace("%YYYY%",captureTaken.Year.ToString());
				pattern = pattern.Replace("%MM%", zeroPad(captureTaken.Month.ToString(), 2));
				pattern = pattern.Replace("%DD%", zeroPad(captureTaken.Day.ToString(), 2));
				pattern = pattern.Replace("%hh%", zeroPad(captureTaken.Hour.ToString(), 2));
				pattern = pattern.Replace("%mm%", zeroPad(captureTaken.Minute.ToString(), 2));
				pattern = pattern.Replace("%ss%", zeroPad(captureTaken.Second.ToString(), 2));
				pattern = pattern.Replace("%domain%", Environment.UserDomainName);
				pattern = pattern.Replace("%user%", Environment.UserName);
				pattern = pattern.Replace("%hostname%", Environment.MachineName);
				pattern = pattern.Replace("%title%", title);
				HorizontalAlignment hAlign = HorizontalAlignment.Center;
				VerticalAlignment vAlign = VerticalAlignment.CENTER;
				switch(location) {
					case Location.TopLeft:
						hAlign = HorizontalAlignment.Left;
						vAlign = VerticalAlignment.TOP;
						break;
					case Location.TopCenter:
						hAlign = HorizontalAlignment.Center;
						vAlign = VerticalAlignment.TOP;
						break;
					case Location.TopRight:
						hAlign = HorizontalAlignment.Right;
						vAlign = VerticalAlignment.TOP;
						break;

					case Location.CenterLeft:
						hAlign = HorizontalAlignment.Left;
						vAlign = VerticalAlignment.CENTER;
						break;
					case Location.CenterCenter:
						hAlign = HorizontalAlignment.Center;
						vAlign = VerticalAlignment.CENTER;
						break;
					case Location.CenterRight:
						hAlign = HorizontalAlignment.Right;
						vAlign = VerticalAlignment.CENTER;
						break;

					case Location.BottomLeft:
						hAlign = HorizontalAlignment.Left;
						vAlign = VerticalAlignment.BOTTOM;
						break;
					case Location.BottomCenter:
						hAlign = HorizontalAlignment.Center;
						vAlign = VerticalAlignment.BOTTOM;
						break;
					case Location.BottomRight:
						hAlign = HorizontalAlignment.Right;
						vAlign = VerticalAlignment.BOTTOM;
						break;
				}
				surface.SelectElement(surface.AddTextContainer(pattern, hAlign, vAlign,
				                                               FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Red, Color.White));
			}
		}
				
		private static string zeroPad(string input, int chars) {
			while(input.Length < chars) input = "0" + input;
			return input;
		}
	}
}