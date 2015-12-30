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

using System.Drawing;

namespace GreenshotPlugin.Extensions
{
	public static class RectangleExtensions
	{
		/// <summary>
		/// calculates the position of an element depending on the desired alignment within a RectangleF
		/// </summary>
		/// <param name="currentRect">the bounds of the element to be aligned</param>
		/// <param name="targetRect">the rectangle reference for aligment of the element</param>
		/// <param name="alignment">the System.Drawing.ContentAlignment value indicating how the element is to be aligned should the width or height differ from targetSize</param>
		/// <returns>a new RectangleF object with Location aligned aligned to targetRect</returns>
		public static RectangleF Align(this RectangleF currentRect, RectangleF targetRect, ContentAlignment alignment)
		{
			RectangleF newRect = new RectangleF(targetRect.Location, currentRect.Size);
			switch (alignment)
			{
				case ContentAlignment.TopCenter:
					newRect.X = (targetRect.Width - currentRect.Width)/2;
					break;
				case ContentAlignment.TopRight:
					newRect.X = (targetRect.Width - currentRect.Width);
					break;
				case ContentAlignment.MiddleLeft:
					newRect.Y = (targetRect.Height - currentRect.Height)/2;
					break;
				case ContentAlignment.MiddleCenter:
					newRect.Y = (targetRect.Height - currentRect.Height)/2;
					newRect.X = (targetRect.Width - currentRect.Width)/2;
					break;
				case ContentAlignment.MiddleRight:
					newRect.Y = (targetRect.Height - currentRect.Height)/2;
					newRect.X = (targetRect.Width - currentRect.Width);
					break;
				case ContentAlignment.BottomLeft:
					newRect.Y = (targetRect.Height - currentRect.Height);
					break;
				case ContentAlignment.BottomCenter:
					newRect.Y = (targetRect.Height - currentRect.Height);
					newRect.X = (targetRect.Width - currentRect.Width)/2;
					break;
				case ContentAlignment.BottomRight:
					newRect.Y = (targetRect.Height - currentRect.Height);
					newRect.X = (targetRect.Width - currentRect.Width);
					break;
			}
			return newRect;
		}

		/// <summary>
		/// Extension for creating rectangles with positive dimensions, regardless of input coordinates
		/// </summary>
		public static Rectangle MakeGuiRectangle(this Rectangle rect)
		{
			if (rect.Width < 0)
			{
				rect.X += rect.Width;
				rect.Width = -rect.Width;
			}
			if (rect.Height < 0)
			{
				rect.Y += rect.Height;
				rect.Height = -rect.Height;
			}
			return rect;
		}

		/// <summary>
		/// Extension for creating rectangles with positive dimensions, regardless of input coordinates
		/// </summary>
		public static RectangleF MakeGuiRectangle(this RectangleF rect)
		{
			if (rect.Width < 0)
			{
				rect.X += rect.Width;
				rect.Width = -rect.Width;
			}
			if (rect.Height < 0)
			{
				rect.Y += rect.Height;
				rect.Height = -rect.Height;
			}
			return rect;
		}
	}
}