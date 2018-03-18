#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Drawing.Fields;

#endregion

namespace Greenshot.Drawing
{
	/// <summary>
	///     Description of LineContainer.
	/// </summary>
	[Serializable]
	public class ArrowContainer : LineContainer
	{
		public enum ArrowHeadCombination
		{
			NONE,
			START_POINT,
			END_POINT,
			BOTH
		}

		private static readonly AdjustableArrowCap ARROW_CAP = new AdjustableArrowCap(4, 6);

		public ArrowContainer(Surface parent) : base(parent)
		{
		}

		public override NativeRect DrawingBounds
		{
			get
			{
				var lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
				if (lineThickness > 0)
				{
					using (var pen = new Pen(Color.White))
					{
						pen.Width = lineThickness;
						SetArrowHeads((ArrowHeadCombination) GetFieldValue(FieldType.ARROWHEADS), pen);
						using (var path = new GraphicsPath())
						{
							path.AddLine(Left, Top, Left + Width, Top + Height);
							using (var matrix = new Matrix())
							{
								NativeRectFloat drawingBounds = path.GetBounds(matrix, pen);
								return drawingBounds.Inflate(2, 2).Round();
							}
						}
					}
				}
				return NativeRect.Empty;
			}
		}

		/// <summary>
		///     Do not use the base, just override so we have our own defaults
		/// </summary>
		protected override void InitializeFields()
		{
			AddField(GetType(), FieldType.LINE_THICKNESS, 2);
			AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
			AddField(GetType(), FieldType.SHADOW, true);
			AddField(GetType(), FieldType.ARROWHEADS, ArrowHeadCombination.END_POINT);
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			var lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			var shadow = GetFieldValueAsBool(FieldType.SHADOW);

			if (lineThickness > 0)
			{
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.None;
				var lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
				var heads = (ArrowHeadCombination) GetFieldValue(FieldType.ARROWHEADS);
				if (lineThickness > 0)
				{
					if (shadow)
					{
						//draw shadow first
						var basealpha = 100;
						var alpha = basealpha;
						var steps = 5;
						var currentStep = 1;
						while (currentStep <= steps)
						{
							using (var shadowCapPen = new Pen(Color.FromArgb(alpha, 100, 100, 100), lineThickness))
							{
								SetArrowHeads(heads, shadowCapPen);

								graphics.DrawLine(shadowCapPen,
									Left + currentStep,
									Top + currentStep,
									Left + currentStep + Width,
									Top + currentStep + Height);

								currentStep++;
								alpha = alpha - basealpha / steps;
							}
						}
					}
					using (var pen = new Pen(lineColor, lineThickness))
					{
						SetArrowHeads(heads, pen);
						graphics.DrawLine(pen, Left, Top, Left + Width, Top + Height);
					}
				}
			}
		}

		private void SetArrowHeads(ArrowHeadCombination heads, Pen pen)
		{
			if (heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.START_POINT)
			{
				pen.CustomStartCap = ARROW_CAP;
			}
			if (heads == ArrowHeadCombination.BOTH || heads == ArrowHeadCombination.END_POINT)
			{
				pen.CustomEndCap = ARROW_CAP;
			}
		}

		public override bool ClickableAt(int x, int y)
		{
			var lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS) + 10;
			if (lineThickness > 0)
			{
				using (var pen = new Pen(Color.White))
				{
					pen.Width = lineThickness;
					SetArrowHeads((ArrowHeadCombination) GetFieldValue(FieldType.ARROWHEADS), pen);
					using (var path = new GraphicsPath())
					{
						path.AddLine(Left, Top, Left + Width, Top + Height);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			}
			return false;
		}
	}
}