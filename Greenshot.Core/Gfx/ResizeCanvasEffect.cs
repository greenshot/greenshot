//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Drawing;
using System.Drawing.Drawing2D;

#endregion

namespace Greenshot.Core.Gfx
{
	/// <summary>
	///     ResizeCanvasEffect
	/// </summary>
	public class ResizeCanvasEffect : IEffect
	{
		public ResizeCanvasEffect(int left, int right, int top, int bottom)
		{
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
			BackgroundColor = Color.Empty; // Uses the default background color depending on the format
		}

		public Color BackgroundColor { get; set; }

		public int Bottom { get; set; }

		public int Left { get; set; }

		public int Right { get; set; }

		public int Top { get; set; }

		public string Name
		{
			get { return "Effect"; }
		}

		public void Reset()
		{
			// values don't have a default value
		}

		public Image Apply(Image sourceImage, Matrix matrix)
		{
			return ImageHelper.ResizeCanvas(sourceImage, BackgroundColor, Left, Right, Top, Bottom, matrix);
		}
	}
}