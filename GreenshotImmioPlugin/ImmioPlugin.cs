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

namespace GreenshotImmioPlugin {
	/// <summary>
	/// This is the ImmioPlugin code
	/// </summary>
	public class ImmioPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImmioPlugin));
		private static ImmioConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost host;
		private ComponentResourceManager resources;

		public ImmioPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new ImmioDestination(this);
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
			config = IniConfig.GetIniSection<ImmioConfiguration>();
			resources = new ComponentResourceManager(typeof(ImmioPlugin));
			
			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem(Language.GetString("immio", LangKey.configure));
			itemPlugInRoot.Image = (Image)resources.GetObject("Immio");
			itemPlugInRoot.Click += delegate {
				config.ShowConfigDialog();
			};

			PluginUtils.AddToContextMenu(host, itemPlugInRoot);
			return true;
		}
		
		public virtual void Shutdown() {
			LOG.Debug("Immio Plugin shutdown.");
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
			LOG.Debug("Application closing, de-registering Immio Plugin!");
			Shutdown();
		}
		
		/// <summary>
		/// Upload the capture to Immio
		/// </summary>
		/// <param name="captureDetails"></param>
		/// <param name="image"></param>
		/// <param name="uploadURL">out string for the url</param>
		/// <returns>true if the upload succeeded</returns>
		public bool Upload(ICaptureDetails captureDetails, Image image, out string uploadURL) {
			OutputSettings outputSettings = new OutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			uploadURL = null;
			try {
				string filename = Path.GetFileName(FilenameHelper.GetFilename(config.UploadFormat, captureDetails));
				string url = null;
			
				// Run upload in the background
				new PleaseWaitForm().ShowAndWait(Attributes.Name, Language.GetString("immio", LangKey.communication_wait), 
					delegate() {
						url = ImmioUtils.UploadToImmio(image, outputSettings, captureDetails.Title, filename);
					}
				);
				uploadURL = url;

				IniConfig.Save();
				try {
					ClipboardHelper.SetClipboardData(url);
				} catch (Exception ex) {
					LOG.Error("Can't write to clipboard: ", ex);
				}
				return true;
			} catch (Exception e) {
				LOG.Error(e);
				MessageBox.Show(Language.GetString("immio", LangKey.upload_failure) + " " + e.Message);
			}
			return false;
		}
	}
}
