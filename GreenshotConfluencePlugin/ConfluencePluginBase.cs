/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Confluence;
using Greenshot.Capturing;
using Greenshot.Core;
using Greenshot.Forms;
using Greenshot.Plugin;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// This is the ConfluencePlugin base code
	/// </summary>
	public class ConfluencePlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
		private ILanguage lang = Language.GetInstance();
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private ConfluenceConnector confluenceConnector = null;
		private PluginAttribute myAttributes;

		public ConfluencePlugin() {
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
			this.myAttributes = myAttributes;
			host.OnImageEditorOpen += new OnImageEditorOpenHandler(ImageEditorOpened);
			
			// Register configuration (don't need the configuration itself)
			IniConfig.GetIniSection<ConfluenceConfiguration>();
		}

		public virtual void Shutdown() {
			LOG.Debug("Plugin shutdown.");
			host.OnImageEditorOpen -= new OnImageEditorOpenHandler(ImageEditorOpened);
			if (confluenceConnector != null) {
				confluenceConnector.logout();
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			IniConfig.GetIniSection<ConfluenceConfiguration>().ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, calling logout of jira!");
			Shutdown();
		}

		/// <summary>
		/// Implementation of the OnImageEditorOpen event
		/// Using the ImageEditor interface to register in the plugin menu
		/// </summary>
		private void ImageEditorOpened(object sender, ImageEditorOpenEventArgs eventArgs) {
			ToolStripMenuItem toolStripMenuItem = eventArgs.ImageEditor.GetPluginMenuItem();
			ToolStripMenuItem item = new ToolStripMenuItem();
			item.Text = "Upload to Confluence";
			item.Tag = eventArgs.ImageEditor;
			item.Click += new System.EventHandler(EditMenuClick);
			toolStripMenuItem.DropDownItems.Add(item);
		}
		

		/// <summary>
		/// This will be called when the menu item in the Editor is clicked
		/// </summary>
		public void EditMenuClick(object sender, EventArgs eventArgs) {
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			IImageEditor imageEditor = (IImageEditor)item.Tag;

			if (confluenceConnector == null) {
				this.confluenceConnector = new ConfluenceConnector();
			}
			ConfluenceForm confluenceForm = new ConfluenceForm(confluenceConnector);
			confluenceForm.setFilename(host.GetFilename(OutputFormat.Png, imageEditor.CaptureDetails));
			DialogResult result = confluenceForm.ShowDialog();
			if (result == DialogResult.OK) {
				using (MemoryStream stream = new MemoryStream()) {
					imageEditor.SaveToStream(stream, OutputFormat.Png, 100);
					byte [] buffer = stream.GetBuffer();
					try {
						confluenceForm.upload(buffer);
						LOG.Debug("Uploaded to Confluence.");
						MessageBox.Show(lang.GetString(LangKey.upload_success));
					} catch(Exception e) {
						MessageBox.Show(lang.GetString(LangKey.upload_failure) + " " + e.Message);
					}
				}
			}
		}
	}
}
