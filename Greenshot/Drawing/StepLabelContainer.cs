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

using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of StepLabelContainer.
	/// This is an enumerated label, every single StepLabelContainer shows the number of the order it was created.
	/// To make sure that deleting recalculates, we check the location before every draw.
	/// </summary>
	[Serializable()]
	public class StepLabelContainer : TextContainer {
		public StepLabelContainer(Surface parent) : base(parent) {
			_defaultEditMode = EditStatus.IDLE;
			parent.StepContainers.AddLast(this);
			// Set defaults
			Width = 40;
			Height = 40;
		}

		public override void Dispose() {
			Parent.StepContainers.Remove(this);
			base.Dispose();
		}

		/// <summary>
		/// We set our own field values
		/// </summary>
		protected override void InitializeFields() {
			AddField(GetType(), FieldType.LINE_COLOR, Color.White);
			AddField(GetType(), FieldType.FILL_COLOR, Color.DarkRed);
			AddField(GetType(), FieldType.FONT_SIZE, 21f);
			AddField(GetType(), FieldType.LINE_THICKNESS, 0);
			base.InitializeFields();
		}

		/// <summary>
		/// Override the parent, calculate the label number, than draw
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		public override void Draw(Graphics graphics, RenderMode rm) {
			int number = 1;
			foreach (StepLabelContainer possibleThis in Parent.StepContainers) {
				if (possibleThis == this) {
					break;
				}
				if (Parent.IsOnSurface(possibleThis)) {
					number++;
				}
			}
			this.Text = number.ToString();
			base.Draw(graphics, rm);
		}
	}
}
