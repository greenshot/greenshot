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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Core;
using Greenshot.Plugin;

namespace FixTitle {
	/// <summary>
	/// An example Plugin so developers can see how they can develop their own plugin
	/// </summary>
	public class FixTitle : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FixTitle));
		private const string CONFIG_FILE_NAME = "titlefix.properties";
		private const string ACTIVE_PROPERTY = "active";
		private const string REGEXP_PROPERTY = ".regexp";
		private const string REPLACE_PROPERTY = ".replace";

		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private Properties config = null;

		public FixTitle() {
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

			LoadConfig();
			
			if (config != null) {
				this.host.OnCaptureTaken += new OnCaptureTakenHandler(CaptureTaken);
			} else {
				LOG.Warn("Not registering FixTitle plugin due to missing configuration");
			}

		}

		public virtual void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
			this.host.OnCaptureTaken -= new OnCaptureTakenHandler(CaptureTaken);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			LOG.Debug("Configure called");
		}
		
		/// <summary>
		/// Implementation of the OnCaptureTaken event
		/// </summary>
		private void CaptureTaken(object sender, CaptureTakenEventArgs eventArgs) {
			string title = eventArgs.Capture.CaptureDetails.Title;
			LOG.Debug("Title before: " + title);
			if (title != null && title.Length > 0) {
				title = title.Trim();
				foreach(string titleIdentifier in config.GetPropertyAsArray(ACTIVE_PROPERTY)) {
					string regexpString = config.GetProperty(titleIdentifier + REGEXP_PROPERTY);
					string replaceString = config.GetProperty(titleIdentifier + REPLACE_PROPERTY);
					if (regexpString != null && regexpString.Length > 0) {
						Regex regex = new Regex(regexpString);
						title = regex.Replace(title, replaceString);
					}
				}
			}
			LOG.Debug("Title after: " + title);
			eventArgs.Capture.CaptureDetails.Title = title;
		}
		
		private void LoadConfig() {
			string configfilename = Path.Combine(host.ConfigurationPath, CONFIG_FILE_NAME);
			if (File.Exists(configfilename)) {
				config = Properties.read(configfilename);
			} else {
				config = new Properties();
			}
			// Check if we have a configuration
			if (!config.ContainsKey(ACTIVE_PROPERTY)) {
				// Create default with FireFox
				config.AddProperty("Firefox"+REGEXP_PROPERTY, " - Mozilla Firefox.*");
				config.AddProperty("Firefox"+REPLACE_PROPERTY, "");
				// and IE
				config.AddProperty("IE"+REGEXP_PROPERTY, " - Microsoft Internet Explorer.*");
				config.AddProperty("IE"+REPLACE_PROPERTY, "");
				// Activate both
				config.AddProperty(ACTIVE_PROPERTY, "IE,Firefox");
				config.write(configfilename);
			}
		}
	}
}