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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Drawing {
	/// <summary>
	/// Represents a rectangular shape on the Surface
	/// </summary>
	[Serializable] 
	public class RectangleContainer : DrawableContainer {

		public RectangleContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration) {
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
			AddField(GetType(), FieldTypes.LINE_THICKNESS, 2);
			AddField(GetType(), FieldTypes.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldTypes.FILL_COLOR, Color.Transparent);
			AddField(GetType(), FieldTypes.SHADOW, true);
		}
		
		public override void Draw(Graphics graphics, RenderMode rm) {
			int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
            var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR, Color.Red);
            var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR, Color.Transparent);
			bool shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();

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
		public static void DrawRectangle(NativeRect rect, Graphics graphics, RenderMode rm, int lineThickness, Color lineColor, Color fillColor, bool shadow) {
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			bool lineVisible = lineThickness > 0 && Colors.IsVisible(lineColor);
			if (shadow && (lineVisible || Colors.IsVisible(fillColor))) {
				//draw shadow first
				int basealpha = 100;
				int alpha = basealpha;
				int steps = 5;
				int currentStep = lineVisible ? 1 : 0;
				while (currentStep <= steps) {
					using (var shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100))) {
						shadowPen.Width = lineVisible ? lineThickness : 1;
						var shadowRect = new NativeRect(
							rect.Left + currentStep,
							rect.Top + currentStep,
                            rect.Width,
                            rect.Height).Normalize();
						graphics.DrawRectangle(shadowPen, shadowRect);
						currentStep++;
						alpha = alpha - basealpha / steps;
					}
				}
			}


			if (Colors.IsVisible(fillColor)) {
				using (Brush brush = new SolidBrush(fillColor)) {
					graphics.FillRectangle(brush, rect);
				}
			}

			graphics.SmoothingMode = SmoothingMode.HighSpeed;
			if (lineVisible) {
				using (var pen = new Pen(lineColor, lineThickness)) {
					graphics.DrawRectangle(pen, rect);
				}
			}

		}
		public override bool ClickableAt(int x, int y) {
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();
			int lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS) + 10;
            var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);

			return RectangleClickableAt(rect, lineThickness, fillColor, x, y);
		}


		public static bool RectangleClickableAt(NativeRect rect, int lineThickness, Color fillColor, int x, int y) {

			// If we clicked inside the rectangle and it's visible we are clickable at.
			if (!Color.Transparent.Equals(fillColor)) {
				if (rect.Contains(x,y)) {
					return true;
				}
			}

			// check the rest of the lines
			if (lineThickness > 0) {
				using (var pen = new Pen(Color.White, lineThickness)) {
					using (var path = new GraphicsPath()) {
						path.AddRectangle(rect);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			}
			return false;
		}
	}
}
