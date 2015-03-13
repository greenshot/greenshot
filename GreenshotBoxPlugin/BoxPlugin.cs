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

namespace GreenshotBoxPlugin {
	/// <summary>
	/// This is the Box base code
	/// </summary>
	public class BoxPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxPlugin));
		private static BoxConfiguration _config;
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
			yield return new BoxDestination(this);
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
			_config = IniConfig.GetIniSection<BoxConfiguration>();
			_resources = new ComponentResourceManager(typeof(BoxPlugin));

			_itemPlugInConfig = new ToolStripMenuItem {
				Image = (Image) _resources.GetObject("Box"),
				Text = Language.GetString("box", LangKey.Configure)
			};
			_itemPlugInConfig.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("box", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			LOG.Debug("Box Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			_config.ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, de-registering Box Plugin!");
			Shutdown();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs) {
			_config.ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public string Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
			try {
				string url = null;
				string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
				SurfaceContainer imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
				
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("box", LangKey.communication_wait), 
					delegate {
						url = BoxUtils.UploadToBox(imageToUpload, captureDetails.Title, filename);
					}
				);

				if (url != null && _config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(url);
				}

				return url;
			} catch (Exception ex) {
				LOG.Error("Error uploading.", ex);
				MessageBox.Show(Language.GetString("box", LangKey.upload_failure) + " " + ex.Message);
				return null;
			}
		}
	}
}
