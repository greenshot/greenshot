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
	public class ArrowContainer : LineContainer {
		public enum ArrowHeadCombination { NONE, START_POINT, END_POINT, BOTH };
		
		private static readonly AdjustableArrowCap ARROW_CAP = new AdjustableArrowCap(4, 6);

		public ArrowContainer(Surface parent) : base(parent) {
			AddField(GetType(), FieldType.ARROWHEADS, 2);
			AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
			AddField(GetType(), FieldType.SHADOW, false);
			AddField(GetType(), FieldType.ARROWHEADS, Greenshot.Drawing.ArrowContainer.ArrowHeadCombination.END_POINT);
		}

		public override void Draw(Graphics g, RenderMode rm) {
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
			bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
			ArrowHeadCombination heads = (ArrowHeadCombination)GetFieldValue(FieldType.ARROWHEADS);;

			if ( shadow && lineThickness > 0 ) {
				//draw shadow first
				int basealpha = 100;
				int alpha = basealpha;
				int steps = 5;
				int currentStep = 1;
				while ( currentStep <= steps ) {
					using (Pen shadowCapPen = new Pen(Color.FromArgb(alpha, 100, 100, 100))) {
						shadowCapPen.Width = lineThickness;
	
						if ( heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.START_POINT ) shadowCapPen.CustomStartCap = ARROW_CAP;
						if ( heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.END_POINT ) shadowCapPen.CustomEndCap = ARROW_CAP;
	
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
	
				if ( pen.Width > 0 ) {
					if ( heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.START_POINT ) pen.CustomStartCap = ARROW_CAP;
					if ( heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.END_POINT ) pen.CustomEndCap = ARROW_CAP;
	
					g.DrawLine(pen, this.Left, this.Top, this.Left + this.Width, this.Top + this.Height);
				}
				
			}
		}
		
		public override Rectangle DrawingBounds {
			get {
				int lineThickness = 0;
				if (HasField(FieldType.LINE_THICKNESS)) {
					lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
				}
				int arrowCapWidth = 0;
				int arrowCapHeight = 0;
				if (HasField(FieldType.ARROWHEADS)) {
					ArrowHeadCombination heads = (ArrowHeadCombination)GetFieldValue(FieldType.ARROWHEADS);
					if(heads.Equals(ArrowHeadCombination.START_POINT) || heads.Equals(ArrowHeadCombination.END_POINT) || heads.Equals(ArrowHeadCombination.BOTH)) {
						arrowCapWidth = (int)ARROW_CAP.Width * lineThickness;
						arrowCapHeight = (int)ARROW_CAP.Height * lineThickness;
					}
				}
	
				int offset = Math.Max(lineThickness/2, Math.Max(arrowCapWidth, arrowCapHeight));
				return new Rectangle(Bounds.Left-offset, Bounds.Top-offset, Bounds.Width+offset*2, Bounds.Height+offset*2);
				
			}
		}
		
		public override bool ClickableAt(int x, int y) {
			bool ret = false;
			ret = base.ClickableAt(x, y);
			if(!ret) {
				// line has not been clicked, check arrow heads
				ArrowHeadCombination heads = (ArrowHeadCombination)GetFieldValue(FieldType.ARROWHEADS);
				if(!ArrowHeadCombination.NONE.Equals(heads)) {
					int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
					double arrowCapHalfWidth = ARROW_CAP.Width * lineThickness / 2;
					double arrowCapHeight = ARROW_CAP.Height * lineThickness;
					// we have to check only if arrow heads are wider than tolerated area of LineContainer
					if(arrowCapHalfWidth > MAX_CLICK_DISTANCE_TOLERANCE) {
						double mouseToLineDist = DrawingHelper.CalculateLinePointDistance(Left, Top, Left+Width, Top+Height, x, y);
						if(mouseToLineDist > -1) { // point next to line at all?
							if(heads.Equals(ArrowHeadCombination.END_POINT) || heads.Equals(ArrowHeadCombination.BOTH)) {
								// calculate a perpendicular line at arrow tip to hittest arrow head easily
								int p1x = Left + Width + Height;
								int p1y = Top + Height - Width;
								int p2x = Left + Width - Height;
								int p2y = Top + Height + Width;
								
								double mouseToPerpDist = DrawingHelper.CalculateLinePointDistance(p1x,p1y,p2x,p2y, x, y);
								if(
								   // point located within rectangular area of width/height of arrowhead?
								   mouseToLineDist <= arrowCapHalfWidth 
								   && mouseToPerpDist <= arrowCapHeight
								   // point located within arrowhead? (nearer to line than to perp * w/h-factor)
								   && mouseToLineDist < mouseToPerpDist * arrowCapHalfWidth / arrowCapHeight) {
									return true;
								}
							} 
							if(heads.Equals(ArrowHeadCombination.START_POINT) || heads.Equals(ArrowHeadCombination.BOTH)) {
								// calculate a perpendicular line at arrow tip to hittest arrow head easily
								int p1x = Left - Height;
								int p1y = Top + Width;
								int p2x = Left + Height;
								int p2y = Top - Width;
								
								double mouseToPerpDist = DrawingHelper.CalculateLinePointDistance(p1x,p1y,p2x,p2y, x, y);
								if(
								   // point located within rectangular area of width/height of arrowhead?
								   mouseToLineDist <= arrowCapHalfWidth 
								   && mouseToPerpDist <= arrowCapHeight
								   // point located within arrowhead? (nearer to line than to perp * w/h-factor)
								   && mouseToLineDist < mouseToPerpDist * arrowCapHalfWidth / arrowCapHeight) {
									return true;
								}
								
							
							}
						}
						
					}
				}
				
			}
			
			return ret;
			  
			/*int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			double distance = DrawingHelper.CalculateLinePointDistance(this.Left, this.Top, this.Left + this.Width, this.Top + this.Height, x, y);
			if (distance < 0) {
				return false;
			}
			return distance <= Math.Max(lineThickness / 2, 10);*/
		}

	}
}
