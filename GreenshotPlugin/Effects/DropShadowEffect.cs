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

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using GreenshotPlugin.Core;

#endregion

namespace GreenshotPlugin.Effects
{
	/// <summary>
	///     DropShadowEffect
	/// </summary>
	[TypeConverter(typeof(EffectConverter))]
	public class DropShadowEffect : IEffect
	{
		public DropShadowEffect()
		{
			Reset();
		}

		public float Darkness { get; set; }

		public int ShadowSize { get; set; }

		public Point ShadowOffset { get; set; }

		public virtual void Reset()
		{
			Darkness = 0.6f;
			ShadowSize = 7;
			ShadowOffset = new Point(-1, -1);
		}

		public virtual Image Apply(Image sourceImage, Matrix matrix)
		{
			return sourceImage.CreateShadow(Darkness, ShadowSize, ShadowOffset, matrix, PixelFormat.Format32bppArgb);
		}
	}
}