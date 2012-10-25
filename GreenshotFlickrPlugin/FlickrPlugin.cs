/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotFlickrPlugin
{
	/// <summary>
	/// This is the Flickr base code
	/// </summary>
	public class FlickrPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FlickrPlugin));
		private static FlickrConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost host;
		private ComponentResourceManager resources;
		private ToolStripMenuItem itemPlugInConfig;

		public FlickrPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new FlickrDestination(this);
		}


		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			this.host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;


			// Register configuration (don't need the configuration itself)
			config = IniConfig.GetIniSection<FlickrConfiguration>();
			resources = new ComponentResourceManager(typeof(FlickrPlugin));

			itemPlugInConfig = new ToolStripMenuItem();
			itemPlugInConfig.Text = Language.GetString("flickr", LangKey.Configure);
			itemPlugInConfig.Tag = host;
			itemPlugInConfig.Image = (Image)resources.GetObject("flickr");
			itemPlugInConfig.Click += new System.EventHandler(ConfigMenuClick);

			PluginUtils.AddToContextMenu(host, itemPlugInConfig);
			Language.LanguageChanged += new LanguageChangedHandler(OnLanguageChanged);
			return true;
		}

		public void OnLanguageChanged() {
			if (itemPlugInConfig != null) {
				itemPlugInConfig.Text = Language.GetString("flickr", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			LOG.Debug("Flickr Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			config.ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, de-registering Flickr Plugin!");
			Shutdown();
		}
	
		public void ConfigMenuClick(object sender, EventArgs eventArgs) {
			config.ShowConfigDialog();
		}

		public void Upload(ICaptureDetails captureDetails, Image image, ExportInformation exportInformation) {
			OutputSettings outputSettings = new OutputSettings(config.UploadFormat, config.UploadJpegQuality, false);
			try {
				string flickrUrl = null;
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("flickr", LangKey.communication_wait), 
					delegate() {
						string filename = Path.GetFileName(FilenameHelper.GetFilename(config.UploadFormat, captureDetails));
						flickrUrl = FlickrUtils.UploadToFlickr(image, outputSettings, captureDetails.Title, filename);
					}
				);
					
				if (flickrUrl == null) {
					exportInformation.ExportMade = false;
					return;
				}
				exportInformation.ExportMade = true;
				exportInformation.Uri = flickrUrl;

				if (config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(flickrUrl);
				}
			} catch (Exception e) {
				MessageBox.Show(Language.GetString("flickr", LangKey.upload_failure) + " " + e.Message);
			}
		}
	}
}
