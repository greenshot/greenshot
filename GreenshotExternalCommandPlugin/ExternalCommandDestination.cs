/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotExternalCommandPlugin {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class ExternalCommandDestination : AbstractDestination {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandDestination));
		private static readonly Regex URI_REGEXP = new Regex(@"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)");
		private static readonly ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private readonly string _presetCommand;
		
		public ExternalCommandDestination(string commando) {
			_presetCommand = commando;
		}

		public override string Designation => "External " + _presetCommand.Replace(',','_');

		public override string Description => _presetCommand;

		public override IEnumerable<IDestination> DynamicDestinations() {
			yield break;
		}

		public override Image DisplayIcon => IconCache.IconForCommand(_presetCommand);

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings();
			outputSettings.PreventGreenshotFormat();

			if (_presetCommand != null) {
				if (!config.RunInbackground.ContainsKey(_presetCommand)) {
					config.RunInbackground.Add(_presetCommand, true);
				}
				bool runInBackground = config.RunInbackground[_presetCommand];
				string fullPath = captureDetails.Filename ?? ImageOutput.SaveNamedTmpFile(surface, captureDetails, outputSettings);

                string output;
				string error;
				if (runInBackground) {
					Thread commandThread = new Thread(delegate()
					{
						CallExternalCommand(exportInformation, fullPath, out output, out error);
						ProcessExport(exportInformation, surface);
					})
					{
						Name = "Running " + _presetCommand,
						IsBackground = true
					};
					commandThread.SetApartmentState(ApartmentState.STA);
					commandThread.Start();
					exportInformation.ExportMade = true;
				} else {
					CallExternalCommand(exportInformation, fullPath, out output, out error);
					ProcessExport(exportInformation, surface);
				}
			}
			return exportInformation;
		}

		/// <summary>
		/// Wrapper method for the background and normal call, this does all the logic:
		/// Call the external command, parse for URI, place to clipboard and set the export information
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="fullPath"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		private void CallExternalCommand(ExportInformation exportInformation, string fullPath, out string output, out string error) {
			output = null;
			error = null;
			try {
				if (CallExternalCommand(_presetCommand, fullPath, out output, out error) == 0) {
					exportInformation.ExportMade = true;
					if (!string.IsNullOrEmpty(output)) {
						MatchCollection uriMatches = URI_REGEXP.Matches(output);
						// Place output on the clipboard before the URI, so if one is found this overwrites
						if (config.OutputToClipboard) {
							ClipboardHelper.SetClipboardData(output);
						}
						if (uriMatches.Count > 0) {
							exportInformation.Uri = uriMatches[0].Groups[1].Value;
							LOG.InfoFormat("Got URI : {0} ", exportInformation.Uri);
							if (config.UriToClipboard) {
								ClipboardHelper.SetClipboardData(exportInformation.Uri);
							}
						}
					}
				} else {
					LOG.WarnFormat("Error calling external command: {0} ", output);
					exportInformation.ExportMade = false;
					exportInformation.ErrorMessage = error;
				}
			} catch (Exception ex) {				
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
				LOG.WarnFormat("Error calling external command: {0} ", exportInformation.ErrorMessage);
			}
		}

		/// <summary>
		/// Wrapper to retry with a runas
		/// </summary>
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		private int CallExternalCommand(string commando, string fullPath, out string output, out string error) {
			try {
				return CallExternalCommand(commando, fullPath, null, out output, out error);
			} catch (Win32Exception w32Ex) {
				try {
					return CallExternalCommand(commando, fullPath, "runas", out output, out error);
				} catch {
					w32Ex.Data.Add("commandline", config.Commandline[_presetCommand]);
					w32Ex.Data.Add("arguments", config.Argument[_presetCommand]);
					throw;
				}
			} catch (Exception ex) {
				ex.Data.Add("commandline", config.Commandline[_presetCommand]);
				ex.Data.Add("arguments", config.Argument[_presetCommand]);
				throw;
			}
		}

		/// <summary>
		/// The actual executing code for the external command
		/// </summary>
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="verb"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		private int CallExternalCommand(string commando, string fullPath, string verb, out string output, out string error) {
			string commandline = config.Commandline[commando];
			string arguments = config.Argument[commando];
			output = null;
			error = null;
			if (!string.IsNullOrEmpty(commandline))
            {
                using Process process = new Process();
                // Fix variables
                commandline = FilenameHelper.FillVariables(commandline, true);
                commandline = FilenameHelper.FillCmdVariables(commandline, true);

                arguments = FilenameHelper.FillVariables(arguments, false);
                arguments = FilenameHelper.FillCmdVariables(arguments, false);

                process.StartInfo.FileName = FilenameHelper.FillCmdVariables(commandline, true);
                process.StartInfo.Arguments = FormatArguments(arguments, fullPath);
                process.StartInfo.UseShellExecute = false;
                if (config.RedirectStandardOutput) {
                    process.StartInfo.RedirectStandardOutput = true;
                }
                if (config.RedirectStandardError) {
                    process.StartInfo.RedirectStandardError = true;
                }
                if (verb != null) {
                    process.StartInfo.Verb = verb;
                }
                LOG.InfoFormat("Starting : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
                process.Start();
                process.WaitForExit();
                if (config.RedirectStandardOutput) {
                    output = process.StandardOutput.ReadToEnd();
                    if (config.ShowStandardOutputInLog && output.Trim().Length > 0) {
                        LOG.InfoFormat("Output:\n{0}", output);
                    }
                }
                if (config.RedirectStandardError) {
                    error = process.StandardError.ReadToEnd();
                    if (error.Trim().Length > 0) {
                        LOG.WarnFormat("Error:\n{0}", error);
                    }
                }
                LOG.InfoFormat("Finished : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
                return process.ExitCode;
            }
			return -1;
		}

		public static string FormatArguments(string arguments, string fullpath)
		{
			return string.Format(arguments, fullpath);
		}
	}
}
