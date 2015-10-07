/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Plugin.Drawing;
using GreenshotEditorPlugin.Helpers;
using GreenshotPlugin.Extensions;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GreenshotEditorPlugin.Drawing
{
	/// <summary>
	/// Description of EllipseContainer.
	/// </summary>
	[Serializable]
	public class EllipseContainer : DrawableContainer
	{
		private int _lineThickness = 2;

		[Field(FieldTypes.LINE_THICKNESS)]
		public int LineThickness
		{
			get
			{
				return _lineThickness;
			}
			set
			{
				_lineThickness = value;
				OnFieldPropertyChanged(FieldTypes.LINE_THICKNESS);
			}
		}

		private Color _lineColor = Color.Red;

		[Field(FieldTypes.LINE_COLOR)]
		public Color LineColor
		{
			get
			{
				return _lineColor;
			}
			set
			{
				_lineColor = value;
				OnFieldPropertyChanged(FieldTypes.LINE_COLOR);
			}
		}

		private Color _fillColor = Color.Transparent;

		[Field(FieldTypes.FILL_COLOR)]
		public Color FillColor
		{
			get
			{
				return _fillColor;
			}
			set
			{
				_fillColor = value;
				OnFieldPropertyChanged(FieldTypes.FILL_COLOR);
			}
		}

		private bool _shadow = true;

		[Field(FieldTypes.SHADOW)]
		public bool Shadow
		{
			get
			{
				return _shadow;
			}
			set
			{
				_shadow = value;
				OnFieldPropertyChanged(FieldTypes.SHADOW);
			}
		}

		public EllipseContainer(Surface parent) : base(parent)
		{
		}

		public override void Draw(Graphics graphics, RenderMode renderMode)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			Rectangle rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			DrawEllipse(rect, graphics, renderMode, _lineThickness, _lineColor, _fillColor, _shadow);
		}

		/// <summary>
		/// This allows another container to draw an ellipse
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="graphics"></param>
		/// <param name="renderMode"></param>
		public static void DrawEllipse(Rectangle rect, Graphics graphics, RenderMode renderMode, int lineThickness, Color lineColor, Color fillColor, bool shadow)
		{
			bool lineVisible = (lineThickness > 0 && ColorHelper.IsVisible(lineColor));
			// draw _shadow before anything else
			if (shadow && (lineVisible || ColorHelper.IsVisible(fillColor)))
			{
				int basealpha = 100;
				int alpha = basealpha;
				int steps = 5;
				int currentStep = lineVisible ? 1 : 0;
				while (currentStep <= steps)
				{
					using (Pen shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100)))
					{
						shadowPen.Width = lineVisible ? lineThickness : 1;
						Rectangle shadowRect = new Rectangle(rect.Left + currentStep, rect.Top + currentStep, rect.Width, rect.Height).MakeGuiRectangle();
						graphics.DrawEllipse(shadowPen, shadowRect);
						currentStep++;
						alpha = alpha - basealpha/steps;
					}
				}
			}
			//draw the original shape
			if (ColorHelper.IsVisible(fillColor))
			{
				using (Brush brush = new SolidBrush(fillColor))
				{
					graphics.FillEllipse(brush, rect);
				}
			}
			if (lineVisible)
			{
				using (Pen pen = new Pen(lineColor, lineThickness))
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
		/// Allow the code to be used externally
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static bool EllipseContains(DrawableContainer caller, int x, int y)
		{
			double xDistanceFromCenter = x - (caller.Left + caller.Width/2);
			double yDistanceFromCenter = y - (caller.Top + caller.Height/2);
			// ellipse: x^2/a^2 + y^2/b^2 = 1
			return Math.Pow(xDistanceFromCenter, 2)/Math.Pow(caller.Width/2, 2) + Math.Pow(yDistanceFromCenter, 2)/Math.Pow(caller.Height/2, 2) < 1;
		}

		public override bool ClickableAt(int x, int y)
		{
			Rectangle rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
			int lineWidth = _lineThickness + 10;
			return EllipseClickableAt(rect, lineWidth, _fillColor, x, y);
		}

		public static bool EllipseClickableAt(Rectangle rect, int lineThickness, Color fillColor, int x, int y)
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
				using (Pen pen = new Pen(Color.White, lineThickness))
				{
					using (GraphicsPath path = new GraphicsPath())
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