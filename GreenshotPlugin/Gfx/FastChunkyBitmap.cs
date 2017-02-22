#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;

#endregion

namespace GreenshotPlugin.Gfx
{
	/// <summary>
	///     This is the implementation of the FastBitmat for the 8BPP pixelformat
	/// </summary>
	public unsafe class FastChunkyBitmap : FastBitmap
	{
		private readonly Dictionary<Color, byte> _colorCache = new Dictionary<Color, byte>();
		// Used for indexed images
		private readonly Color[] _colorEntries;

		public FastChunkyBitmap(Bitmap source, Rectangle? area = null) : base(source, area)
		{
			_colorEntries = Bitmap.Palette.Entries;
		}

		/// <summary>
		///     Get the color from the specified location
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>Color</returns>
		public override Color GetColorAt(int x, int y)
		{
			var offset = x + y * Stride;
			var colorIndex = Pointer[offset];
			return _colorEntries[colorIndex];
		}

		/// <summary>
		///     Get the color from the specified location into the specified array
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="color">byte[4] as reference</param>
		public override void GetColorAt(int x, int y, byte[] color)
		{
			throw new NotImplementedException("No performance gain!");
		}

		/// <summary>
		///     Set the color at the specified location from the specified array
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="color">byte[4] as reference</param>
		public override void SetColorAt(int x, int y, byte[] color)
		{
			throw new NotImplementedException("No performance gain!");
		}

		/// <summary>
		///     Get the color-index from the specified location
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>byte with index</returns>
		public byte GetColorIndexAt(int x, int y)
		{
			var offset = x + y * Stride;
			return Pointer[offset];
		}

		/// <summary>
		///     Set the color-index at the specified location
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="colorIndex"></param>
		public void SetColorIndexAt(int x, int y, byte colorIndex)
		{
			var offset = x + y * Stride;
			Pointer[offset] = colorIndex;
		}

		/// <summary>
		///     Set the supplied color at the specified location.
		///     Throws an ArgumentException if the color is not in the palette
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="color">Color to set</param>
		public override void SetColorAt(int x, int y, Color color)
		{
			var offset = x + y * Stride;
			byte colorIndex;
			if (!_colorCache.TryGetValue(color, out colorIndex))
			{
				var foundColor = false;
				for (colorIndex = 0; colorIndex < _colorEntries.Length; colorIndex++)
				{
					if (color == _colorEntries[colorIndex])
					{
						_colorCache.Add(color, colorIndex);
						foundColor = true;
						break;
					}
				}
				if (!foundColor)
				{
					throw new ArgumentException("No such color!");
				}
			}
			Pointer[offset] = colorIndex;
		}
	}
}