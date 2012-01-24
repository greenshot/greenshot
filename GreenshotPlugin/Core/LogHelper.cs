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
using System.IO;
using System.Windows.Forms;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of LogHelper.
	/// </summary>
	public class LogHelper {
		private const string LOG4NET_FILE = "log4net.xml";
		// Initialize Log4J
		public static string InitializeLog4NET() {
			// Setup log4j, currently the file is called log4net.xml
			string pafLog4NetFilename =  Path.Combine(Application.StartupPath, @"App\Greenshot\" + LOG4NET_FILE);
			string log4netFilename = Path.Combine(Application.StartupPath, LOG4NET_FILE);
			if (File.Exists(log4netFilename)) {
				XmlConfigurator.Configure(new FileInfo(log4netFilename)); 
			} else if (File.Exists(pafLog4NetFilename)) {
				XmlConfigurator.Configure(new FileInfo(pafLog4NetFilename)); 
			} else {
				MessageBox.Show("Can't find file " + LOG4NET_FILE);
			}
			
			// Get the logfile
			try {
				IAppender appender = ((Hierarchy)LogManager.GetRepository()).Root.Appenders[0];
				if (appender is FileAppender) {
					return ((FileAppender)appender).File;
				}
			} catch (Exception) {
				// Ignore
			}
			return null;
		}
	}
}
