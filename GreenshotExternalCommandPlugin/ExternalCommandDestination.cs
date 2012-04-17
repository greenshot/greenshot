/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Threading;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace ExternalCommand {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class ExternalCommandDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandDestination));
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private IGreenshotHost host;
		private string presetCommand;
		
		public ExternalCommandDestination(IGreenshotHost host, string commando) {
			this.host = host;
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
		
		public override bool isActive {
			get {
				return true;
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

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			string fullPath = captureDetails.Filename;
			if (fullPath == null) {
				using (Image image = surface.GetImageForExport()) {
					fullPath = host.SaveNamedTmpFile(image, captureDetails, OutputFormat.png, 100, false);
				}
			}
			if (presetCommand != null) {
				Thread commandThread = new Thread (delegate() {
					CallExternalCommand(presetCommand, fullPath);
				});
				commandThread.Name = "Running " + presetCommand;
				commandThread.IsBackground = true;
				commandThread.Start();
				surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString("exported_to", Description));
				surface.Modified = false;
			}
			return true;
		}
		
		private void CallExternalCommand(string commando, string fullPath) {
			string commandline = config.commandlines[commando];
			string arguments = config.arguments[commando];
			if (commandline != null && commandline.Length > 0) {
				Process p = new Process();
				p.StartInfo.FileName = commandline;
				p.StartInfo.Arguments = String.Format(arguments, fullPath);
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				LOG.Info("Starting : " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
				p.Start();
				string output = p.StandardOutput.ReadToEnd();
				if (output != null && output.Trim().Length > 0) {
					LOG.Info("Output:\n" + output);
				}
				LOG.Info("Finished : " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
			}					
		}
	}
}
