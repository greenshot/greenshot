/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;

using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
	public class ImgurPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
		private static ImgurConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost host;
		private ComponentResourceManager resources;
		private ToolStripMenuItem historyMenuItem = null;

		public ImgurPlugin() {
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
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			this.host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;

			// Get configuration
			config = IniConfig.GetIniSection<ImgurConfiguration>();
			resources = new ComponentResourceManager(typeof(ImgurPlugin));
			
			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur");
			itemPlugInRoot.Image = (Image)resources.GetObject("Imgur");

			historyMenuItem = new ToolStripMenuItem(Language.GetString("imgur", LangKey.imgur_history));
			historyMenuItem.Tag = host;
			historyMenuItem.Click += delegate {
				ImgurHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(historyMenuItem);

			ToolStripMenuItem itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.imgur_configure));
			itemPlugInConfig.Tag = host;
			itemPlugInConfig.Click += delegate {
				config.ShowConfigDialog();
			};
			itemPlugInRoot.DropDownItems.Add(itemPlugInConfig);

			PluginUtils.AddToContextMenu(host, itemPlugInRoot);

			// retrieve history in the background
			Thread backgroundTask = new Thread (new ThreadStart(CheckHistory));
			backgroundTask.Name = "Imgur History";
			backgroundTask.IsBackground = true;
			backgroundTask.SetApartmentState(ApartmentState.STA);
			backgroundTask.Start();
			return true;
		}
		
		private void CheckHistory() {
			try {
				ImgurUtils.LoadHistory();
				if (config.ImgurUploadHistory.Count > 0) {
					historyMenuItem.Enabled = true;
				} else {
					historyMenuItem.Enabled = false;
				}
			} catch {};
		}

		public virtual void Shutdown() {
			LOG.Debug("Imgur Plugin shutdown.");
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
		
		public bool Upload(ICaptureDetails captureDetails, Image image) {
			using (MemoryStream stream = new MemoryStream()) {
				BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(Attributes.Name, Language.GetString("imgur", LangKey.communication_wait));

				host.SaveToStream(image, stream, config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
				try {
					string filename = Path.GetFileName(host.GetFilename(config.UploadFormat, captureDetails));
					ImgurInfo imgurInfo = ImgurUtils.UploadToImgur(stream.GetBuffer(), (int)stream.Length, captureDetails.DateTime.ToString(), filename);
					LOG.InfoFormat("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
					config.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
					config.runtimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
					CheckHistory();
					imgurInfo.Image = ImageHelper.CreateThumbnail(image, 90, 90);
					IniConfig.Save();
					try {
						if (config.UsePageLink) {
							Clipboard.SetText(imgurInfo.Page);
						} else {
							Clipboard.SetText(imgurInfo.Original);
						}
					} catch (Exception ex) {
						LOG.Error("Can't write to clipboard: ", ex);
					}
					return true;
				} catch (Exception e) {
					MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
				} finally {
					backgroundForm.CloseDialog();
				}
			}
			return false;
		}

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public void EditMenuClick(object sender, EventArgs eventArgs) {
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			IImageEditor imageEditor = (IImageEditor)item.Tag;
			using (MemoryStream stream = new MemoryStream()) {
				BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(Attributes.Name, Language.GetString("imgur", LangKey.communication_wait));

				imageEditor.SaveToStream(stream, config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
				try {
					string filename = Path.GetFileName(host.GetFilename(config.UploadFormat, imageEditor.CaptureDetails));
					ImgurInfo imgurInfo = ImgurUtils.UploadToImgur(stream.GetBuffer(), (int)stream.Length, imageEditor.CaptureDetails.Title, filename);
					imageEditor.Surface.Modified = false;
					LOG.InfoFormat("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
					config.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
					config.runtimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
					CheckHistory();
					using (Image exportedImage = imageEditor.GetImageForExport()) {
						imgurInfo.Image = ImageHelper.CreateThumbnail(exportedImage, 90, 90);
					}
					// Make sure the configuration is save, so we don't lose the deleteHash
					IniConfig.Save();
					
					if (config.UsePageLink) {
						Clipboard.SetText(imgurInfo.Page);
					} else {
						Clipboard.SetText(imgurInfo.Original);
					}
				} catch(Exception e) {
					MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
				} finally {
					backgroundForm.CloseDialog();
				}
			}
		}
	}
}
