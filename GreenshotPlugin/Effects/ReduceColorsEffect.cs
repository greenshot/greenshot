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
using log4net;

#endregion

namespace GreenshotPlugin.Effects
{
	/// <summary>
	///     ReduceColorsEffect
	/// </summary>
	public class ReduceColorsEffect : IEffect
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ReduceColorsEffect));

		public ReduceColorsEffect()
		{
			Reset();
		}

		public int Colors { get; set; }

		public void Reset()
		{
			Colors = 256;
		}

		public Image Apply(Image sourceImage, Matrix matrix)
		{
			using (var quantizer = new WuQuantizer((Bitmap) sourceImage))
			{
				var colorCount = quantizer.GetColorCount();
				if (colorCount > Colors)
				{
					try
					{
						return quantizer.GetQuantizedImage(Colors);
					}
					catch (Exception e)
					{
						Log.Warn("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
					}
				}
			}
			return null;
		}
	}
}