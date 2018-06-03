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

namespace GreenshotImgurPlugin {
	/// <summary>
	/// This is the ImgurPlugin code
	/// </summary>
	public class ImgurPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
		private static ImgurConfiguration _config;
		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _historyMenuItem;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_historyMenuItem != null) {
					_historyMenuItem.Dispose();
					_historyMenuItem = null;
				}
				if (_itemPlugInConfig != null) {
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new ImgurDestination(this);
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
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			_host = pluginHost;
			Attributes = myAttributes;

			// Get configuration
			_config = IniConfig.GetIniSection<ImgurConfiguration>();
			_resources = new ComponentResourceManager(typeof(ImgurPlugin));

			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur")
			{
				Image = (Image) _resources.GetObject("Imgur")
			};

			_historyMenuItem = new ToolStripMenuItem(Language.GetString("imgur", LangKey.history))
			{
				Tag = _host
			};
			_historyMenuItem.Click += delegate {
				ImgurHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure))
			{
				Tag = _host
			};
			_itemPlugInConfig.Click += delegate {
				_config.ShowConfigDialog();
			};
			itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

			PluginUtils.AddToContextMenu(_host, itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;

			// Enable history if there are items available
			UpdateHistoryMenuItem();
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("imgur", LangKey.configure);
			}
			if (_historyMenuItem != null) {
				_historyMenuItem.Text = Language.GetString("imgur", LangKey.history);
			}
		}

		private void UpdateHistoryMenuItem() {
		    if (_historyMenuItem == null)
		    {
		        return;
		    }
			try
            {
				_host.GreenshotForm.BeginInvoke((MethodInvoker)delegate {
					if (_config.ImgurUploadHistory != null && _config.ImgurUploadHistory.Count > 0) {
						_historyMenuItem.Enabled = true;
					} else {
						_historyMenuItem.Enabled = false;
					}
				});
			} catch (Exception ex) {
				Log.Error("Error loading history", ex);
			}
		}

		public virtual void Shutdown() {
			Log.Debug("Imgur Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			_config.ShowConfigDialog();
		}

		/// <summary>
		/// Upload the capture to imgur
		/// </summary>
		/// <param name="captureDetails">ICaptureDetails</param>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="uploadUrl">out string for the url</param>
		/// <returns>true if the upload succeeded</returns>
		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, _config.UploadReduceColors);
			try {
				string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_config.FilenamePattern, _config.UploadFormat, captureDetails));
				ImgurInfo imgurInfo = null;

				// Run upload in the background
				new PleaseWaitForm().ShowAndWait("Imgur plug-in", Language.GetString("imgur", LangKey.communication_wait),
					delegate
					{
						imgurInfo = ImgurUtils.UploadToImgur(surfaceToUpload, outputSettings, captureDetails.Title, filename);
						if (imgurInfo != null && _config.AnonymousAccess) {
							Log.InfoFormat("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
							_config.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
							_config.runtimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
							UpdateHistoryMenuItem();
						}
					}
				);

				if (imgurInfo != null) {
					// TODO: Optimize a second call for export
					using (Image tmpImage = surfaceToUpload.GetImageForExport()) {
						imgurInfo.Image = ImageHelper.CreateThumbnail(tmpImage, 90, 90);
					}
					IniConfig.Save();

					if (_config.UsePageLink)
					{
						uploadUrl = imgurInfo.Page;
					}
					else
					{
						uploadUrl = imgurInfo.Original;
					}
					if (!string.IsNullOrEmpty(uploadUrl) && _config.CopyLinkToClipboard)
					{
						try
						{
							ClipboardHelper.SetClipboardData(uploadUrl);

						}
						catch (Exception ex)
						{
							Log.Error("Can't write to clipboard: ", ex);
							uploadUrl = null;
						}
					}
					return true;
				}
			} catch (Exception e) {
				Log.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}
