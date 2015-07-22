/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// This is the ImgurPlugin code
	/// </summary>
	public class ImgurPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
		private static ImgurConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost host;
		private ComponentResourceManager resources;
		private ToolStripMenuItem historyMenuItem = null;
		private ToolStripMenuItem itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (historyMenuItem != null) {
					historyMenuItem.Dispose();
					historyMenuItem = null;
				}
				if (itemPlugInConfig != null) {
					itemPlugInConfig.Dispose();
					itemPlugInConfig = null;
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
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			this.host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;

			// Get configuration
			config = IniConfig.GetIniSection<ImgurConfiguration>();
			resources = new ComponentResourceManager(typeof(ImgurPlugin));
			
			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur");
			itemPlugInRoot.Image = (Image)resources.GetObject("Imgur");

			historyMenuItem = new ToolStripMenuItem(Language.GetString("imgur", LangKey.history));
			historyMenuItem.Tag = host;
			historyMenuItem.Click += async (sender , e) => {
				await ImgurHistory.ShowHistoryAsync().ConfigureAwait(false);
			};
			itemPlugInRoot.DropDownItems.Add(historyMenuItem);

			itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure));
			itemPlugInConfig.Tag = host;
			itemPlugInConfig.Click += delegate {
				config.ShowConfigDialog();
			};
			itemPlugInRoot.DropDownItems.Add(itemPlugInConfig);

			PluginUtils.AddToContextMenu(host, itemPlugInRoot);
			Language.LanguageChanged += new LanguageChangedHandler(OnLanguageChanged);

			// retrieve history in the background
			var historyTask = CheckHistory();
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (itemPlugInConfig != null) {
				itemPlugInConfig.Text = Language.GetString("imgur", LangKey.configure);
			}
			if (historyMenuItem != null) {
				historyMenuItem.Text = Language.GetString("imgur", LangKey.history);
			}
		}

		private async Task CheckHistory() {
			try {
				await ImgurUtils.LoadHistory().ConfigureAwait(false);
				host.GreenshotForm.BeginInvoke((MethodInvoker)delegate {
					if (config.ImgurUploadHistory.Count > 0) {
						historyMenuItem.Enabled = true;
					} else {
						historyMenuItem.Enabled = false;
					}
				});
			} catch (Exception ex) {
				LOG.Error("Error loading history", ex);
			};
		}

		public virtual void Shutdown() {
			LOG.Debug("Imgur Plugin shutdown.");
			Language.LanguageChanged -= new LanguageChangedHandler(OnLanguageChanged);
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
			LOG.Debug("Application closing, de-registering Imgur Plugin!");
			Shutdown();
		}
		
		/// <summary>
		/// Upload the capture to imgur
		/// </summary>
		/// <param name="captureDetails"></param>
		/// <param name="image"></param>
		/// <param name="uploadURL">out string for the url</param>
		/// <returns>URL if the upload succeeded</returns>
		public async Task<string> UploadAsync(ICaptureDetails captureDetails, ISurface surfaceToUpload, CancellationToken token = default(CancellationToken)) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			string uploadURL = null;
			try {
				string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(config.FilenamePattern, config.UploadFormat, captureDetails));
				var imgurInfo = await PleaseWaitWindow.CreateAndShowAsync("Imgur plug-in", Language.GetString("imgur", LangKey.communication_wait), (progress, pleaseWaitToken) => {
					return ImgurUtils.UploadToImgurAsync(surfaceToUpload, outputSettings, captureDetails.Title, filename, pleaseWaitToken);
				}).ConfigureAwait(false);

				if (imgurInfo != null) {
					await CheckHistory().ConfigureAwait(false);
					IniConfig.Save();
					try {
						if (config.UsePageLink) {
							if (imgurInfo.Page.AbsoluteUri != null) {
								uploadURL = imgurInfo.Page.AbsoluteUri;
								if (config.CopyUrlToClipboard) {
									ClipboardHelper.SetClipboardData(imgurInfo.Page.AbsoluteUri);
								}
							}
						} else {
							if (imgurInfo.Original.AbsoluteUri != null) {

								uploadURL = imgurInfo.Original.AbsoluteUri;
								if (config.CopyUrlToClipboard) {
									ClipboardHelper.SetClipboardData(imgurInfo.Original.AbsoluteUri);
								}
							}
						}
					} catch (Exception ex) {
						LOG.Error("Can't write to clipboard: ", ex);
						uploadURL = null;
					}
				}
			} catch (Exception e) {
				LOG.Error("Error uploading.", e);
				MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
				uploadURL = null;
			}
			return uploadURL;
		}
	}
}
