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
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Helpers;

#endregion

namespace Greenshot.Drawing
{
	/// <summary>
	///     empty container for filter-only elements
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
		}

		public enum PreparedFilterMode
		{
			OBFUSCATE,
			HIGHLIGHT
		}

		public FilterContainer(Surface parent) : base(parent)
		{
			Init();
		}

		public PreparedFilter Filter
		{
			get { return (PreparedFilter) GetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT); }
		}

		protected override void OnDeserialized(StreamingContext streamingContext)
		{
			base.OnDeserialized(streamingContext);
			Init();
		}

		private void Init()
		{
			CreateDefaultAdorners();
		}

		protected override void InitializeFields()
		{
			AddField(GetType(), FieldType.LINE_THICKNESS, 0);
			AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
			AddField(GetType(), FieldType.SHADOW, false);
		}

		public override void Draw(Graphics graphics, RenderMode rm)
		{
			var lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
			var lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR);
			var shadow = GetFieldValueAsBool(FieldType.SHADOW);
			var lineVisible = lineThickness > 0 && Colors.IsVisible(lineColor);
			if (lineVisible)
			{
				graphics.SmoothingMode = SmoothingMode.HighSpeed;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.None;
				//draw shadow first
				if (shadow)
				{
					var basealpha = 100;
					var alpha = basealpha;
					var steps = 5;
					var currentStep = lineVisible ? 1 : 0;
					while (currentStep <= steps)
					{
						using (var shadowPen = new Pen(Color.FromArgb(alpha, 100, 100, 100), lineThickness))
						{
							var shadowRect = new NativeRect(Left + currentStep, Top + currentStep, Width, Height).Normalize();
							graphics.DrawRectangle(shadowPen, shadowRect);
							currentStep++;
							alpha = alpha - basealpha / steps;
						}
					}
				}
				var rect = new NativeRect(Left, Top, Width, Height).Normalize();
				if (lineThickness > 0)
				{
					using (var pen = new Pen(lineColor, lineThickness))
					{
						graphics.DrawRectangle(pen, rect);
					}
				}
			}
		}
	}
}