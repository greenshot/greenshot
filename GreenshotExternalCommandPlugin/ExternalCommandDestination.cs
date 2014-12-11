/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ExternalCommand {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class ExternalCommandDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandDestination));
		private static Regex URI_REGEXP = new Regex(@"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)");
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private string presetCommand;
		
		public ExternalCommandDestination(string commando) {
			this.presetCommand = commando;
		}

		public override string Designation {
			get {
				return "External " + presetCommand.Replace(',','_');
			}
		}

		public override string Description {
			get {
				return presetCommand;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			yield break;
		}

		public override Image DisplayIcon {
			get {
				return IconCache.IconForCommand(presetCommand);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings();

			
			if (presetCommand != null) {
				if (!config.runInbackground.ContainsKey(presetCommand)) {
					config.runInbackground.Add(presetCommand, true);
				}
				bool runInBackground = config.runInbackground[presetCommand];
				string fullPath = captureDetails.Filename;
				if (fullPath == null) {
					fullPath = ImageOutput.SaveNamedTmpFile(surface, captureDetails, outputSettings);
				}

				string output;
				string error;
				if (runInBackground) {
					Thread commandThread = new Thread(delegate() {
						CallExternalCommand(exportInformation, presetCommand, fullPath, out output, out error);
						ProcessExport(exportInformation, surface);
					});
					commandThread.Name = "Running " + presetCommand;
					commandThread.IsBackground = true;
					commandThread.SetApartmentState(ApartmentState.STA);
					commandThread.Start();
					exportInformation.ExportMade = true;
				} else {
					CallExternalCommand(exportInformation, presetCommand, fullPath, out output, out error);
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
		/// <param name="commando"></param>
		/// <param name="fullPath"></param>
		/// <param name="output"></param>
		/// <param name="error"></param>
		private void CallExternalCommand(ExportInformation exportInformation, string commando, string fullPath, out string output, out string error) {
			output = null;
			error = null;
			try {
				if (CallExternalCommand(presetCommand, fullPath, out output, out error) == 0) {
					exportInformation.ExportMade = true;
					if (!string.IsNullOrEmpty(output)) {
						MatchCollection uriMatches = URI_REGEXP.Matches(output);
						// Place output on the clipboard before the URI, so if one is found this overwrites
						if (config.OutputToClipboard) {
							ClipboardHelper.SetClipboardData(output);
						}
						if (uriMatches != null && uriMatches.Count >= 0) {
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
				LOG.WarnFormat("Error calling external command: {0} ", exportInformation.ErrorMessage);
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
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
			} catch (Win32Exception w32ex) {
				try {
					return CallExternalCommand(commando, fullPath, "runas", out output, out error);
				} catch {
					w32ex.Data.Add("commandline", config.commandlines[presetCommand]);
					w32ex.Data.Add("arguments", config.arguments[presetCommand]);
					throw;
				}
			} catch (Exception ex) {
				ex.Data.Add("commandline", config.commandlines[presetCommand]);
				ex.Data.Add("arguments", config.arguments[presetCommand]);
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
			string commandline = config.commandlines[commando];
			string arguments = config.arguments[commando];
			output = null;
			error = null;
			if (!string.IsNullOrEmpty(commandline)) {
				using (Process process = new Process()) {
					process.StartInfo.FileName = commandline;
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
						if (config.ShowStandardOutputInLog && output != null && output.Trim().Length > 0) {
							LOG.InfoFormat("Output:\n{0}", output);
						}
					}
					if (config.RedirectStandardError) {
						error = process.StandardError.ReadToEnd();
						if (error != null && error.Trim().Length > 0) {
							LOG.WarnFormat("Error:\n{0}", error);
						}
					}
					LOG.InfoFormat("Finished : {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
					return process.ExitCode;
				}
			}
			return -1;
		}

        public static string FormatArguments(string arguments, string fullpath)
        {
            return String.Format(arguments, fullpath);
        }
    }
}
