﻿/*
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

using Greenshot.IniFile;
using GreenshotPlugin.Core;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Greenshot.Controls {
	/// <summary>
	/// ToolStripProfessionalRenderer which draws the Check correctly when the icons are larger
	/// </summary>
	public class ContextMenuToolStripProfessionalRenderer : ToolStripProfessionalRenderer {
		private static readonly CoreConfiguration coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
		private static Image scaledCheckbox;

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) {
			if (scaledCheckbox == null || scaledCheckbox.Size != coreConfiguration.IconSize) {
				if (scaledCheckbox != null) {
					scaledCheckbox.Dispose();
				}
				scaledCheckbox = ImageHelper.ResizeImage(e.Image, true, coreConfiguration.IconSize.Width, coreConfiguration.IconSize.Height, null);
			}
			Rectangle old = e.ImageRectangle;
			ToolStripItemImageRenderEventArgs clone = new ToolStripItemImageRenderEventArgs(e.Graphics, e.Item, scaledCheckbox, new Rectangle(old.X, 0, old.Width, old.Height));
			base.OnRenderItemCheck(clone);
		}
	}
}
