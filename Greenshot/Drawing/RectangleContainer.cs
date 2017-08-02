/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Greenshot.Drawing.Fields;
using Greenshot.Helpers;
using Greenshot.Plugin.Drawing;
using System.Runtime.Serialization;

namespace Greenshot.Drawing {
	/// <summary>
	/// Represents a rectangular shape on the Surface
	/// </summary>
	[Serializable] 
	public class RectangleContainer : DrawableContainer {

		public RectangleContainer(Surface parent) : base(parent) {
			Init();
		}

		/// <summary>
		/// Do some logic to make sure all field are initiated correctly
		/// </summary>
		/// <param name="streamingContext">StreamingContext</param>
		protected override void OnDeserialized(StreamingContext streamingContext)
		{
			base.OnDeserialized(streamingContext);
			Init();
		}

		private void Init()
		{
			CreateDefaultAdorners();
		}

		protected override void InitializeFields() {
			AddField(GetType(), FieldType.LINE_THICKNESS, 2);
			AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
			AddField(GetType(), FieldType.SHADOW, true);
		}
		
		public override void Draw(Graphics graphics, RenderMode rm) {
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR, Color.Red);
			Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR, Color.Transparent);
			bool shadow = GetFieldValueAsBool(FieldType.SHADOW);
			Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);

			DrawRectangle(rect, graphics, rm, lineThickness, lineColor, fillColor, shadow);
		}

		/// <summary>
		/// This method can also be used from other containers, if the right values are passed!
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="graphics"></param>
		/// <param name="rm"></param>
		/// <param name="lineThickness"></param>
		/// <param name="lineColor"></param>
		/// <param name="fillColor"></param>
		/// <param name="shadow"></param>
		public static void DrawRectangle(Rectangle rect, Graphics graphics, RenderMode rm, int lineThickness, Color lineColor, Color fillColor, bool shadow) {
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			bool lineVisible = lineThickness > 0 && Colors.IsVisible(lineColor);
			if (shadow && (lineVisible || Colors.IsVisible(fillColor)))
			{
				using (GraphicsPath bubble = new GraphicsPath())
				{

				Rectangle rect1 = GuiRectangle.GetGuiRectangle(rect.Left, rect.Top, rect.Width, rect.Height);
				Rectangle bubbleRect = GuiRectangle.GetGuiRectangle(0, 0, rect1.Width, rect1.Height);

				int smallerSideLength = Math.Min(Math.Abs(bubbleRect.Width), Math.Abs(bubbleRect.Height));
				int cornerRadius = Math.Min(3, smallerSideLength / 2 - lineThickness);

					if (cornerRadius > 0)
					{
						bubble.AddArc(bubbleRect.X, bubbleRect.Y, cornerRadius, cornerRadius, 180, 90);
						bubble.AddArc(bubbleRect.X + bubbleRect.Width - cornerRadius, bubbleRect.Y, cornerRadius, cornerRadius, 270, 90);
						bubble.AddArc(bubbleRect.X + bubbleRect.Width - cornerRadius, bubbleRect.Y + bubbleRect.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
						bubble.AddArc(bubbleRect.X, bubbleRect.Y + bubbleRect.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
						bubble.CloseAllFigures();
						using (Matrix bubbleMatrix = new Matrix())
						{
							bubbleMatrix.Translate(rect.Left, rect.Top);
							bubble.Transform(bubbleMatrix);
						}

						//GraphicsPath bubble = CreateBubble(lineThickness);

						using (Matrix shadowMatrix = new Matrix())
						//using (GraphicsPath bubbleClone = (GraphicsPath)bubble.Clone())
						{
							shadowMatrix.Translate(1, 1);
							DrawShadow(lineThickness, (alpha, currentStep, shadowPen, nil) =>
							{
								bubble.Transform(shadowMatrix);
								graphics.DrawPath(shadowPen, bubble);
							});
						}
					} else
						DrawShadow(lineThickness, (alpha, currentStep, shadowPen, nil) =>
						{
							var shadowRect = GuiRectangle.GetGuiRectangle(rect.Left + currentStep, rect.Top + currentStep, rect.Width, rect.Height);
							graphics.DrawRectangle(shadowPen, shadowRect);
						});

				}
			}

			if (Colors.IsVisible(fillColor)) {
				using (Brush brush = new SolidBrush(fillColor)) {
					graphics.FillRectangle(brush, rect);
				}
			}

			graphics.SmoothingMode = SmoothingMode.HighSpeed;
			if (lineVisible) {
				using (Pen pen = new Pen(lineColor, lineThickness)) {
					graphics.DrawRectangle(pen, rect);
				}
			}

		}
		public override bool ClickableAt(int x, int y) {
			Rectangle rect = GuiRectangle.GetGuiRectangle(Left, Top, Width, Height);
			int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS) + 10;
			Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR);

			return RectangleClickableAt(rect, lineThickness, fillColor, x, y);
		}


		public static bool RectangleClickableAt(Rectangle rect, int lineThickness, Color fillColor, int x, int y) {

			// If we clicked inside the rectangle and it's visible we are clickable at.
			if (!Color.Transparent.Equals(fillColor)) {
				if (rect.Contains(x,y)) {
					return true;
				}
			}

			// check the rest of the lines
			if (lineThickness > 0) {
				using (Pen pen = new Pen(Color.White, lineThickness)) {
					using (GraphicsPath path = new GraphicsPath()) {
						path.AddRectangle(rect);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			}
			return false;
		}
	}
}
