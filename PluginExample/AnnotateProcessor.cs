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
using System.Drawing;
using System.Windows.Forms;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace PluginExample {
	/// <summary>
	/// This processor shows how the current surface could be modified before it's passed to the editor or another destination
	/// </summary>
	public class AnnotateProcessor : AbstractProcessor {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AnnotateProcessor));
		private static PluginExampleConfiguration conf = IniConfig.GetIniSection<PluginExampleConfiguration>();

		private IGreenshotHost host;
		
		public AnnotateProcessor(IGreenshotHost host) {
			this.host = host;
		}

		public override string Designation {
			get {
				return "Annotate";
			}
		}

		public override string Description {
			get {
				return Designation;
			}
		}
		
		public override bool isActive {
			get {
				return conf.AnnotationProcessor;
			}
		}
		
		public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails) {
			surface.SelectElement(surface.AddCursorContainer(Cursors.Hand, 100, 100));
			// Do something with the screenshot
			string title = captureDetails.Title;
			if (title != null) {
				LOG.Debug("Added title to surface: " + title);
				surface.SelectElement(surface.AddTextContainer(title, HorizontalAlignment.Center, VerticalAlignment.CENTER,
                       FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Red, Color.White));
			}
			surface.SelectElement(surface.AddTextContainer(Environment.UserName, HorizontalAlignment.Right, VerticalAlignment.TOP,
                       FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Red, Color.White));
			surface.SelectElement(surface.AddTextContainer(Environment.MachineName, HorizontalAlignment.Right, VerticalAlignment.BOTTOM,
                       FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Red, Color.White));
			surface.SelectElement(surface.AddTextContainer(captureDetails.DateTime.ToLongDateString(), HorizontalAlignment.Left, VerticalAlignment.BOTTOM,
                       FontFamily.GenericSansSerif, 12f, false, false, false, 2, Color.Red, Color.White));
			return true;
		}
	}
}
