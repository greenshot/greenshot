using System;
using System.Drawing;
using System.IO;
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace ExternalCommand {
	public static class IconCache {
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IconCache));

		public static Image IconForCommand(string commandName) {
			Image icon = null;
			if (commandName != null) {
				if (config.commandlines.ContainsKey(commandName) && File.Exists(config.commandlines[commandName])) {
					try {
						icon = PluginUtils.GetCachedExeIcon(config.commandlines[commandName], 0);
					} catch (Exception ex) {
						LOG.Warn("Problem loading icon for " + config.commandlines[commandName], ex);
					}
				}
			}
			return icon;
		}
	}
}
