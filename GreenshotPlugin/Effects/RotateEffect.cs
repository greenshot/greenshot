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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using GreenshotPlugin.Core;

#endregion

namespace GreenshotPlugin.Effects
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

		public Image Apply(Image sourceImage, Matrix matrix)
		{
			RotateFlipType flipType;
			if (Angle == 90)
			{
				matrix.Rotate(90, MatrixOrder.Append);
				matrix.Translate(sourceImage.Height, 0, MatrixOrder.Append);
				flipType = RotateFlipType.Rotate90FlipNone;
			}
			else if (Angle == -90 || Angle == 270)
			{
				flipType = RotateFlipType.Rotate270FlipNone;
				matrix.Rotate(-90, MatrixOrder.Append);
				matrix.Translate(0, sourceImage.Width, MatrixOrder.Append);
			}
			else
			{
				throw new NotSupportedException("Currently only an angle of 90 or -90 (270) is supported.");
			}
			return sourceImage.ApplyRotateFlip(flipType);
		}
	}
}