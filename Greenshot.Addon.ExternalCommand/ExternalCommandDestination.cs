#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using Dapplo.Ini;
using Dapplo.Log;
using GreenshotPlugin.Addons;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
	/// <summary>
	///     Description of OCRDestination.
	/// </summary>
	public class ExternalCommandDestination : AbstractDestination
	{
		private static readonly LogSource Log = new LogSource();

		private static readonly Regex UriRegexp = new Regex(
				@"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)", RegexOptions.Compiled);

		private static readonly IExternalCommandConfiguration Config = IniConfig.Current.Get<IExternalCommandConfiguration>();
		private readonly string _presetCommand;

		public ExternalCommandDestination(string commando)
		{
			_presetCommand = commando;
		}

		public string Designation => "External " + _presetCommand.Replace(',', '_');

		public override string Description => _presetCommand;

		public override Bitmap GetDisplayIcon(double dpi)
		{
			return IconCache.IconForCommand(_presetCommand, dpi > 100);
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			yield break;
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var outputSettings = new SurfaceOutputSettings();
			outputSettings.PreventGreenshotFormat();

			if (_presetCommand != null)
			{
				if (!Config.RunInbackground.ContainsKey(_presetCommand))
				{
					Config.RunInbackground.Add(_presetCommand, true);
				}
				var runInBackground = Config.RunInbackground[_presetCommand];
				var fullPath = captureDetails.Filename;
				if (fullPath == null)
				{
					fullPath = ImageOutput.SaveNamedTmpFile(surface, captureDetails, outputSettings);
				}

			    if (runInBackground)
				{
					var commandThread = new Thread(delegate()
					{
						CallExternalCommand(exportInformation, fullPath, out _, out _);
						ProcessExport(exportInformation, surface);
					})
					{
						Name = "Running " + _presetCommand,
						IsBackground = true
					};
					commandThread.SetApartmentState(ApartmentState.STA);
					commandThread.Start();
					exportInformation.ExportMade = true;
				}
				else
				{
					CallExternalCommand(exportInformation, fullPath, out _, out _);
					ProcessExport(exportInformation, surface);
				}
			}
			return exportInformation;
		}

		/// <summary>
		///     Wrapper method for the background and normal call, this does all the logic:
		///     Call the external command, parse for URI, place to clipboard and set the export information
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="fullPath"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		private void CallExternalCommand(ExportInformation exportInformation, string fullPath, out string output, out string error)
		{
			output = null;
			error = null;
			try
			{
				if (CallExternalCommand(_presetCommand, fullPath, out output, out error) == 0)
				{
					exportInformation.ExportMade = true;
					if (!string.IsNullOrEmpty(output))
					{
						var uriMatches = UriRegexp.Matches(output);
						// Place output on the clipboard before the URI, so if one is found this overwrites
						if (Config.OutputToClipboard)
						{
							ClipboardHelper.SetClipboardData(output);
						}
						if (uriMatches.Count > 0)
						{
							exportInformation.Uri = uriMatches[0].Groups[1].Value;
							Log.Info().WriteLine("Got URI : {0} ", exportInformation.Uri);
							if (Config.UriToClipboard)
							{
								ClipboardHelper.SetClipboardData(exportInformation.Uri);
							}
						}
					}
				}
				else
				{
					Log.Warn().WriteLine("Error calling external command: {0} ", output);
					exportInformation.ExportMade = false;
					exportInformation.ErrorMessage = error;
				}
			}
			catch (Exception ex)
			{
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
				Log.Warn().WriteLine("Error calling external command: {0} ", exportInformation.ErrorMessage);
			}
		}

		/// <summary>
		///     Wrapper to retry with a runas
		/// </summary>
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		private int CallExternalCommand(string commando, string fullPath, out string output, out string error)
		{
			try
			{
				return CallExternalCommand(commando, fullPath, null, out output, out error);
			}
			catch (Win32Exception w32Ex)
			{
				try
				{
					return CallExternalCommand(commando, fullPath, "runas", out output, out error);
				}
				catch
				{
					w32Ex.Data.Add("commandline", Config.Commandline[_presetCommand]);
					w32Ex.Data.Add("arguments", Config.Argument[_presetCommand]);
					throw;
				}
			}
			catch (Exception ex)
			{
				ex.Data.Add("commandline", Config.Commandline[_presetCommand]);
				ex.Data.Add("arguments", Config.Argument[_presetCommand]);
				throw;
			}
		}

		/// <summary>
		///     The actual executing code for the external command
		/// </summary>
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="verb"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		private static int CallExternalCommand(string commando, string fullPath, string verb, out string output, out string error)
		{
			var commandline = Config.Commandline[commando];
			var arguments = Config.Argument[commando];
			output = null;
			error = null;
			if (!string.IsNullOrEmpty(commandline))
			{
				using (var process = new Process())
				{
					// Fix variables
					commandline = FilenameHelper.FillVariables(commandline, true);
					commandline = FilenameHelper.FillCmdVariables(commandline);

					arguments = FilenameHelper.FillVariables(arguments, false);
					arguments = FilenameHelper.FillCmdVariables(arguments, false);

					process.StartInfo.FileName = FilenameHelper.FillCmdVariables(commandline);
					process.StartInfo.Arguments = FormatArguments(arguments, fullPath);
					process.StartInfo.UseShellExecute = false;
					if (Config.RedirectStandardOutput)
					{
						process.StartInfo.RedirectStandardOutput = true;
					}
					if (Config.RedirectStandardError)
					{
						process.StartInfo.RedirectStandardError = true;
					}
					if (verb != null)
					{
						process.StartInfo.Verb = verb;
					}
					Log.Info().WriteLine("Starting : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
					process.Start();
					process.WaitForExit();
					if (Config.RedirectStandardOutput)
					{
						output = process.StandardOutput.ReadToEnd();
						if (Config.ShowStandardOutputInLog && output.Trim().Length > 0)
						{
							Log.Info().WriteLine("Output:\n{0}", output);
						}
					}
					if (Config.RedirectStandardError)
					{
						error = process.StandardError.ReadToEnd();
						if (error.Trim().Length > 0)
						{
							Log.Warn().WriteLine("Error:\n{0}", error);
						}
					}
					Log.Info().WriteLine("Finished : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
					return process.ExitCode;
				}
			}
			return -1;
		}

		public static string FormatArguments(string arguments, string fullpath)
		{
			return string.Format(arguments, fullpath);
		}
	}
}