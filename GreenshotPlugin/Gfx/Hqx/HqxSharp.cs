/*
 * Copyright © 2003 Maxim Stepin (maxst@hiend3d.com)
 * 
 * Copyright © 2010 Cameron Zemek (grom@zeminvaders.net)
 * 
 * Copyright © 2011 Tamme Schichler (tamme.schichler@googlemail.com)
 * 
 * This file is part of hqxSharp.
 *
 * hqxSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * hqxSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with hqxSharp. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace hqx
{
	/// <summary>
	/// Provides access to hqxSharp, the extended port of the hqx pixel art magnification filter.
	/// </summary>
	/// <remarks>
	/// The main focus of hqxSharp lies on asset creation and use in tools; it is not necessarily intended as final output for real-time graphics.
	/// <para>This means that additional functionality (like alpha support and variable AYUV thresholds) and easier code are usually preferred over a small performance increase.</para>
	/// <para>Calls to hqx methods are compatible with the corresponding hqxSharp methods and the default thresholds are those used in hqx.</para>
	/// </remarks>
	public static partial class HqxSharp
	{
		const int Ymask = 0x00ff0000;
		const int Umask = 0x0000ff00;
		const int Vmask = 0x000000ff;

		/// <summary>
		/// Compares two ARGB colors according to the provided Y, U, V and A thresholds.
		/// </summary>
		/// <param name="c1">An ARGB color.</param>
		/// <param name="c2">A second ARGB color.</param>
		/// <param name="trY">The Y (luminance) threshold.</param>
		/// <param name="trU">The U (chrominance) threshold.</param>
		/// <param name="trV">The V (chrominance) threshold.</param>
		/// <param name="trA">The A (transparency) threshold.</param>
		/// <returns>Returns true if colors differ more than the thresholds permit, otherwise false.</returns>
		private static bool Diff(uint c1, uint c2, uint trY, uint trU, uint trV, uint trA)
		{
			int YUV1 = (int)RgbYuv.GetYuv(c1);
			int YUV2 = (int)RgbYuv.GetYuv(c2);

			return ((Math.Abs((YUV1 & Ymask) - (YUV2 & Ymask)) > trY) ||
			(Math.Abs((YUV1 & Umask) - (YUV2 & Umask)) > trU) ||
			(Math.Abs((YUV1 & Vmask) - (YUV2 & Vmask)) > trV) ||
			(Math.Abs(((int)((uint)c1 >> 24) - (int)((uint)c2 >> 24))) > trA));
		}
	}
}