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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

using Greenshot.Capturing;
using Greenshot.Core;
using Greenshot.Plugin;

namespace RunAtOutput {
	/// <summary>
	/// An Plugin to run commands after an image was written
	/// </summary>
	public class RunAtOutputPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(RunAtOutputPlugin));
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private RunAtOutputConfiguration config;

		public RunAtOutputPlugin() {
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public virtual void Initialize(IGreenshotPluginHost pluginHost, ICaptureHost captureHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			
			this.host = (IGreenshotPluginHost)pluginHost;
			this.captureHost = captureHost;
			this.myAttributes = myAttributes;
			this.host.OnImageOutput += new OnImageOutputHandler(ImageOutput);
			
			this.config = IniConfig.GetIniSection<RunAtOutputConfiguration>();
		}

		public virtual void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
			this.host.OnImageOutput -= new OnImageOutputHandler(ImageOutput);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			LOG.Debug("Configure called");
			new SettingsForm().ShowDialog();
		}

		/// <summary>
		/// Handling of the OnImageOutputHandler event from the IGreenshotPlugin
		/// </summary>
		/// <param name="ImageOutputEventArgs">Has the FullPath to the image</param>
		private void ImageOutput(object sender, ImageOutputEventArgs eventArgs) {
			LOG.Debug("ImageOutput called with full path: " + eventArgs.FullPath);
			foreach(string commando in config.active) {
				string commandline = config.commandlines[commando];
				string arguments = config.arguments[commando];
				if (commandline != null && commandline.Length > 0) {
					Process p = new Process();
			        p.StartInfo.FileName = commandline;
			        p.StartInfo.Arguments = String.Format(arguments, eventArgs.FullPath);
			        p.StartInfo.UseShellExecute = false;
			        p.StartInfo.RedirectStandardOutput = true;
					LOG.Info("Starting : " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
			        p.Start();
			        string output = p.StandardOutput.ReadToEnd();
			        if (output != null && output.Trim().Length > 0) {
				        LOG.Info("Output:\n" + output);
			        }
					LOG.Info("Finished : " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
				}					
			}
		}
	}
}