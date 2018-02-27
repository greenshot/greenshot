/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;

namespace GreenshotLutimPlugin  {
	/// <summary>
	/// Description of LutimDestination.
	/// </summary>
	public class LutimDestination : AbstractDestination {
		private readonly LutimPlugin _plugin;

		public LutimDestination(LutimPlugin plugin) {
			_plugin = plugin;
		}
		
		public override string Designation => "Lutim";

		public override string Description => Language.GetString("lutim", LangKey.upload_menu_item);

		public override Bitmap DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(LutimPlugin));
				return (Bitmap)resources.GetObject("Lutim");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			string uploadUrl;
			exportInformation.ExportMade = _plugin.Upload(captureDetails, surface, out uploadUrl);
			exportInformation.Uri = uploadUrl;
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
