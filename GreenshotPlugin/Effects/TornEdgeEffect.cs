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
	///     TornEdgeEffect extends on DropShadowEffect
	/// </summary>
	[TypeConverter(typeof(EffectConverter))]
	public sealed class TornEdgeEffect : DropShadowEffect
	{
		public TornEdgeEffect()
		{
			Reset();
		}

		public int ToothHeight { get; set; }

		public int HorizontalToothRange { get; set; }

		public int VerticalToothRange { get; set; }

		public bool[] Edges { get; set; }

		public bool GenerateShadow { get; set; }

		public override void Reset()
		{
			base.Reset();
			ShadowSize = 7;
			ToothHeight = 12;
			HorizontalToothRange = 20;
			VerticalToothRange = 20;
			Edges = new[] {true, true, true, true};
			GenerateShadow = true;
		}

		public override Image Apply(Image sourceImage, Matrix matrix)
		{
			var tmpTornImage = sourceImage.CreateTornEdge(ToothHeight, HorizontalToothRange, VerticalToothRange, Edges);
			if (GenerateShadow)
			{
				using (tmpTornImage)
				{
					return tmpTornImage.CreateShadow(Darkness, ShadowSize, ShadowOffset, matrix, PixelFormat.Format32bppArgb);
				}
			}
			return tmpTornImage;
		}
	}
}