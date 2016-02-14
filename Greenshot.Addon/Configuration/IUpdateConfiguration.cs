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
using System.ComponentModel;
using Dapplo.Config.Ini;

namespace Greenshot.Addon.Configuration
{
	public enum BuildStates
	{
		ALPHA,
		BETA,
		RELEASE_CANDIDATE,
		RELEASE
	}

	/// <summary>
	/// Configuration used for the update check
	/// </summary>
	public interface IUpdateConfiguration
	{

		[Description("How many days between every update check? (0=no checks)"), DefaultValue(7)]
		int UpdateCheckInterval
		{
			get;
			set;
		}

		[Description("Last update check")]
		DateTimeOffset LastUpdateCheck
		{
			get;
			set;
		}

		[Description("What kind of build is this, injected to the DefaultValue during the build."), IniPropertyBehavior(Read = false, Write = false), DefaultValue("@build_type@")]
		BuildStates BuildState
		{
			get;
			set;
		}


		[Description("Also check for unstable version updates"), DefaultValue(false)]
		bool CheckForUnstable
		{
			get;
			set;
		}
	}
}
