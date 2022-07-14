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

using Dapplo.Log;
using log4net;

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    /// Used to make Dapplo.Log, used in Dapplo.Jira, write to Log4net
    /// </summary>
    public class Log4NetLogger : AbstractLogger
    {
        private ILog GetLogger(LogSource logSource) => logSource.SourceType != null ? LogManager.GetLogger(logSource.SourceType) : LogManager.GetLogger(logSource.Source);

        /// <summary>
        ///     Write the supplied information to a log4net.ILog
        /// </summary>
        /// <param name="logInfo">LogInfo</param>
        /// <param name="messageTemplate">string</param>
        /// <param name="propertyValues">params object[]</param>
        public override void Write(LogInfo logInfo, string messageTemplate, params object[] propertyValues)
        {
            var log = GetLogger(logInfo.Source);

            switch (logInfo.LogLevel)
            {
                case LogLevels.Verbose:
                case LogLevels.Debug:
                    if (propertyValues != null)
                        log.DebugFormat(messageTemplate, propertyValues);
                    else
                        log.Debug(messageTemplate);
                    break;
                case LogLevels.Error:
                    if (propertyValues != null)
                        log.ErrorFormat(messageTemplate, propertyValues);
                    else
                        log.Error(messageTemplate);
                    break;
                case LogLevels.Fatal:
                    if (propertyValues != null)
                        log.FatalFormat(messageTemplate, propertyValues);
                    else
                        log.Fatal(messageTemplate);
                    break;
                case LogLevels.Info:
                    if (propertyValues != null)
                        log.InfoFormat(messageTemplate, propertyValues);
                    else
                        log.Info(messageTemplate);
                    break;
                case LogLevels.Warn:
                    if (propertyValues != null)
                        log.WarnFormat(messageTemplate, propertyValues);
                    else
                        log.Warn(messageTemplate);
                    break;
            }
        }

        /// <summary>
        /// Make sure there are no newlines passed
        /// </summary>
        /// <param name="logInfo"></param>
        /// <param name="messageTemplate"></param>
        /// <param name="logParameters"></param>
        public override void WriteLine(LogInfo logInfo, string messageTemplate, params object[] logParameters) => Write(logInfo, messageTemplate, logParameters);

        /// <summary>
        ///     Test if a certain LogLevels enum is enabled
        /// </summary>
        /// <param name="level">LogLevels value</param>
        /// <param name="logSource">LogSource to check for</param>
        /// <returns>bool true if the LogLevels enum is enabled</returns>
        public override bool IsLogLevelEnabled(LogLevels level, LogSource logSource = null)
        {
            var log = GetLogger(logSource);
            return level switch
            {
                LogLevels.Verbose or LogLevels.Debug => log.IsDebugEnabled,
                LogLevels.Error => log.IsErrorEnabled,
                LogLevels.Fatal => log.IsFatalEnabled,
                LogLevels.Info => log.IsInfoEnabled,
                LogLevels.Warn => log.IsWarnEnabled,
                _ => false,
            };
        }
    }
}