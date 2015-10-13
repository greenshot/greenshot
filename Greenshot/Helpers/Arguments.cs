/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Windows.Native;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greenshot.Helpers
{
	public class Arguments
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (GreenshotServer));

		public IList<string> FilesToOpen
		{
			get;
			set;
		}

		public bool IsExit
		{
			get;
			set;
		}

		public bool IsHelp
		{
			get;
			set;
		}

		public bool IsReload
		{
			get;
			set;
		}

		public bool IsNoRun
		{
			get;
			set;
		}

		public string Language
		{
			get;
			set;
		}

		public string IniDirectory
		{
			get;
			set;
		}

		/// <summary>
		/// Show the help to the user
		/// </summary>
		private void ShowHelp()
		{
			// Try to attach to the console
			bool attachedToConsole = Kernel32.AttachConsole(Kernel32.ATTACHCONSOLE_ATTACHPARENTPROCESS);
			// If attach didn't work, open a console
			if (!attachedToConsole)
			{
				Kernel32.AllocConsole();
			}
			var helpOutput = new StringBuilder();
			helpOutput.AppendLine();
			helpOutput.AppendLine("Greenshot commandline options:");
			helpOutput.AppendLine();
			helpOutput.AppendLine();
			helpOutput.AppendLine("\t/help");
			helpOutput.AppendLine("\t\tThis help.");
			helpOutput.AppendLine();
			helpOutput.AppendLine();
			helpOutput.AppendLine("\t/exit");
			helpOutput.AppendLine("\t\tTries to close all running instances.");
			helpOutput.AppendLine();
			helpOutput.AppendLine();
			helpOutput.AppendLine("\t/reload");
			helpOutput.AppendLine("\t\tReload the configuration of Greenshot.");
			helpOutput.AppendLine();
			helpOutput.AppendLine();
			helpOutput.AppendLine("\t/language [language code]");
			helpOutput.AppendLine("\t\tSet the language of Greenshot, e.g. greenshot /language en-US.");
			helpOutput.AppendLine();
			helpOutput.AppendLine();
			helpOutput.AppendLine("\t/inidirectory [directory]");
			helpOutput.AppendLine("\t\tSet the directory where the greenshot.ini should be stored & read.");
			helpOutput.AppendLine();
			helpOutput.AppendLine();
			helpOutput.AppendLine("\t[filename]");
			helpOutput.AppendLine("\t\tOpen the bitmap files in the running Greenshot instance or start a new instance");
			Console.WriteLine(helpOutput.ToString());

			// If attach didn't work, wait for key otherwise the console will close to quickly
			if (!attachedToConsole)
			{
				Console.ReadKey();
			}
		}

		public Arguments(string[] args)
		{
			FilesToOpen = new List<string>();

			if (args.Length > 0 && LOG.IsDebugEnabled)
			{
				StringBuilder argumentString = new StringBuilder();
				for (int argumentNr = 0; argumentNr < args.Length; argumentNr++)
				{
					argumentString.Append("[").Append(args[argumentNr]).Append("] ");
				}
				LOG.Debug("Greenshot arguments: " + argumentString);
			}

			for (int argumentNr = 0; argumentNr < args.Length; argumentNr++)
			{
				string argument = args[argumentNr];
				// Help
				if (argument.ToLower().Equals("/help") || argument.ToLower().Equals("/h") || argument.ToLower().Equals("/?"))
				{
					ShowHelp();
					IsHelp = true;
					return;
				}

				if (argument.ToLower().Equals("/exit"))
				{
					IsExit = true;
					continue;
				}

				// Reload the configuration
				if (argument.ToLower().Equals("/reload"))
				{
					IsReload = true;
					continue;
				}

				// Stop running
				if (argument.ToLower().Equals("/norun"))
				{
					IsNoRun = true;
					continue;
				}

				// Language
				if (argument.ToLower().Equals("/language"))
				{
					Language = args[++argumentNr];
					continue;
				}

				// Setting the INI-directory
				if (argument.ToLower().Equals("/inidirectory"))
				{
					IniDirectory = args[++argumentNr];
					continue;
				}

				// Files to open
				FilesToOpen.Add(argument);
			}
		}
	}
}