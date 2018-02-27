/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#region Usings
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Dapplo.Log;
using Greenshot.Gfx;
using GreenshotImgurPlugin.Forms;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
#endregion

namespace GreenshotImgurPlugin {
    /// <summary>
    /// This is the ImgurPlugin code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public class ImgurPlugin : IGreenshotPlugin {
	    private static readonly LogSource Log = new LogSource();
		private readonly ImgurConfiguration _imgurConfiguration;
		private readonly IGreenshotHost _greenshotHost;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _historyMenuItem;
		private ToolStripMenuItem _itemPlugInConfig;

        [ImportingConstructor]
        public ImgurPlugin(IGreenshotHost greenshotGreenshotHost, ImgurConfiguration imgurConfiguration)
        {
            _greenshotHost = greenshotGreenshotHost;
            _imgurConfiguration = imgurConfiguration;
        }

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
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize() {
			_resources = new ComponentResourceManager(typeof(ImgurPlugin));

			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur")
			{
				Image = (Image) _resources.GetObject("Imgur")
			};

			_historyMenuItem = new ToolStripMenuItem(Language.GetString("imgur", LangKey.history))
			{
				Tag = _greenshotHost
			};
			_historyMenuItem.Click += delegate {
				ImgurHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure))
			{
				Tag = _greenshotHost
			};
			itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

			PluginUtils.AddToContextMenu(_greenshotHost, itemPlugInRoot);
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
				_greenshotHost.GreenshotForm.BeginInvoke((MethodInvoker)delegate {
					if (_imgurConfiguration.ImgurUploadHistory != null && _imgurConfiguration.ImgurUploadHistory.Count > 0) {
						_historyMenuItem.Enabled = true;
					} else {
						_historyMenuItem.Enabled = false;
					}
				});
			} catch (Exception ex) {
				Log.Error().WriteLine(ex, "Error loading history");
			}
		}

		public virtual void Shutdown() {
			Log.Debug().WriteLine("Imgur Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
		}

		/// <summary>
		/// Upload the capture to imgur
		/// </summary>
		/// <param name="captureDetails">ICaptureDetails</param>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="uploadUrl">out string for the url</param>
		/// <returns>true if the upload succeeded</returns>
		public bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_imgurConfiguration.UploadFormat, _imgurConfiguration.UploadJpegQuality, _imgurConfiguration.UploadReduceColors);
			try {
				string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_imgurConfiguration.FilenamePattern, _imgurConfiguration.UploadFormat, captureDetails));
				ImgurInfo imgurInfo = null;

				// Run upload in the background
				new PleaseWaitForm().ShowAndWait("Imgur plug-in", Language.GetString("imgur", LangKey.communication_wait),
					delegate
					{
						imgurInfo = ImgurUtils.UploadToImgur(surfaceToUpload, outputSettings, captureDetails.Title, filename);
						if (imgurInfo != null && _imgurConfiguration.AnonymousAccess) {
							Log.Info().WriteLine("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
							_imgurConfiguration.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
							_imgurConfiguration.RuntimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
							UpdateHistoryMenuItem();
						}
					}
				);

				if (imgurInfo != null) {
					// TODO: Optimize a second call for export
					using (var tmpImage = surfaceToUpload.GetBitmapForExport())
					{
						imgurInfo.Image = tmpImage.CreateThumbnail(90, 90);
					}

					if (_imgurConfiguration.UsePageLink)
					{
						uploadUrl = imgurInfo.Page;
					}
					else
					{
						uploadUrl = imgurInfo.Original;
					}
					if (!string.IsNullOrEmpty(uploadUrl) && _imgurConfiguration.CopyLinkToClipboard)
					{
						try
						{
							ClipboardHelper.SetClipboardData(uploadUrl);

						}
						catch (Exception ex)
						{
							Log.Error().WriteLine(ex, "Can't write to clipboard: ");
							uploadUrl = null;
						}
					}
					return true;
				}
			} catch (Exception e) {
				Log.Error().WriteLine(e, "Error uploading.");
				MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
			}
			uploadUrl = null;
			return false;
		}
	}
}
