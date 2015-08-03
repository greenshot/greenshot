/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.IO;
using GreenshotPlugin.Core;
using Dapplo.Config.Ini;
using System.ComponentModel;

namespace ExternalCommand
{
	/// <summary>
	/// Description of FlickrConfiguration.
	/// </summary>
	[IniSection("ExternalCommand"), Description("Greenshot ExternalCommand Plugin configuration")]
	public interface ExternalCommandConfiguration : IIniSection<ExternalCommandConfiguration> {
		[Description("The commands that are available.")]
		IList<string> Commands
		{
			get;
			set;
		}

		[Description("Redirect the standard error of all external commands, used to output as warning to the greenshot.log."), DefaultValue(true)]
		bool RedirectStandardError
		{
			get;
			set;
		}

		[Description("Redirect the standard output of all external commands, used for different other functions (more below)."), DefaultValue(true)]
		bool RedirectStandardOutput
		{
			get;
			set;
		}

		[Description("Depends on 'RedirectStandardOutput': Show standard output of all external commands to the Greenshot log, this can be usefull for debugging."), DefaultValue(false)]
		bool ShowStandardOutputInLog
		{
			get;
			set;
		}

		[Description("Depends on 'RedirectStandardOutput': Parse the output and take the first found URI, if a URI is found than clicking on the notify bubble goes there."), DefaultValue(true)]
		bool ParseOutputForUri
		{
			get;
			set;
		}

		[Description("Depends on 'RedirectStandardOutput': Place the standard output on the clipboard."), DefaultValue(false)]
		bool OutputToClipboard
		{
			get;
			set;
		}

		[Description("Depends on 'RedirectStandardOutput' & 'ParseForUri': If an URI is found in the standard input, place it on the clipboard. (This overwrites the output from OutputToClipboard setting.)"), DefaultValue(true)]
		bool UriToClipboard
		{
			get;
			set;
		}

		[Description("The commandline for the output command.")]
		IDictionary<string, string> Commandline
		{
			get;
			set;
		}

		[Description("The arguments for the output command.")]
		IDictionary<string, string> Argument
		{
			get;
			set;
		}

		[Description("Should the command be started in the background.")]
		IDictionary<string, bool> RunInbackground
		{
			get;
			set;
		}

		[Description("Are defaults added."), DefaultValue(false)]
		bool DefaultsAdded
		{
			get;
			set;
		}
	}
}
