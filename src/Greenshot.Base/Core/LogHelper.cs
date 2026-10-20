/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
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

        /// <summary>
        /// The level of the root logger. A change applies until Greenshot restarts, log4net.xml stays as it is.
        /// </summary>
        public static Level RootLevel
        {
            get => ((Hierarchy) LogManager.GetRepository()).Root.Level;
            set
            {
                var hierarchy = (Hierarchy) LogManager.GetRepository();
                hierarchy.Root.Level = value;
                hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
            }
        }

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
                    using Stream stream = assembly.GetManifestResourceStream("Greenshot.Base.log4net-embedded.xml")
                                          ?? assembly.GetManifestResourceStream("GreenshotPlugin.log4net-embedded.xml");
                    if (stream != null)
                    {
                        XmlConfigurator.Configure(stream);
                        _isLog4NetConfigured = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, InitMessage, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (_isLog4NetConfigured)
            {
                KeepEntriesApart();

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

        /// <summary>
        /// Every appender writes its entries with <see cref="EntryLayout"/>
        /// </summary>
        private static void KeepEntriesApart()
        {
            try
            {
                foreach (var appender in LogManager.GetRepository().GetAppenders().OfType<AppenderSkeleton>())
                {
                    if (appender.Layout != null && appender.Layout is not EntryLayout)
                    {
                        appender.Layout = new EntryLayout(appender.Layout);
                    }
                }
            }
            catch
            {
                // ignored, the log keeps its layout
            }
        }
    }

    /// <summary>
    /// Writes a log entry so that only its first line starts at the beginning of a line: every further line, of the message
    /// (e.g. a window title or a value from a link) or of the exception, is indented.
    /// So no text in an entry can look like the start of another entry, in the file or in the log viewer.
    /// Control characters and invisible formatting characters are escaped (<see cref="LogSanitizer.EscapeUnsafeCharacters"/>),
    /// and a very long entry is cut (<see cref="LogSanitizer.MaxEntryLength"/>).
    /// </summary>
    public sealed class EntryLayout : ILayout
    {
        /// <summary>
        /// Put in front of every line of an entry after its first one
        /// </summary>
        public const string Indent = "    ";

        private readonly ILayout _layout;

        public EntryLayout(ILayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        public string ContentType => _layout.ContentType;
        public string Header => _layout.Header;
        public string Footer => _layout.Footer;
        // The exception is always written here, so it is indented and escaped like the rest of the entry
        public bool IgnoresException => false;

        public void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            var entry = new StringWriter();
            _layout.Format(entry, loggingEvent);
            if (_layout.IgnoresException && loggingEvent.ExceptionObject != null)
            {
                // A layout without %exception: the appender would add the exception as it is
                entry.WriteLine(loggingEvent.GetExceptionString());
            }

            string text = LogSanitizer.EscapeUnsafeCharacters(entry.ToString());
            if (text.Length > LogSanitizer.MaxEntryLength)
            {
                // The cut entry still ends with a line break, the next entry starts on its own line
                text = LogSanitizer.Limit(text) + Environment.NewLine;
            }
            writer.Write(IndentFollowingLines(text));
        }

        /// <summary>
        /// Indents every line after the first one; a line break (\r\n, \n or \r) at the very end stays as it is
        /// </summary>
        public static string IndentFollowingLines(string entry)
        {
            if (string.IsNullOrEmpty(entry) || entry.IndexOfAny(new[] { '\r', '\n' }) < 0)
            {
                return entry;
            }

            var result = new System.Text.StringBuilder(entry.Length + 32);
            for (int i = 0; i < entry.Length; i++)
            {
                char c = entry[i];
                result.Append(c);
                bool lineBreak = c == '\n' || (c == '\r' && (i + 1 >= entry.Length || entry[i + 1] != '\n'));
                if (lineBreak && i + 1 < entry.Length)
                {
                    result.Append(Indent);
                }
            }
            return result.ToString();
        }
    }

    /// <summary>
    /// A simple helper class to support the logging to the AppData location
    /// </summary>
    public class SpecialFolderPatternConverter : PatternConverter
    {
        public override void Convert(TextWriter writer, object state)
        {
            Environment.SpecialFolder specialFolder = (Environment.SpecialFolder) Enum.Parse(typeof(Environment.SpecialFolder), Option, true);
            writer.Write(Environment.GetFolderPath(specialFolder));
        }
    }
}