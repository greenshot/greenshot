/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

namespace GreenshotDropboxPlugin {
	/// <summary>
	/// This is the Dropbox base code
	/// </summary>
	public class DropboxPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DropboxPlugin));
		private static DropboxPluginConfiguration _config;
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

		public IEnumerable<IDestination> Destinations() {
			yield return new DropboxDestination(this);
		}


		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			_host = pluginHost;
			Attributes = myAttributes;

			// Register configuration (don't need the configuration itself)
			_config = IniConfig.GetIniSection<DropboxPluginConfiguration>();
			_resources = new ComponentResourceManager(typeof(DropboxPlugin));

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = Language.GetString("dropbox", LangKey.Configure),
				Tag = _host,
				Image = (Image)_resources.GetObject("Dropbox")
			};
			_itemPlugInConfig.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("dropbox", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			Log.Debug("Dropbox Plugin shutdown.");
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

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl) {
			uploadUrl = null;
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
			try {
				string dropboxUrl = null;
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("dropbox", LangKey.communication_wait), 
					delegate
					{
						string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
						dropboxUrl = DropboxUtils.UploadToDropbox(surfaceToUpload, outputSettings, filename);
					}
				);
				if (dropboxUrl == null) {
					return false;
				}
				uploadUrl = dropboxUrl;
				return true;
			} catch (Exception e) {
				Log.Error(e);
				MessageBox.Show(Language.GetString("dropbox", LangKey.upload_failure) + " " + e.Message);
				return false;
			}
		}
	}
}
