/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using GreenshotEditorPlugin.Helpers;
using GreenshotPlugin.Interfaces.Drawing;

namespace GreenshotEditorPlugin.Drawing
{
	/// <summary>
	/// Description of LineContainer.
	/// </summary>
	[Serializable]
	public class LineContainer : DrawableContainer
	{
		public static readonly int MaxClickDistanceTolerance = 10;

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

		private bool _shadow;

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

		public LineContainer(Surface parent) : base(parent)
		{
			Init();
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			InitGrippers();
			DoLayout();
			Init();
		}

		protected void Init()
		{
			if (Grippers != null)
			{
				foreach (int index in new[]
				{
					1, 2, 3, 5, 6, 7
				})
				{
					Grippers[index].Enabled = false;
				}
			}
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			if (LineThickness > 0)
			{
				if (Shadow)
				{
					//draw _shadow first
					int basealpha = 100;
					int alpha = basealpha;
					int steps = 5;
					int currentStep = 1;
					while (currentStep <= steps)
					{
						using (var shadowCapPen = new Pen(Color.FromArgb(alpha, 100, 100, 100), LineThickness))
						{
							graphics.DrawLine(shadowCapPen, Left + currentStep, Top + currentStep, Left + currentStep + Width, Top + currentStep + Height);

							currentStep++;
							alpha = alpha - (basealpha/steps);
						}
					}
				}

				using (var pen = new Pen(LineColor, LineThickness))
				{
					graphics.DrawLine(pen, Left, Top, Left + Width, Top + Height);
				}
			}
		}

		public override bool ClickableAt(int x, int y)
		{
			int lineWidth = _lineThickness + 5;
			if (_lineThickness > 0)
			{
				using (var pen = new Pen(Color.White))
				{
					pen.Width = lineWidth;
					using (var path = new GraphicsPath())
					{
						path.AddLine(Left, Top, Left + Width, Top + Height);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			}
			else
			{
				return false;
			}
		}

		protected override ScaleHelper.IDoubleProcessor GetAngleRoundProcessor()
		{
			return ScaleHelper.LineAngleRoundBehavior.Instance;
		}
	}
}