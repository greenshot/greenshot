/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Greenshot.Base.IniFile;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Util;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Initialize the logger
    /// </summary>
    public class LogHelper
    {
        private static bool _isLog4NetConfigured;
        private const string InitMessage = "Greenshot initialization of log system failed";

        public static bool IsInitialized => _isLog4NetConfigured;

        // Initialize Log4J
        public static string InitializeLog4Net()
        {
            // Setup log4j, currently the file is called log4net.xml
            foreach (var logName in new[]
            {
                "log4net.xml", @"App\Greenshot\log4net-portable.xml"
            })
            {
                string log4NetFilename = Path.Combine(Application.StartupPath, logName);
                if (File.Exists(log4NetFilename))
                {
                    try
                    {
                        XmlConfigurator.Configure(new FileInfo(log4NetFilename));
                        _isLog4NetConfigured = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, InitMessage, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            // Fallback
            if (!_isLog4NetConfigured)
            {
                try
                {
                    Assembly assembly = typeof(LogHelper).Assembly;
                    using Stream stream = assembly.GetManifestResourceStream("GreenshotPlugin.log4net-embedded.xml");
                    XmlConfigurator.Configure(stream);
                    _isLog4NetConfigured = true;
                    IniConfig.ForceIniInStartupPath();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, InitMessage, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (_isLog4NetConfigured)
            {
                // Get the logfile name
                try
                {
                    if (((Hierarchy) LogManager.GetRepository()).Root.Appenders.Count > 0)
                    {
                        foreach (IAppender appender in ((Hierarchy) LogManager.GetRepository()).Root.Appenders)
                        {
                            var fileAppender = appender as FileAppender;
                            if (fileAppender != null)
                            {
                                return fileAppender.File;
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }
    }

    /// <summary>
    /// A simple helper class to support the logging to the AppData location
    /// </summary>
    public class SpecialFolderPatternConverter : PatternConverter
    {
        protected override void Convert(TextWriter writer, object state)
        {
            Environment.SpecialFolder specialFolder = (Environment.SpecialFolder) Enum.Parse(typeof(Environment.SpecialFolder), Option, true);
            writer.Write(Environment.GetFolderPath(specialFolder));
        }
    }
}