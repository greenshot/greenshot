/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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

namespace ExternalCommand {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class ExternalCommandDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandDestination));
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
				return IconCache.IconForExe(presetCommand);
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

				string output = null;
				if (runInBackground) {
					Thread commandThread = new Thread(delegate() {
						CallExternalCommand(presetCommand, fullPath, out output);
					});
					commandThread.Name = "Running " + presetCommand;
					commandThread.IsBackground = true;
					commandThread.Start();
					exportInformation.ExportMade = true;
				} else {
					try {
						if (CallExternalCommand(presetCommand, fullPath, out output) == 0) {
							exportInformation.ExportMade = true;
						} else {
							exportInformation.ErrorMessage = output;
						}
					} catch (Exception ex) {
						exportInformation.ErrorMessage = ex.Message;
					}
				}

				//exportInformation.Uri = "file://" + fullPath;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
		
		private int CallExternalCommand(string commando, string fullPath, out string output) {
			string commandline = config.commandlines[commando];
			string arguments = config.arguments[commando];
			output = null;
			if (commandline != null && commandline.Length > 0) {
				Process p = new Process();
				p.StartInfo.FileName = commandline;
				p.StartInfo.Arguments = String.Format(arguments, fullPath);
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				LOG.Info("Starting : " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
				p.Start();
				p.WaitForExit();
				output = p.StandardOutput.ReadToEnd();
				if (output != null && output.Trim().Length > 0) {
					LOG.Info("Output:\n" + output);
				}
				LOG.Info("Finished : " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
				return p.ExitCode;
			}
			return -1;
		}
	}
}
