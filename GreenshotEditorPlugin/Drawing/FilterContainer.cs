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
using GreenshotPlugin.Extensions;
using System.Drawing.Drawing2D;
using GreenshotEditorPlugin.Helpers;
using GreenshotEditorPlugin.Drawing.Filters;
using GreenshotPlugin.Interfaces.Drawing;

namespace GreenshotEditorPlugin.Drawing
{
	/// <summary>
	/// empty container for filter-only elements
	/// </summary>
	[Serializable]
	public abstract class FilterContainer : DrawableContainer
	{
		public enum PreparedFilter
		{
			BLUR,
			PIXELIZE,
			TEXT_HIGHTLIGHT,
			AREA_HIGHLIGHT,
			GRAYSCALE,
			MAGNIFICATION
		};

		private int _lineThickness;

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

		public abstract PreparedFilter Filter
		{
			get;
			set;
		}

		public FilterContainer(Surface parent) : base(parent)
		{
		}

		protected void ConfigurePreparedFilters()
		{
			Filters.Clear();
			switch (Filter)
			{
				case PreparedFilter.BLUR:
					Add(new BlurFilter(this));
					break;
				case PreparedFilter.PIXELIZE:
					Add(new PixelizationFilter(this));
					break;
				case PreparedFilter.TEXT_HIGHTLIGHT:
					Add(new HighlightFilter(this));
					break;
				case PreparedFilter.AREA_HIGHLIGHT:
					AbstractFilter bf = new BrightnessFilter(this);
					bf.Invert = true;
					Add(bf);
					bf = new BlurFilter(this);
					bf.Invert = true;
					Add(bf);
					break;
				case PreparedFilter.GRAYSCALE:
					AbstractFilter f = new GrayscaleFilter(this);
					f.Invert = true;
					Add(f);
					break;
				case PreparedFilter.MAGNIFICATION:
					Add(new MagnifierFilter(this));
					break;
			}
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			bool lineVisible = (_lineThickness > 0 && ColorHelper.IsVisible(_lineColor));
			var state = graphics.Save();
			if (lineVisible)
			{
				graphics.SmoothingMode = SmoothingMode.HighSpeed;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.None;
				//draw _shadow first
				if (_shadow)
				{
					int basealpha = 100;
					int alpha = basealpha;
					int steps = 5;
					int currentStep = lineVisible ? 1 : 0;
					while (currentStep <= steps)
					{
						using (Pen shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100), _lineThickness))
						{
							Rectangle shadowRect = new Rectangle(Left + currentStep, Top + currentStep, Width, Height).MakeGuiRectangle();
							graphics.DrawRectangle(shadowPen, shadowRect);
							currentStep++;
							alpha = alpha - (basealpha/steps);
						}
					}
				}
				Rectangle rect = new Rectangle(Left, Top, Width, Height).MakeGuiRectangle();
				if (_lineThickness > 0)
				{
					using (Pen pen = new Pen(_lineColor, _lineThickness))
					{
						graphics.DrawRectangle(pen, rect);
					}
				}
				graphics.Restore(state);
			}
		}
	}
}