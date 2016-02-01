/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.LogFacade;

namespace Greenshot.Helpers
{
	/// <summary>
	/// Wrapper for Dapplo.HttpExtensions.ILogger -> SeriLog
	/// </summary>
	public class DapploSeriLogLogger : ILogger
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext<DapploSeriLogLogger>();

		private Serilog.Events.LogEventLevel MapLevel(LogInfo logInfo)
		{
			Serilog.Events.LogEventLevel level = Serilog.Events.LogEventLevel.Debug;
			switch (logInfo.Level)
			{
				case LogLevel.Verbose:
					level = Serilog.Events.LogEventLevel.Verbose;
					break;
				case LogLevel.Debug:
					level = Serilog.Events.LogEventLevel.Debug;
					break;
				case LogLevel.Info:
					level = Serilog.Events.LogEventLevel.Information;
					break;
				case LogLevel.Warn:
					level = Serilog.Events.LogEventLevel.Warning;
					break;
				case LogLevel.Error:
					level = Serilog.Events.LogEventLevel.Error;
					break;
				case LogLevel.Fatal:
					level = Serilog.Events.LogEventLevel.Fatal;
					break;
			}
			return level;
		}

		public void Write(LogInfo logInfo, string messageTemplate, params object[] propertyValues)
		{
			Log.ForContext(logInfo.Caller).Write(MapLevel(logInfo), messageTemplate, propertyValues);
		}

		public void Write(LogInfo logInfo, Exception exception, string messageTemplate, params object[] propertyValues)
		{
			Log.ForContext(logInfo.Caller).Write(MapLevel(logInfo), exception, messageTemplate, propertyValues);
		}
	}
}
