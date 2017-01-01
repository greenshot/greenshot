/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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

namespace GreenshotPicasaPlugin {
	/// <summary>
	/// This is the Picasa base code
	/// </summary>
	public class PicasaPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(PicasaPlugin));
		private static PicasaConfiguration _config;
		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInRoot;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_itemPlugInRoot != null) {
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new PicasaDestination(this);
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

			// Get configuration
			_config = IniConfig.GetIniSection<PicasaConfiguration>();
			_resources = new ComponentResourceManager(typeof(PicasaPlugin));

			_itemPlugInRoot = new ToolStripMenuItem
			{
				Text = Language.GetString("picasa", LangKey.Configure),
				Tag = _host,
				Image = (Image) _resources.GetObject("Picasa")
			};
			_itemPlugInRoot.Click += ConfigMenuClick;
			PluginUtils.AddToContextMenu(_host, _itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInRoot != null) {
				_itemPlugInRoot.Text = Language.GetString("picasa", LangKey.Configure);
			}
		}

		public virtual void Shutdown() {
			Log.Debug("Picasa Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
			//host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			_config.ShowConfigDialog();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs) {
			Configure();
		}

		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality);
			try {
				string url = null;
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("picasa", LangKey.communication_wait), 
					delegate
					{
						string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
						url = PicasaUtils.UploadToPicasa(surfaceToUpload, outputSettings, captureDetails.Title, filename);
					}
				);
				uploadUrl = url;

				if (uploadUrl != null && _config.AfterUploadLinkToClipBoard) {
					ClipboardHelper.SetClipboardData(uploadUrl);
				}
				return true;
			} catch (Exception e) {
				Log.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("picasa", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}
