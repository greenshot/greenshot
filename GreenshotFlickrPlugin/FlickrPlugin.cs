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

using FlickrNet;
using Greenshot.Capturing;
using Greenshot.Core;
using Greenshot.Forms;
using Greenshot.Plugin;
using Microsoft.Win32;

namespace GreenshotFlickrPlugin {
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	public class FlickrPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FlickrPlugin));
		private const string CONFIG_FILENAME = "flickr-config.properties";
		private const string AUTHENTICATION_TOKEN_PROPERTY = "authentication.token";
		private const string ApiKey = "f967e5148945cb3c4e149cc5be97796a";
		private const string SharedSecret = "4180a21a1d2f8666";
		private ILanguage lang = Language.GetInstance();
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private Properties config = null;

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
			if (dllFilename.ToLower().StartsWith("flickr")) {
				return Assembly.LoadFile(Path.Combine(dllPath, dllFilename));
			}
			return null;
		}

		private void LoadConfig() {
			string filename = Path.Combine(host.ConfigurationPath, CONFIG_FILENAME);
			if (File.Exists(filename)) {
				config = Properties.read(filename);
			} else {
				LOG.Debug("No flickr configuration found at: " + filename);
			}
			if (config == null) {
				config = new Properties();
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
			bool authentication = false;
				
			Flickr flickr = new Flickr(ApiKey, SharedSecret);
			if(config.GetProperty(AUTHENTICATION_TOKEN_PROPERTY) == null) {
				string frob = flickr.AuthGetFrob();
				// Calculate the URL at Flickr to redirect the user to
				string flickrUrl = flickr.AuthCalcUrl(frob, AuthLevel.Write);
				FlickrAuthenticationForm authForm = new FlickrAuthenticationForm(flickrUrl);
				DialogResult authFormResult = authForm.ShowDialog();
				if (authFormResult == DialogResult.OK) {
					try {
						// use the temporary Frob to get the authentication
						Auth auth = flickr.AuthGetToken(frob);
						// Store this Token for later usage, 
						// or set your Flickr instance to use it.
						LOG.Debug("User authenticated successfully");
						LOG.Debug("Authentication token is " + auth.Token); 
						LOG.Debug("User id is " + auth.User.UserId);
						LOG.Debug("User name is " + auth.User.UserName);
						LOG.Debug("User fullname is " + auth.User.FullName);
						config.AddProperty(AUTHENTICATION_TOKEN_PROPERTY, auth.Token);
						SaveConfig();
						authentication = true;
					} catch (FlickrException ex) {
						// If user did not authenticat your application 
						// then a FlickrException will be thrown.
						LOG.Debug("User did not authenticate you", ex);
					}
				}
			} else {
				// Token is there
				authentication = true;
			}
			if (!authentication) {
				MessageBox.Show("No Authentication made!");
				return;
			}
			flickr.AuthToken = config.GetProperty(AUTHENTICATION_TOKEN_PROPERTY);
			FlickrUploadForm uploader = new FlickrUploadForm();
			DialogResult uploaderResult = uploader.ShowDialog();
			if (uploaderResult == DialogResult.OK) {
				using (MemoryStream stream = new MemoryStream()) {
					imageEditor.SaveToStream(stream, "PNG", 100);
					stream.Seek(0, SeekOrigin.Begin);
					try {
						string file = "test.png";
						string title = "Test Photo";
						string description = "This is the description of the photo";
						string tags = "greenshot,screenshot";
						string photoId = flickr.UploadPicture(stream, file, title, description, tags, false, false, false, ContentType.Screenshot, SafetyLevel.Restricted, HiddenFromSearch.Hidden);
					
						flickr.PhotosSetMeta(photoId, "Greenshot screenshot", "This is a greenshot screenshot!");
//						// Get list of users sets
//						PhotosetCollection sets = flickr.PhotosetsGetList();
//						string photosetId = null;
//						if (sets.Count == 0) {
//							LOG.Debug("Not photosets found, creating");
//							Photoset photoset = flickr.PhotosetsCreate("Greenshot", "Greenshot screenshots", photoId);
//							LOG.Debug("Created photoset with description: " + photoset.Description);
//							LOG.Debug("Created photoset with id: " + photoset.PhotosetId);
//							photosetId = photoset.PhotosetId;
//						} else {
//							// Get the first set in the collection
//							photosetId = sets[0].PhotosetId;
//						}
//						
//						// Add the photo to that set
//						flickr.PhotosetsAddPhoto(photosetId, photoId);
						MessageBox.Show(lang.GetString(LangKey.upload_success));
					} catch(Exception e) {
						LOG.Error("Problem uploading", e);
						MessageBox.Show(lang.GetString(LangKey.upload_failure) + " " + e.Message);
					}
				}
			}
		}
	}
}