/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using log4net;

namespace GreenshotFlickrPlugin
{
	/// <summary>
	/// This is the Flickr base code
	/// </summary>
	public class FlickrPlugin : IGreenshotPlugin {
		private static readonly ILog Log = LogManager.GetLogger(typeof(FlickrPlugin));
		private static FlickrConfiguration _config;
		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposing) {
				return;
			}
			if (_itemPlugInConfig == null) {
				return;
			}
			_itemPlugInConfig.Dispose();
			_itemPlugInConfig = null;
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
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute pluginAttribute) {
			_host = pluginHost;
			Attributes = pluginAttribute;


			// Register configuration (don't need the configuration itself)
			_config = IniConfig.GetIniSection<FlickrConfiguration>();
			_resources = new ComponentResourceManager(typeof(FlickrPlugin));

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = Language.GetString("flickr", LangKey.Configure),
				Tag = _host,
				Image = (Image) _resources.GetObject("flickr")
			};
			_itemPlugInConfig.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("flickr", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			Log.Debug("Flickr Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			_config.ShowConfigDialog();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs) {
			_config.ShowConfigDialog();
		}

		public bool Upload(ICaptureDetails captureDetails, ISurface surface, out string uploadUrl) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
			uploadUrl = null;
			try {
				string flickrUrl = null;
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("flickr", LangKey.communication_wait), 
					delegate {
						string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
						flickrUrl = FlickrUtils.UploadToFlickr(surface, outputSettings, captureDetails.Title, filename);
					}
				);
					
				if (flickrUrl == null) {
					return false;
				}
				uploadUrl = flickrUrl;

				if (_config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(flickrUrl);
				}
				return true;
			} catch (Exception e) {
				Log.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("flickr", LangKey.upload_failure) + " " + e.Message);
			}
			return false;
		}
	}
}
