/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using IniFile;

namespace PluginExample {
	/// <summary>
	/// This processor shows how the current surface could be modified before it's passed to the editor or another destination
	/// </summary>
	public class GreyscaleProcessor : AbstractProcessor {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(GreyscaleProcessor));
		private static PluginExampleConfiguration conf = IniConfig.GetIniSection<PluginExampleConfiguration>();
		private IGreenshotHost host;
		
		public GreyscaleProcessor(IGreenshotHost host) {
			this.host = host;
		}

		public override string Designation {
			get {
				return "Greyscale";
			}
		}

		public override string Description {
			get {
				return Designation;
			}
		}
		
		public override bool isActive {
			get {
				return conf.GreyscaleProcessor;
			}
		}
		
		public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails) {
			LOG.DebugFormat("Changing surface to grayscale!");
			using (BitmapBuffer bbb = new BitmapBuffer(surface.Image as Bitmap, false)) {
				bbb.Lock();
				for(int y=0;y<bbb.Height; y++) {
					for(int x=0;x<bbb.Width; x++) {
						Color color = bbb.GetColorAt(x, y);
						int luma  = (int)((0.3*color.R) + (0.59*color.G) + (0.11*color.B));
						color = Color.FromArgb(luma, luma, luma);
						bbb.SetColorAt(x, y, color);
					}
				}
			}

			return true;
		}
	}
}
