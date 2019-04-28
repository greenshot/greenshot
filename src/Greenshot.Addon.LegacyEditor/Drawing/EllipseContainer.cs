// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of EllipseContainer.
	/// </summary>
	[Serializable]
	public class EllipseContainer : DrawableContainer
	{
		public EllipseContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
        {
			CreateDefaultAdorners();
		}

		protected override void InitializeFields()
		{
			AddField(GetType(), FieldTypes.LINE_THICKNESS, 2);
			AddField(GetType(), FieldTypes.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldTypes.FILL_COLOR, Color.Transparent);
			AddField(GetType(), FieldTypes.SHADOW, true);
		}

		public override void Draw(Graphics graphics, RenderMode renderMode)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
			var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
			var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);
			var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();
			DrawEllipse(rect, graphics, renderMode, lineThickness, lineColor, fillColor, shadow);
		}

		/// <summary>
		///     This allows another container to draw an ellipse
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="graphics"></param>
		/// <param name="renderMode"></param>
		/// <param name="lineThickness"></param>
		/// <param name="lineColor"></param>
		/// <param name="fillColor"></param>
		/// <param name="shadow"></param>
		public static void DrawEllipse(NativeRect rect, Graphics graphics, RenderMode renderMode, int lineThickness, Color lineColor, Color fillColor, bool shadow)
		{
			var lineVisible = lineThickness > 0 && Colors.IsVisible(lineColor);
			// draw shadow before anything else
			if (shadow && (lineVisible || Colors.IsVisible(fillColor)))
			{
				var basealpha = 100;
				var alpha = basealpha;
				var steps = 5;
				var currentStep = lineVisible ? 1 : 0;
				while (currentStep <= steps)
				{
					using (var shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100)))
					{
						shadowPen.Width = lineVisible ? lineThickness : 1;
						var shadowRect = new NativeRect(rect.Left + currentStep, rect.Top + currentStep, rect.Width, rect.Height).Normalize();
						graphics.DrawEllipse(shadowPen, shadowRect);
						currentStep++;
						alpha = alpha - basealpha / steps;
					}
				}
			}
			//draw the original shape
			if (Colors.IsVisible(fillColor))
			{
				using (Brush brush = new SolidBrush(fillColor))
				{
					graphics.FillEllipse(brush, rect);
				}
			}
			if (lineVisible)
			{
				using (var pen = new Pen(lineColor, lineThickness))
				{
					graphics.DrawEllipse(pen, rect);
				}
			}
		}

		public override bool Contains(int x, int y)
		{
			return EllipseContains(this, x, y);
		}

		/// <summary>
		///     Allow the code to be used externally
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static bool EllipseContains(DrawableContainer caller, int x, int y)
		{
			double xDistanceFromCenter = x - (caller.Left + caller.Width / 2);
			double yDistanceFromCenter = y - (caller.Top + caller.Height / 2);
			// ellipse: x^2/a^2 + y^2/b^2 = 1
			return Math.Pow(xDistanceFromCenter, 2) / Math.Pow(caller.Width / 2, 2) + Math.Pow(yDistanceFromCenter, 2) / Math.Pow(caller.Height / 2, 2) < 1;
		}

		public override bool ClickableAt(int x, int y)
		{
			var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS) + 10;
			var fillColor = GetFieldValueAsColor(FieldTypes.FILL_COLOR);
			var rect = new NativeRect(Left, Top, Width, Height).Normalize();
			return EllipseClickableAt(rect, lineThickness, fillColor, x, y);
		}

		public static bool EllipseClickableAt(NativeRect rect, int lineThickness, Color fillColor, int x, int y)
		{
			// If we clicked inside the rectangle and it's visible we are clickable at.
			if (!Color.Transparent.Equals(fillColor))
			{
				if (rect.Contains(x, y))
				{
					return true;
				}
			}

			// check the rest of the lines
			if (lineThickness > 0)
			{
				using (var pen = new Pen(Color.White, lineThickness))
				{
					using (var path = new GraphicsPath())
					{
						path.AddEllipse(rect);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			}
			return false;
		}
	}
}