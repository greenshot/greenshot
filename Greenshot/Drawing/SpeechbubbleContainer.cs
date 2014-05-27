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
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of SpeechbubbleContainer.
	/// </summary>
	[Serializable()]
	public class SpeechbubbleContainer : TextContainer {
		public SpeechbubbleContainer(Surface parent)
			: base(parent) {
		}

		/// <summary>
		/// We set our own field values
		/// </summary>
		protected override void InitializeFields() {
			AddField(GetType(), FieldType.LINE_THICKNESS, 0);
			AddField(GetType(), FieldType.LINE_COLOR, Color.White);
			AddField(GetType(), FieldType.SHADOW, false);
			AddField(GetType(), FieldType.FONT_ITALIC, false);
			AddField(GetType(), FieldType.FONT_BOLD, true);
			AddField(GetType(), FieldType.FILL_COLOR, Color.LightBlue);
			AddField(GetType(), FieldType.FONT_FAMILY, FontFamily.GenericSansSerif.Name);
			AddField(GetType(), FieldType.FONT_SIZE, 20f);
			AddField(GetType(), FieldType.TEXT_HORIZONTAL_ALIGNMENT, HorizontalAlignment.Center);
			AddField(GetType(), FieldType.TEXT_VERTICAL_ALIGNMENT, VerticalAlignment.CENTER);
		}

		protected override void TargetGripperMove(int absX, int absY) {
			base.TargetGripperMove(absX, absY);
			Invalidate();
		}

		/// <summary>
		/// Called from Surface (the _parent) when the drawing begins (mouse-down)
		/// </summary>
		/// <returns>true if the surface doesn't need to handle the event</returns>
		public override bool HandleMouseDown(int mouseX, int mouseY) {
			if (TargetGripper == null) {
				InitTargetGripper(Color.Green, new Point(mouseX, mouseY));
			}
			return base.HandleMouseDown(mouseX, mouseY);
		}

		public override Rectangle DrawingBounds {
			get {
				return new Rectangle(0, 0, _parent.Width, _parent.Height);
			}
		}
		public override void Draw(Graphics graphics, RenderMode renderMode) {
			if (TargetGripper == null) {
				return;
			}
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
			Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);

			bool lineVisible = (lineThickness > 0 && Colors.IsVisible(lineColor));
			Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);

			int tailAngle = 90 + (int)GeometryHelper.Angle2D(Left + (Width / 2), Top + (Height / 2), TargetGripper.Left, TargetGripper.Top);
			int tailLength = GeometryHelper.Distance2D(Left + (Width / 2), Top + (Height / 2), TargetGripper.Left, TargetGripper.Top);
			int tailWidth = (Math.Abs(Width) + Math.Abs(Height)) / 20;

			GraphicsPath bubble = new GraphicsPath();
			bubble.AddEllipse(0, 0, Math.Abs(rect.Width), Math.Abs(rect.Height));
			bubble.CloseAllFigures();

			GraphicsPath tail = new GraphicsPath();
			tail.AddLine(-tailWidth, 0, tailWidth, 0);
			tail.AddLine(tailWidth, 0, 0, -tailLength);
			tail.CloseFigure();

			GraphicsState state = graphics.Save();
			// draw the tail border where the bubble is not visible
			using (Region clipRegion = new Region(bubble)) {
				clipRegion.Translate(Left, Top);
				graphics.SetClip(clipRegion, CombineMode.Exclude);
				graphics.TranslateTransform(Left + (Width / 2), Top + (Height / 2));
				graphics.RotateTransform(tailAngle);
				using (Pen pen = new Pen(lineColor, lineThickness)) {
					graphics.DrawPath(pen, tail);
				}
			}
			graphics.Restore(state);


			if (Colors.IsVisible(fillColor)) {
				//draw the bubbleshape
				state = graphics.Save();
				graphics.TranslateTransform(Left, Top);
				using (Brush brush = new SolidBrush(fillColor)) {
					graphics.FillPath(brush, bubble);
				}
				graphics.Restore(state);
			}

			if (lineVisible) {
				//draw the bubble border
				state = graphics.Save();
				// Draw bubble where the Tail is not visible.
				using (Region clipRegion = new Region(tail)) {
					Matrix transformMatrix = new Matrix();
					transformMatrix.Rotate(tailAngle);
					clipRegion.Transform(transformMatrix);
					clipRegion.Translate(Left + (Width / 2), Top + (Height / 2));
					graphics.SetClip(clipRegion, CombineMode.Exclude);
					graphics.TranslateTransform(Left, Top);
					using (Pen pen = new Pen(lineColor, lineThickness)) {
						graphics.DrawPath(pen, bubble);
					}
				}
				graphics.Restore(state);
			}

			if (Colors.IsVisible(fillColor)) {
				// Draw the tail border
				state = graphics.Save();
				graphics.TranslateTransform(Left + (Width / 2), Top + (Height / 2));
				graphics.RotateTransform(tailAngle);
				using (Brush brush = new SolidBrush(fillColor)) {
					graphics.FillPath(brush, tail);
				}
				graphics.Restore(state);
			}

			// cleanup the paths
			bubble.Dispose();
			tail.Dispose();

			// Draw the text
			UpdateFormat();
			DrawText(graphics, rect, lineThickness, lineColor, false, StringFormat, Text, Font);


		}

		public override bool Contains(int x, int y) {
			double xDistanceFromCenter = x - (Left + Width / 2);
			double yDistanceFromCenter = y - (Top + Height / 2);
			// ellipse: x^2/a^2 + y^2/b^2 = 1
			return Math.Pow(xDistanceFromCenter, 2) / Math.Pow(Width / 2, 2) + Math.Pow(yDistanceFromCenter, 2) / Math.Pow(Height / 2, 2) < 1;
		}

		public override bool ClickableAt(int x, int y) {
			Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			int lineThicknessPlusSafety = lineThickness + 10;

			// If we clicked inside the rectangle and it's visible we are clickable at.
			Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);
			if (!Color.Transparent.Equals(fillColor)) {
				if (Contains(x, y)) {
					return true;
				}
			}

			// check the rest of the lines
			if (lineThicknessPlusSafety > 0) {
				using (Pen pen = new Pen(Color.White, lineThicknessPlusSafety)) {
					using (GraphicsPath path = new GraphicsPath()) {
						path.AddEllipse(rect);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			} else {
				return false;
			}
		}
	}
}
