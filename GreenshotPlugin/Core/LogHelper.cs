/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Greenshot.IniFile;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using System;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of LogHelper.
	/// </summary>
	public class LogHelper {
		private const string LOG4NET_FILE = "log4net.xml";
		private static bool isLog4NetConfigured = false;
		private const string INIT_MESSAGE = "Greenshot initialization of log system failed";
		public static bool isInitialized {
			get {
				return isLog4NetConfigured;
			}
		}

		// Initialize Log4J
		public static string InitializeLog4NET() {
			// Setup log4j, currently the file is called log4net.xml
			string pafLog4NetFilename =  Path.Combine(Application.StartupPath, @"App\Greenshot\" + LOG4NET_FILE);
			string log4netFilename = Path.Combine(Application.StartupPath, LOG4NET_FILE);
			
			if (File.Exists(log4netFilename)) {
				try {
					XmlConfigurator.Configure(new FileInfo(log4netFilename)); 
					isLog4NetConfigured = true;
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, INIT_MESSAGE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			} else if (File.Exists(pafLog4NetFilename)) {
				try {
					XmlConfigurator.Configure(new FileInfo(pafLog4NetFilename)); 
					isLog4NetConfigured = true;
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, INIT_MESSAGE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}

			if (!isLog4NetConfigured) {
				try {
					Assembly assembly = typeof(LogHelper).Assembly;
					using (Stream stream = assembly.GetManifestResourceStream("GreenshotPlugin.log4net-embedded.xml")) {
						XmlConfigurator.Configure(stream);
						isLog4NetConfigured = true;
						IniConfig.ForceIniInStartupPath();
					}
				} catch (Exception ex){
					MessageBox.Show(ex.Message, INIT_MESSAGE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}

			if (isLog4NetConfigured) {
				// Get the logfile name
				try {
					if (((Hierarchy)LogManager.GetRepository()).Root.Appenders.Count > 0) {
						foreach (IAppender appender in ((Hierarchy)LogManager.GetRepository()).Root.Appenders) {
							if (appender is FileAppender) {
								return ((FileAppender)appender).File;
							}
						}
					}
				} catch {}
			}
			return null;
		}
	}

	/// <summary>
	/// A simple helper class to support the logging to the AppData location
	/// </summary>
	public class SpecialFolderPatternConverter : log4net.Util.PatternConverter {
		override protected void Convert(System.IO.TextWriter writer, object state) {
			Environment.SpecialFolder specialFolder = (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), base.Option, true);
			writer.Write(Environment.GetFolderPath(specialFolder));
		}
	}
}
