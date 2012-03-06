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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;

using Confluence;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using TranslationByMarkupExtension;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// This is the ConfluencePlugin base code
	/// </summary>
	public class ConfluencePlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
		private static ConfluenceConnector confluenceConnector = null;
		private static PluginAttribute ConfluencePluginAttributes;
		private static ConfluenceConfiguration config = null;
		private static ILanguage lang = Language.GetInstance();
		private static IGreenshotHost host;
		
		public static ConfluenceConnector ConfluenceConnectorNoLogin {
			get {
				if (confluenceConnector == null) {
					if (config.Url.Contains("soap-axis")) {
						confluenceConnector = new ConfluenceConnector(config.Url, config.Timeout);
					} else {
						confluenceConnector = new ConfluenceConnector(config.Url + ConfluenceConfiguration.DEFAULT_POSTFIX, config.Timeout);
					}
				}
				return confluenceConnector;
			}
		}

		public static ConfluenceConnector ConfluenceConnector {
			get {
				if (confluenceConnector == null) {
					confluenceConnector = ConfluenceConnectorNoLogin;
				}
				try {
					if (!confluenceConnector.isLoggedIn) {
						confluenceConnector.login();
					}
				} catch (Exception e) {
					MessageBox.Show(lang.GetFormattedString(LangKey.login_error, e.Message));
				}
				return confluenceConnector;
			}
		}
		
		public static IGreenshotHost Host {
			get {
				return host;
			}
		}

		public ConfluencePlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new ConfluenceDestination();
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
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			host = pluginHost;
			ConfluencePluginAttributes = myAttributes;

			// Register configuration (don't need the configuration itself)
			config = IniConfig.GetIniSection<ConfluenceConfiguration>();
			if(config.IsDirty) {
				IniConfig.Save();
			}
			TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
			//resources = new ComponentResourceManager(typeof(JiraPlugin));
			return true;
		}

		public virtual void Shutdown() {
			LOG.Debug("Confluence Plugin shutdown.");
			if (confluenceConnector != null) {
				confluenceConnector.logout();
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			ConfluenceConfiguration clonedConfig = config.Clone();
			ConfluenceConfigurationForm configForm = new ConfluenceConfigurationForm(clonedConfig);
			string url = config.Url;
			Nullable<bool> dialogResult = configForm.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value) {
				// copy the new object to the old...
				clonedConfig.CloneTo(config);
				IniConfig.Save();
				if (confluenceConnector != null) {
					if (!url.Equals(config.Url) && confluenceConnector.isLoggedIn) {
						confluenceConnector.logout();
					}
				}
			}
		}
	}
}
