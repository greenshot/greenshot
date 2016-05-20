/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

namespace GreenshotDropboxPlugin {
	/// <summary>
	/// This is the Dropbox base code
	/// </summary>
	public class DropboxPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(DropboxPlugin));
		private static DropboxPluginConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost host;
		private ComponentResourceManager resources;
		private ToolStripMenuItem itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (itemPlugInConfig != null) {
					itemPlugInConfig.Dispose();
					itemPlugInConfig = null;
				}
			}
		}

		public DropboxPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new DropboxDestination(this);
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
			host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;

			// Register configuration (don't need the configuration itself)
			config = IniConfig.GetIniSection<DropboxPluginConfiguration>();
			resources = new ComponentResourceManager(typeof(DropboxPlugin));

			itemPlugInConfig = new ToolStripMenuItem();
			itemPlugInConfig.Text = Language.GetString("dropbox", LangKey.Configure);
			itemPlugInConfig.Tag = host;
			itemPlugInConfig.Click += new EventHandler(ConfigMenuClick);
			itemPlugInConfig.Image = (Image)resources.GetObject("Dropbox");

			PluginUtils.AddToContextMenu(host, itemPlugInConfig);
			Language.LanguageChanged += new LanguageChangedHandler(OnLanguageChanged);
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (itemPlugInConfig != null) {
				itemPlugInConfig.Text = Language.GetString("dropbox", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			LOG.Debug("Dropbox Plugin shutdown.");
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
			LOG.Debug("Application closing, de-registering Dropbox Plugin!");
			Shutdown();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs) {
			config.ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl) {
			uploadUrl = null;
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, false);
			try {
				string dropboxUrl = null;
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("dropbox", LangKey.communication_wait), 
					delegate() {
						string filename = Path.GetFileName(FilenameHelper.GetFilename(config.UploadFormat, captureDetails));
						dropboxUrl = DropboxUtils.UploadToDropbox(surfaceToUpload, outputSettings, filename);
					}
				);
				if (dropboxUrl == null) {
					return false;
				}
				uploadUrl = dropboxUrl;
				return true;
			} catch (Exception e) {
				LOG.Error(e);
				MessageBox.Show(Language.GetString("dropbox", LangKey.upload_failure) + " " + e.Message);
				return false;
			}
		}
	}
}
