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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace PluginExample {
	/// <summary>
	/// An example Plugin so developers can see how they can develop their own plugin
	/// </summary>
	public class PluginExample : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PluginExample));
		private IGreenshotHost host;
		private PluginAttribute myAttributes;

		public PluginExample() {
		}
		
		public IEnumerable<IDestination> Destinations() {
			yield return new SimpleOutputDestination(host);
		}

		public IEnumerable<IProcessor> Processors() {
			yield return new AnnotateProcessor(host);
			yield return new GreyscaleProcessor(host);
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			
			this.host = pluginHost;
			this.myAttributes = myAttributes;
			return true;
		}

		public virtual void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			LOG.Debug("Configure called");
			SettingsForm settingsForm = new SettingsForm();
			settingsForm.ShowDialog();
		}
	}
}