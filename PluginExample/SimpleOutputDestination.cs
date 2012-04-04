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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace PluginExample {
	/// <summary>
	/// This destination shows a simple save to...
	/// </summary>
	public class SimpleOutputDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SimpleOutputDestination));
		private IGreenshotHost host;

		public SimpleOutputDestination(IGreenshotHost host) {
			this.host = host;
		}

		public override string Designation {
			get {
				return "SimpleOutput";
			}
		}

		public override string Description {
			get {
				return Designation;
			}
		}
		
		public override bool isActive {
			get {
				return true;
			}
		}
		
		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();
			string file = host.GetFilename(OutputFormat.png, null);
			string filePath = Path.Combine(config.OutputFilePath, file);
			using (FileStream stream = new FileStream(filePath, FileMode.Create)) {
				using (Image image = surface.GetImageForExport()) {
					host.SaveToStream(image, stream, OutputFormat.png, config.OutputFileJpegQuality, config.OutputFileReduceColors);
				}
			}
			MessageBox.Show("Saved test file to: " + filePath);
			return true;
		}
	}
}
