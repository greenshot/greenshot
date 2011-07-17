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
using System.Runtime.Serialization;
using System.Windows.Forms;

using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin.Drawing;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of CropContainer.
	/// </summary>
	public class CropContainer : DrawableContainer {
		public CropContainer(Surface parent) : base(parent) {
			AddField(GetType(), FieldType.FLAGS, FieldType.Flag.CONFIRMABLE);
		}

		public override void Invalidate() {
			parent.Invalidate();
		}

		public override void Draw(Graphics g, RenderMode rm) {
			using (Brush cropBrush = new SolidBrush(Color.FromArgb(100, 150, 150, 100))) {
				Rectangle r = GuiRectangle.GetGuiRectangle(this.Left, this.Top, this.Width, this.Height);
				Rectangle selectionRect = new Rectangle(r.Left - 1, r.Top - 1, r.Width + 1, r.Height + 1);

				DrawSelectionBorder(g, selectionRect);
				
				// top
				g.FillRectangle(cropBrush, new Rectangle(0, 0, parent.Width, r.Top));
				// left
				g.FillRectangle(cropBrush, new Rectangle(0, r.Top, r.Left, r.Height));
				// right
				g.FillRectangle(cropBrush, new Rectangle(r.Left + r.Width, r.Top, parent.Width - (r.Left + r.Width), r.Height));
				// bottom
				g.FillRectangle(cropBrush, new Rectangle(0, r.Top + r.Height, parent.Width, parent.Height - (r.Top + r.Height)));
			}
		}
	}
}
