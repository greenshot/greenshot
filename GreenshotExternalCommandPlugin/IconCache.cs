using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace ExternalCommand {
	public static class IconCache {
		private static Dictionary<string, Image> iconCache = new Dictionary<string, Image>();
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IconCache));

		public static Image IconForCommand(string commandName) {
			if (commandName != null) {
				if (!iconCache.ContainsKey(commandName)) {
					Image icon = null;
					if (config.commandlines.ContainsKey(commandName) && File.Exists(config.commandlines[commandName])) {
						try {
							icon = PluginUtils.GetExeIcon(config.commandlines[commandName], 0);
						} catch (Exception ex) {
							LOG.Warn("Problem loading icon for " + config.commandlines[commandName], ex);
						}
					}
					// Also add null to the cache if nothing is found
					iconCache.Add(commandName, icon);
				}
				if (iconCache.ContainsKey(commandName)) {
					return iconCache[commandName];
				}
			}
			return null;
		}
	}
}
