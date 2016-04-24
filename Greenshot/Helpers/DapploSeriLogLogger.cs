/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using Dapplo.LogFacade;
using Dapplo.LogFacade.Loggers;

namespace Greenshot.Helpers
{
	/// <summary>
	/// Wrapper for Dapplo.HttpExtensions.ILogger -> SeriLog
	/// </summary>
	public class DapploSeriLogLogger : AbstractLogger
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext<DapploSeriLogLogger>();

		private Serilog.Events.LogEventLevel MapLevel(LogLevel level)
		{
			Serilog.Events.LogEventLevel seriLogLevel = Serilog.Events.LogEventLevel.Debug;
			switch (level)
			{
				case LogLevel.Verbose:
					seriLogLevel = Serilog.Events.LogEventLevel.Verbose;
					break;
				case LogLevel.Debug:
					seriLogLevel = Serilog.Events.LogEventLevel.Debug;
					break;
				case LogLevel.Info:
					seriLogLevel = Serilog.Events.LogEventLevel.Information;
					break;
				case LogLevel.Warn:
					seriLogLevel = Serilog.Events.LogEventLevel.Warning;
					break;
				case LogLevel.Error:
					seriLogLevel = Serilog.Events.LogEventLevel.Error;
					break;
				case LogLevel.Fatal:
					seriLogLevel = Serilog.Events.LogEventLevel.Fatal;
					break;
			}
			return seriLogLevel;
		}

		public override void Write(LogInfo logInfo, string messageTemplate, params object[] propertyValues)
		{
			Log.ForContext(logInfo.Source.SourceType).Write(MapLevel(logInfo.Level), messageTemplate, propertyValues);
		}

		public override void Write(LogInfo logInfo, Exception exception, string messageTemplate = null, params object[] propertyValues)
		{
			Log.ForContext(logInfo.Source.SourceType).Write(MapLevel(logInfo.Level), exception, messageTemplate, propertyValues);
		}

		public override bool IsLogLevelEnabled(LogLevel level)
		{
			return Log.IsEnabled(MapLevel(level));
		}
	}
}
