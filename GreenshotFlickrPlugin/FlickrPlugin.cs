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
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Microsoft.Win32;

namespace GreenshotFlickrPlugin {
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	public class FlickrPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FlickrPlugin));
		private const string CONFIG_FILENAME = "flickr-config.properties";
		private ILanguage lang = Language.GetInstance();
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private Properties config = new Properties();

		public FlickrPlugin() { }

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public void Initialize(IGreenshotPluginHost host, ICaptureHost captureHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			this.host = (IGreenshotPluginHost)host;
			this.captureHost = captureHost;
			this.myAttributes = myAttributes;
			
			host.OnImageEditorOpen += new OnImageEditorOpenHandler(ImageEditorOpened);
	
			// Make sure the MODI-DLLs are found by adding a resolver
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyAssemblyResolver);

			LoadConfig();
		}

		/// <summary>
		/// Implementation of the OnImageEditorOpen event
		/// Using the ImageEditor interface to register in the plugin menu
		/// </summary>
		private void ImageEditorOpened(object sender, ImageEditorOpenEventArgs eventArgs) {
			ToolStripMenuItem toolStripMenuItem = eventArgs.ImageEditor.GetPluginMenuItem();
			ToolStripMenuItem item = new ToolStripMenuItem();
			item.Text = "Upload to Flickr";
			item.Tag = eventArgs.ImageEditor;
			item.Click += new System.EventHandler(EditMenuClick);
			toolStripMenuItem.DropDownItems.Add(item);
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
			host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("This plugin doesn't have a configuration screen.");
			stringBuilder.AppendLine("Configuration is stored at: " + Path.Combine(host.ConfigurationPath, CONFIG_FILENAME));
			MessageBox.Show(stringBuilder.ToString());
		}

		/// <summary>
		/// This method helps to resolve the MODI DLL files
		/// </summary>
		/// <param name="sender">object which is starting the resolve</param>
		/// <param name="args">ResolveEventArgs describing the Assembly that needs to be found</param>
		/// <returns></returns>
		private Assembly MyAssemblyResolver(object sender, ResolveEventArgs args) {
			string dllPath = Path.GetDirectoryName(myAttributes.DllFile);
			string dllFilename = args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
			LOG.Debug("Resolving: " + dllFilename);
			if (dllFilename.StartsWith("Flickr")) {
				return Assembly.LoadFile(Path.Combine(dllPath, dllFilename));
			}
			return null;
		}

		private void LoadConfig() {
			string filename = Path.Combine(host.ConfigurationPath, CONFIG_FILENAME);
			if (File.Exists(filename)) {
				LOG.Debug("Loading configuration from: " + filename);
				config = Properties.read(filename);
			}
			bool changed = false;
			if (config == null) {
				config = new Properties();
				changed = true;
			}
			if (changed) {
				SaveConfig();
			}

		}

		private void SaveConfig() {
			string filename = Path.Combine(host.ConfigurationPath, CONFIG_FILENAME);
			LOG.Debug("Saving configuration to: " + filename);
			config.write(filename, "# The configuration file for the Greenshot Flickr Plugin");
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public void EditMenuClick(object sender, EventArgs eventArgs) {
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			IImageEditor imageEditor = (IImageEditor)item.Tag;
			FlickrUploadForm uploader = new FlickrUploadForm();
			DialogResult result = uploader.ShowDialog();
			if (result == DialogResult.OK) {
				using (MemoryStream stream = new MemoryStream()) {
					imageEditor.SaveToStream(stream, "PNG", 100);
					stream.Seek(0, SeekOrigin.Begin);
					try {
						uploader.upload(stream);
						MessageBox.Show(lang.GetString(LangKey.upload_success));
					} catch(Exception e) {
						LOG.Debug("Problem uploading", e);
						MessageBox.Show(lang.GetString(LangKey.upload_failure) + " " + e.Message);
					}
				}
			}
		}
	}
}