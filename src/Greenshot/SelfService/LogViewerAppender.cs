/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Greenshot.Base.Core;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace Greenshot.SelfService
{
    /// <summary>
    /// Collects the log entries written while the log viewer is open, in the layout of the log file.
    /// Attached to the root logger only while the log viewer is open, so it costs nothing otherwise.
    /// </summary>
    internal sealed class LogViewerAppender : AppenderSkeleton
    {
        private const string DefaultPattern = "%date{ISO8601} [%thread] %-5level - [%logger] %m%n%exception";
        private readonly ConcurrentQueue<string> _entries = new ConcurrentQueue<string>();

        private Hierarchy _hierarchy;

        /// <summary>
        /// Creates the appender and attaches it to the root logger of the application's repository
        /// </summary>
        public static LogViewerAppender Attach()
        {
            return Attach(LogManager.GetRepository());
        }

        /// <summary>
        /// Creates the appender and attaches it to the root logger of the given repository
        /// </summary>
        public static LogViewerAppender Attach(ILoggerRepository repository)
        {
            var hierarchy = (Hierarchy) repository;
            var fileLayout = hierarchy.Root.Appenders.OfType<FileAppender>().FirstOrDefault()?.Layout;
            var appender = new LogViewerAppender
            {
                Layout = fileLayout ?? new EntryLayout(CreateDefaultLayout()),
                _hierarchy = hierarchy
            };
            hierarchy.Root.AddAppender(appender);
            return appender;
        }

        /// <summary>
        /// Detaches the appender from the root logger
        /// </summary>
        public void Detach()
        {
            _hierarchy.Root.RemoveAppender(this);
            Close();
        }

        /// <summary>
        /// The entries written since the last call, empty when there are none
        /// </summary>
        public string TakeEntries()
        {
            var text = new StringBuilder();
            while (_entries.TryDequeue(out var entry))
            {
                text.Append(entry);
            }
            return text.ToString();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            string entry = RenderLoggingEvent(loggingEvent);
            if (Layout.IgnoresException && loggingEvent.ExceptionObject != null)
            {
                entry += loggingEvent.GetExceptionString() + System.Environment.NewLine;
            }
            _entries.Enqueue(entry);
        }

        protected override bool RequiresLayout => true;

        private static ILayout CreateDefaultLayout()
        {
            var layout = new PatternLayout(DefaultPattern);
            layout.ActivateOptions();
            return layout;
        }
    }
}
