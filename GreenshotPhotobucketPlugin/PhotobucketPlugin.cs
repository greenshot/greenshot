/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotPhotobucketPlugin {
	/// <summary>
	/// This is the GreenshotPhotobucketPlugin base code
	/// </summary>
	public class PhotobucketPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(PhotobucketPlugin));
		private static PhotobucketConfiguration _config;
		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_itemPlugInConfig != null) {
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
		}

		public PhotobucketPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new PhotobucketDestination(this, null);
		}

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			_host = pluginHost;
			Attributes = myAttributes;

			// Get configuration
			_config = IniConfig.GetIniSection<PhotobucketConfiguration>();
			_resources = new ComponentResourceManager(typeof(PhotobucketPlugin));

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("photobucket", LangKey.configure))
			{
				Tag = _host,
				Image = (Image)_resources.GetObject("Photobucket")
			};
			_itemPlugInConfig.Click += delegate {
				_config.ShowConfigDialog();
			};

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("photobucket", LangKey.configure);
			}
		}

		public virtual void Shutdown() {
			Log.Debug("Photobucket Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			_config.ShowConfigDialog();
		}

		/// <summary>
		/// Upload the capture to Photobucket
		/// </summary>
		/// <param name="captureDetails"></param>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="albumPath">Path to the album</param>
		/// <param name="uploadUrl">out string for the url</param>
		/// <returns>true if the upload succeeded</returns>
		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, string albumPath, out string uploadUrl) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, _config.UploadReduceColors);
			try {
				string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
				PhotobucketInfo photobucketInfo = null;
			
				// Run upload in the background
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("photobucket", LangKey.communication_wait), 
					delegate {
						photobucketInfo = PhotobucketUtils.UploadToPhotobucket(surfaceToUpload, outputSettings, albumPath, captureDetails.Title, filename);
					}
				);
				// This causes an exeption if the upload failed :)
				Log.DebugFormat("Uploaded to Photobucket page: " + photobucketInfo.Page);
				uploadUrl = null;
				try {
					if (_config.UsePageLink) {
						uploadUrl = photobucketInfo.Page;
						Clipboard.SetText(photobucketInfo.Page);
					} else {
						uploadUrl = photobucketInfo.Original;
						Clipboard.SetText(photobucketInfo.Original);
					}
				} catch (Exception ex) {
					Log.Error("Can't write to clipboard: ", ex);
				}
				return true;
			} catch (Exception e) {
				Log.Error(e);
				MessageBox.Show(Language.GetString("photobucket", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}
