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
using System.Runtime.Serialization;
using Greenshot.Addon.LegacyEditor.Drawing.Adorners;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx.Legacy;

namespace Greenshot.Addon.LegacyEditor.Drawing
{
	/// <summary>
	///     Description of LineContainer.
	/// </summary>
	[Serializable]
	public class LineContainer : DrawableContainer
	{
		public static readonly int MAX_CLICK_DISTANCE_TOLERANCE = 10;

        /// <summary>
        /// Constructor taking all dependencies
        /// </summary>
        /// <param name="parent">parent</param>
        /// <param name="editorConfiguration">IEditorConfiguration</param>
		public LineContainer(Surface parent, IEditorConfiguration editorConfiguration) : base(parent, editorConfiguration)
		{
			Init();
		}

        /// <inheritdoc />
		protected override void InitializeFields()
		{
			AddField(GetType(), FieldTypes.LINE_THICKNESS, 2);
			AddField(GetType(), FieldTypes.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldTypes.SHADOW, true);
		}

        /// <inheritdoc />
		protected override void OnDeserialized(StreamingContext context)
		{
			Init();
		}

        /// <summary>
        /// Initialize this container
        /// </summary>
		protected void Init()
		{
			Adorners.Add(new MoveAdorner(this, Positions.TopLeft));
			Adorners.Add(new MoveAdorner(this, Positions.BottomRight));
		}

        /// <inheritdoc />
		public override void Draw(Graphics graphics, RenderMode rm)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.None;

			var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS);
			var lineColor = GetFieldValueAsColor(FieldTypes.LINE_COLOR);
			var shadow = GetFieldValueAsBool(FieldTypes.SHADOW);

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
							graphics.DrawLine(shadowCapPen,
								Left + currentStep,
								Top + currentStep,
								Left + currentStep + Width,
								Top + currentStep + Height);

							currentStep++;
#pragma warning disable IDE0054 // Use compound assignment
                            alpha = alpha - basealpha / steps;
#pragma warning restore IDE0054 // Use compound assignment
                        }
					}
				}

				using (var pen = new Pen(lineColor, lineThickness))
				{
					graphics.DrawLine(pen, Left, Top, Left + Width, Top + Height);
				}
			}
		}

        /// <inheritdoc />
		public override bool ClickableAt(int x, int y)
		{
			var lineThickness = GetFieldValueAsInt(FieldTypes.LINE_THICKNESS) + 5;
			if (lineThickness > 0)
			{
				using (var pen = new Pen(Color.White))
				{
					pen.Width = lineThickness;
					using (var path = new GraphicsPath())
					{
						path.AddLine(Left, Top, Left + Width, Top + Height);
						return path.IsOutlineVisible(x, y, pen);
					}
				}
			}
			return false;
		}

        /// <inheritdoc />
		protected override IDoubleProcessor GetAngleRoundProcessor()
		{
			return LineAngleRoundBehavior.Instance;
		}
	}
}