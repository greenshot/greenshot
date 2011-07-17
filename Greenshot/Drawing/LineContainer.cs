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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin.Drawing;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of LineContainer.
	/// </summary>
	[Serializable()] 
	public class LineContainer : DrawableContainer {
		public static readonly int MAX_CLICK_DISTANCE_TOLERANCE = 10;
		
		public LineContainer(Surface parent) : base(parent) {
			Init();
			AddField(GetType(), FieldType.LINE_THICKNESS, 1);
			AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldType.SHADOW, false);
		}
		
		private void Init() {
			grippers[1].Enabled = false;
			grippers[2].Enabled = false;
			grippers[3].Enabled = false;
			grippers[5].Enabled = false;
			grippers[6].Enabled = false;
			grippers[7].Enabled = false;
		}
		
		public override void Draw(Graphics g, RenderMode rm) {
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
			bool shadow = GetFieldValueAsBool(FieldType.SHADOW);

			if ( shadow && lineThickness > 0 ) {
				//draw shadow first
				int basealpha = 100;
				int alpha = basealpha;
				int steps = 5;
				int currentStep = 1;
				while (currentStep <= steps) {
					using (Pen shadowCapPen = new Pen(Color.FromArgb(alpha, 100, 100, 100))) {
						shadowCapPen.Width = lineThickness;
	
						g.DrawLine(shadowCapPen,
							this.Left + currentStep,
							this.Top + currentStep,
							this.Left + currentStep + this.Width,
							this.Top + currentStep + this.Height);
						
						currentStep++;
						alpha = alpha - (basealpha / steps);
					}
				}
			}


			using (Pen pen = new Pen(lineColor)) {
				pen.Width = lineThickness;
	
				if(pen.Width > 0) {
					g.DrawLine(pen, this.Left, this.Top, this.Left + this.Width, this.Top + this.Height);
				}
			}
		}
		
		public override bool ClickableAt(int x, int y) {
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			double distance = DrawingHelper.CalculateLinePointDistance(this.Left, this.Top, this.Left + this.Width, this.Top + this.Height, x, y);
			if (distance < 0) {
				return false;
			}
			return distance <= Math.Max(lineThickness / 2, MAX_CLICK_DISTANCE_TOLERANCE);
		}
		
	}
}
