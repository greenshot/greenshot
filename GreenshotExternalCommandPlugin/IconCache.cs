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

		public static Image IconForExe(string exepath) {
			if (exepath != null) {
				if (!iconCache.ContainsKey(exepath)) {
					Image icon = null;
					if (File.Exists(config.commandlines[exepath])) {
						try {
							icon = PluginUtils.GetExeIcon(config.commandlines[exepath], 0);
						} catch (Exception ex) {
							LOG.Warn("Problem loading icon for " + config.commandlines[exepath], ex);
						}
					}
					iconCache.Add(exepath, icon);
				}
				return iconCache[exepath];
			} else {
				return null;
			}
		}
	}
}
