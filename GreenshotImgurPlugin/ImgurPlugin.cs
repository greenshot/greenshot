/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;
using System.Xml;

using Greenshot.Plugin;
using GreenshotImgurPlugin.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
	public class ImgurPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
		private static ImgurConfiguration config;
		public static PluginAttribute Attributes;
		private ILanguage lang = Language.GetInstance();
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private ComponentResourceManager resources;

		public ImgurPlugin() {
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public virtual void Initialize(IGreenshotPluginHost pluginHost, ICaptureHost captureHost, PluginAttribute myAttributes) {
			this.host = (IGreenshotPluginHost)pluginHost;
			this.captureHost = captureHost;
			Attributes = myAttributes;
			host.OnImageEditorOpen += new OnImageEditorOpenHandler(ImageEditorOpened);

			// Register configuration (don't need the configuration itself)
			config = IniConfig.GetIniSection<ImgurConfiguration>();
			resources = new ComponentResourceManager(typeof(ImgurPlugin));
		}

		public virtual void Shutdown() {
			LOG.Debug("Imgur Plugin shutdown.");
			host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
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
		/// Implementation of the OnImageEditorOpen event
		/// Using the ImageEditor interface to register in the plugin menu
		/// </summary>
		private void ImageEditorOpened(object sender, ImageEditorOpenEventArgs eventArgs) {
			ToolStripMenuItem item = new ToolStripMenuItem();
			item.Text = lang.GetString(LangKey.upload_menu_item); //"Upload to Imgur";
			item.Tag = eventArgs.ImageEditor;
			item.Click += new System.EventHandler(EditMenuClick);
			item.ShortcutKeys = ((Keys)((Keys.Control | Keys.I)));
			item.Image = (Image)resources.GetObject("Imgur");
			PluginUtils.AddToFileMenu(eventArgs.ImageEditor, item);
		}
		

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public void EditMenuClick(object sender, EventArgs eventArgs) {
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			IImageEditor imageEditor = (IImageEditor)item.Tag;
			using (MemoryStream stream = new MemoryStream()) {
				BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(Attributes.Name, lang.GetString(LangKey.communication_wait));

				imageEditor.SaveToStream(stream, config.UploadFormat, config.UploadJpegQuality);
				byte [] buffer = stream.GetBuffer();
				try {
					string filename = Path.GetFileName(host.GetFilename(config.UploadFormat, imageEditor.CaptureDetails));
					ImgurInfo imgurInfo = ImgurUtils.UploadToImgur(buffer, imageEditor.CaptureDetails.Title, filename);
					LOG.InfoFormat("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
					if (config.ImgurUploadHistory == null) {
						config.ImgurUploadHistory = new Dictionary<string, string>();
					}
					config.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
					config.runtimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
					imgurInfo.Image = ImgurUtils.CreateThumbnail(imageEditor.GetImageForExport(), 90, 90);
					// Make sure the configuration is save, so we don't lose the deleteHash
					IniConfig.Save();
					// Make sure the history is loaded, will be done only once
					ImgurUtils.LoadHistory();
					// Show
					ImgurHistory.ShowHistory();
				} catch(Exception e) {
					MessageBox.Show(lang.GetString(LangKey.upload_failure) + " " + e.Message);
				} finally {
					backgroundForm.CloseDialog();
				}
			}
		}
	}
}
