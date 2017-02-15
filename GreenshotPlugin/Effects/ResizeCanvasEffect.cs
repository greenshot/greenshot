#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Drawing;
using System.Drawing.Drawing2D;
using GreenshotPlugin.Core;

#endregion

namespace GreenshotPlugin.Effects
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

		public int Left { get; set; }

		public int Right { get; set; }

		public int Top { get; set; }

		public int Bottom { get; set; }

		public Color BackgroundColor { get; set; }

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