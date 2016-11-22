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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;

#endregion

namespace Greenshot.Core.Gfx
{
	/// <summary>
	///     RotateEffect
	/// </summary>
	public class RotateEffect : IEffect
	{
		public RotateEffect(int angle)
		{
			Angle = angle;
		}

		public int Angle { get; set; }

		public void Reset()
		{
			// Angle doesn't have a default value
		}

		public string Name => Angle == 90 ? "editor_rotatecw" : "editor_rotateccw";

		public Image Apply(Image sourceImage, Matrix matrix)
		{
			RotateFlipType flipType;
			if (Angle == 90)
			{
				matrix.Rotate(90, MatrixOrder.Append);
				matrix.Translate(sourceImage.Height, 0, MatrixOrder.Append);
				flipType = RotateFlipType.Rotate90FlipNone;
			}
			else if ((Angle == -90) || (Angle == 270))
			{
				flipType = RotateFlipType.Rotate270FlipNone;
				matrix.Rotate(-90, MatrixOrder.Append);
				matrix.Translate(0, sourceImage.Width, MatrixOrder.Append);
			}
			else
			{
				throw new NotSupportedException("Currently only an angle of 90 or -90 (270) is supported.");
			}
			return ImageHelper.RotateFlip(sourceImage, flipType);
		}
	}
}