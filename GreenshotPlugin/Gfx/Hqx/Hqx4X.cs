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
	public static partial class HqxSharp
	{
		/// <summary>
		/// This is the extended C# port of the hq4x algorithm.
		/// <para>The image is scaled to four times its size.</para>
		/// </summary>
		/// <param name="bitmap">The source image.</param>
		/// <param name="trY">The Y (luminance) threshold.</param>
		/// <param name="trU">The U (chrominance) threshold.</param>
		/// <param name="trV">The V (chrominance) threshold.</param>
		/// <param name="trA">The A (transparency) threshold.</param>
		/// <param name="wrapX">Used for images that can be seamlessly repeated horizontally.</param>
		/// <param name="wrapY">Used for images that can be seamlessly repeated vertically.</param>
		/// <returns>A new Bitmap instance that contains the source imagage scaled to four times its size.</returns>
		public static unsafe Bitmap Scale4(Bitmap bitmap, uint trY = 48, uint trU = 7, uint trV = 6, uint trA = 0, bool wrapX = false, bool wrapY = false)
		{

			int Xres = bitmap.Width;
			int Yres = bitmap.Height;

			var dest = new Bitmap(bitmap.Width * 4, bitmap.Height * 4);

			var bmpData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			var destData = dest.LockBits(new Rectangle(Point.Empty, dest.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			{

				uint* sp = (uint*)bmpData.Scan0.ToPointer();
				uint* dp = (uint*)destData.Scan0.ToPointer();

				Scale4(sp, dp, Xres, Yres, trY, trU, trV, trA, wrapX, wrapY);
			}
			bitmap.UnlockBits(bmpData);
			dest.UnlockBits(destData);

			return dest;
		}

		/// <summary>
		/// This is the extended C# port of the hq4x algorithm.
		/// <para>The destination image must be exactly four times as large in both dimensions as the source image.</para>
		/// </summary>
		/// <param name="sp">A pointer to the source image.</param>
		/// <param name="dp">A pointer to the destination image.</param>
		/// <param name="Xres">The horizontal resolution of the source image.</param>
		/// <param name="Yres">The vertical resolution of the source image.</param>
		/// <param name="trY">The Y (luminance) threshold.</param>
		/// <param name="trU">The U (chrominance) threshold.</param>
		/// <param name="trV">The V (chrominance) threshold.</param>
		/// <param name="trA">The A (transparency) threshold.</param>
		/// <param name="wrapX">Used for images that can be seamlessly repeated horizontally.</param>
		/// <param name="wrapY">Used for images that can be seamlessly repeated vertically.</param>
		public static unsafe void Scale4(uint* sp, uint* dp, int Xres, int Yres, uint trY = 48, uint trU = 7, uint trV = 6, uint trA = 0, bool wrapX = false, bool wrapY = false)
		{
			unchecked
			{
				//Don't shift trA, as it uses shift right instead of a mask for comparisons.
				trY <<= 2 * 8;
				trU <<= 1 * 8;
				int dpL = Xres * 4;

				int prevline, nextline;
				var w = new uint[9];

				for (int j = 0; j < Yres; j++)
				{
					if (j > 0)
					{
						prevline = -Xres;
					}
					else
					{
						if (wrapY)
						{
							prevline = Xres * (Yres - 1);
						}
						else
						{
							prevline = 0;
						}
					}
					if (j < Yres - 1)
					{
						nextline = Xres;
					}
					else
					{
						if (wrapY)
						{
							nextline = -(Xres * (Yres - 1));
						}
						else
						{
							nextline = 0;
						}
					}

					for (int i = 0; i < Xres; i++)
					{
						w[1] = *(sp + prevline);
						w[4] = *sp;
						w[7] = *(sp + nextline);

						if (i > 0)
						{
							w[0] = *(sp + prevline - 1);
							w[3] = *(sp - 1);
							w[6] = *(sp + nextline - 1);
						}
						else
						{
							if (wrapX)
							{
								w[0] = *(sp + prevline + Xres - 1);
								w[3] = *(sp + Xres - 1);
								w[6] = *(sp + nextline + Xres - 1);
							}
							else
							{
								w[0] = w[1];
								w[3] = w[4];
								w[6] = w[7];
							}
						}

						if (i < Xres - 1)
						{
							w[2] = *(sp + prevline + 1);
							w[5] = *(sp + 1);
							w[8] = *(sp + nextline + 1);
						}
						else
						{
							if (wrapX)
							{
								w[2] = *(sp + prevline - Xres + 1);
								w[5] = *(sp - Xres + 1);
								w[8] = *(sp + nextline - Xres + 1);
							}
							else
							{
								w[2] = w[1];
								w[5] = w[4];
								w[8] = w[7];
							}
						}

						int pattern = 0;
						int flag = 1;

						for (int k = 0; k < 9; k++)
						{
							if (k == 4) continue;

							if (w[k] != w[4])
							{
								if (Diff(w[4], w[k], trY, trU, trV, trA))
									pattern |= flag;
							}
							flag <<= 1;
						}

						switch (pattern)
						{
							case 0:
							case 1:
							case 4:
							case 32:
							case 128:
							case 5:
							case 132:
							case 160:
							case 33:
							case 129:
							case 36:
							case 133:
							case 164:
							case 161:
							case 37:
							case 165:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 2:
							case 34:
							case 130:
							case 162:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 16:
							case 17:
							case 48:
							case 49:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 64:
							case 65:
							case 68:
							case 69:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 8:
							case 12:
							case 136:
							case 140:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 3:
							case 35:
							case 131:
							case 163:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 6:
							case 38:
							case 134:
							case 166:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 20:
							case 21:
							case 52:
							case 53:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 144:
							case 145:
							case 176:
							case 177:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 192:
							case 193:
							case 196:
							case 197:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 96:
							case 97:
							case 100:
							case 101:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 40:
							case 44:
							case 168:
							case 172:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 9:
							case 13:
							case 137:
							case 141:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 18:
							case 50:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 80:
							case 81:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 72:
							case 76:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 10:
							case 138:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + 1) = w[4];
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 66:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 24:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 7:
							case 39:
							case 135:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 148:
							case 149:
							case 180:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 224:
							case 228:
							case 225:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 41:
							case 169:
							case 45:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 22:
							case 54:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 208:
							case 209:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 104:
							case 108:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 11:
							case 139:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 19:
							case 51:
								{
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[3]);
										*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 1) = Interpolation.Mix3To1(w[1], w[4]);
										*(dp + 2) = Interpolation.Mix5To3(w[1], w[5]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix2To1To1(w[5], w[4], w[1]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 146:
							case 178:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix2To1To1(w[1], w[4], w[5]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix5To3(w[5], w[1]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 84:
							case 85:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[5], w[4]);
										*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[5], w[7]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix2To1To1(w[7], w[4], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 112:
							case 113:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[5], w[4], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[7], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 200:
							case 204:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix2To1To1(w[3], w[4], w[7]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 73:
							case 77:
								{
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL) = Interpolation.Mix3To1(w[3], w[4]);
										*(dp + dpL + dpL) = Interpolation.Mix5To3(w[3], w[7]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix2To1To1(w[7], w[4], w[3]);
									}
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 42:
							case 170:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
										*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.Mix2To1To1(w[1], w[4], w[3]);
										*(dp + dpL) = Interpolation.Mix5To3(w[3], w[1]);
										*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 14:
							case 142:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.Mix5To3(w[1], w[3]);
										*(dp + 2) = Interpolation.Mix3To1(w[1], w[4]);
										*(dp + 3) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix2To1To1(w[3], w[4], w[1]);
										*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									}
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 67:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 70:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 28:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 152:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 194:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 98:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 56:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 25:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 26:
							case 31:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 82:
							case 214:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 88:
							case 248:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 74:
							case 107:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 27:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 86:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 216:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 106:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 30:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 210:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 120:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 75:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 29:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 198:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 184:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 99:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 57:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 71:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 156:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 226:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 60:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 195:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 102:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 153:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 58:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 83:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 92:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 202:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 78:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 154:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 114:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									break;
								}
							case 89:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 90:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 55:
							case 23:
								{
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[3]);
										*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 1) = Interpolation.Mix3To1(w[1], w[4]);
										*(dp + 2) = Interpolation.Mix5To3(w[1], w[5]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix2To1To1(w[5], w[4], w[1]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 182:
							case 150:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix2To1To1(w[1], w[4], w[5]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix5To3(w[5], w[1]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 213:
							case 212:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[5], w[4]);
										*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[5], w[7]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix2To1To1(w[7], w[4], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 241:
							case 240:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[5], w[4], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[7], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 236:
							case 232:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix2To1To1(w[3], w[4], w[7]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 109:
							case 105:
								{
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL) = Interpolation.Mix3To1(w[3], w[4]);
										*(dp + dpL + dpL) = Interpolation.Mix5To3(w[3], w[7]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix2To1To1(w[7], w[4], w[3]);
									}
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 171:
							case 43:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
										*(dp + dpL + 1) = w[4];
										*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.Mix2To1To1(w[1], w[4], w[3]);
										*(dp + dpL) = Interpolation.Mix5To3(w[3], w[1]);
										*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 143:
							case 15:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
										*(dp + dpL) = w[4];
										*(dp + dpL + 1) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.Mix5To3(w[1], w[3]);
										*(dp + 2) = Interpolation.Mix3To1(w[1], w[4]);
										*(dp + 3) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix2To1To1(w[3], w[4], w[1]);
										*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									}
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 124:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 203:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 62:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 211:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 118:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 217:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 110:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 155:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 188:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 185:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 61:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 157:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 103:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 227:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 230:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 199:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 220:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 158:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 234:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 242:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									break;
								}
							case 59:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 121:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 87:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 79:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 122:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 94:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL + 2) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 218:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 91:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL + 1) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 229:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 167:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 173:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 181:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 186:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 115:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									break;
								}
							case 93:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 206:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 205:
							case 201:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 174:
							case 46:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[0]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
										*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
										*(dp + 1) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL + 1) = w[4];
									}
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 179:
							case 147:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
										*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 117:
							case 116:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									}
									else
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									break;
								}
							case 189:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 231:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 126:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 219:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 125:
								{
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix3To1(w[4], w[3]);
										*(dp + dpL) = Interpolation.Mix3To1(w[3], w[4]);
										*(dp + dpL + dpL) = Interpolation.Mix5To3(w[3], w[7]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix2To1To1(w[7], w[4], w[3]);
									}
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 221:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix3To1(w[4], w[5]);
										*(dp + dpL + 3) = Interpolation.Mix3To1(w[5], w[4]);
										*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[5], w[7]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix2To1To1(w[7], w[4], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 207:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
										*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
										*(dp + dpL) = w[4];
										*(dp + dpL + 1) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.Mix5To3(w[1], w[3]);
										*(dp + 2) = Interpolation.Mix3To1(w[1], w[4]);
										*(dp + 3) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + dpL) = Interpolation.Mix2To1To1(w[3], w[4], w[1]);
										*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									}
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 238:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.Mix2To1To1(w[3], w[4], w[7]);
										*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[7]);
									}
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 190:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = w[4];
										*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									}
									else
									{
										*(dp + 2) = Interpolation.Mix2To1To1(w[1], w[4], w[5]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix5To3(w[5], w[1]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 187:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
										*(dp + dpL + 1) = w[4];
										*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.Mix2To1To1(w[1], w[4], w[3]);
										*(dp + dpL) = Interpolation.Mix5To3(w[3], w[1]);
										*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
										*(dp + dpL + dpL) = Interpolation.Mix3To1(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix3To1(w[4], w[3]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 243:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
										*(dp + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[5], w[4], w[7]);
										*(dp + dpL + dpL + dpL) = Interpolation.Mix3To1(w[4], w[7]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[7], w[5]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 119:
								{
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp) = Interpolation.Mix5To3(w[4], w[3]);
										*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 2) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix3To1(w[4], w[1]);
										*(dp + 1) = Interpolation.Mix3To1(w[1], w[4]);
										*(dp + 2) = Interpolation.Mix5To3(w[1], w[5]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
										*(dp + dpL + 3) = Interpolation.Mix2To1To1(w[5], w[4], w[1]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 237:
							case 233:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[5]);
									*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[1]);
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 175:
							case 47:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix6To1To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									break;
								}
							case 183:
							case 151:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[3]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 245:
							case 244:
								{
									*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[3]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix6To1To1(w[4], w[3], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 250:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									break;
								}
							case 123:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 95:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 222:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 252:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix4To2To1(w[4], w[1], w[0]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 249:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix4To2To1(w[4], w[1], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									break;
								}
							case 235:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[2]);
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 111:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix4To2To1(w[4], w[5], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 63:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix4To2To1(w[4], w[7], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 159:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix4To2To1(w[4], w[7], w[6]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 215:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 246:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix4To2To1(w[4], w[3], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 254:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[0]);
									*(dp + 1) = Interpolation.Mix3To1(w[4], w[0]);
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = Interpolation.Mix3To1(w[4], w[0]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[0]);
									*(dp + dpL + 2) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 253:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 1) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 2) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[1]);
									*(dp + dpL) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + 3) = Interpolation.Mix7To1(w[4], w[1]);
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 251:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[2]);
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[2]);
									*(dp + dpL + 3) = Interpolation.Mix3To1(w[4], w[2]);
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									break;
								}
							case 239:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									*(dp + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[5]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[5]);
									break;
								}
							case 127:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 2) = w[4];
										*(dp + 3) = w[4];
										*(dp + dpL + 3) = w[4];
									}
									else
									{
										*(dp + 2) = Interpolation.MixEven(w[1], w[4]);
										*(dp + 3) = Interpolation.MixEven(w[1], w[5]);
										*(dp + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
									}
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL) = w[4];
										*(dp + dpL + dpL + dpL + 1) = w[4];
									}
									else
									{
										*(dp + dpL + dpL) = Interpolation.MixEven(w[3], w[4]);
										*(dp + dpL + dpL + dpL) = Interpolation.MixEven(w[7], w[3]);
										*(dp + dpL + dpL + dpL + 1) = Interpolation.MixEven(w[7], w[4]);
									}
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[8]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix3To1(w[4], w[8]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[8]);
									break;
								}
							case 191:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 2) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + 3) = Interpolation.Mix7To1(w[4], w[7]);
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 2) = Interpolation.Mix5To3(w[4], w[7]);
									*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix5To3(w[4], w[7]);
									break;
								}
							case 223:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
										*(dp + 1) = w[4];
										*(dp + dpL) = w[4];
									}
									else
									{
										*(dp) = Interpolation.MixEven(w[1], w[3]);
										*(dp + 1) = Interpolation.MixEven(w[1], w[4]);
										*(dp + dpL) = Interpolation.MixEven(w[3], w[4]);
									}
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix3To1(w[4], w[6]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[6]);
									*(dp + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + 3) = w[4];
										*(dp + dpL + dpL + dpL + 2) = w[4];
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + 3) = Interpolation.MixEven(w[5], w[4]);
										*(dp + dpL + dpL + dpL + 2) = Interpolation.MixEven(w[7], w[4]);
										*(dp + dpL + dpL + dpL + 3) = Interpolation.MixEven(w[7], w[5]);
									}
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[6]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix3To1(w[4], w[6]);
									break;
								}
							case 247:
								{
									*(dp) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									*(dp + dpL + dpL + dpL) = Interpolation.Mix5To3(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 1) = Interpolation.Mix7To1(w[4], w[3]);
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
							case 255:
								{
									if (Diff(w[3], w[1], trY, trU, trV, trA))
									{
										*dp = w[4];
									}
									else
									{
										*(dp) = Interpolation.Mix2To1To1(w[4], w[1], w[3]);
									}
									*(dp + 1) = w[4];
									*(dp + 2) = w[4];
									if (Diff(w[1], w[5], trY, trU, trV, trA))
									{
										*(dp + 3) = w[4];
									}
									else
									{
										*(dp + 3) = Interpolation.Mix2To1To1(w[4], w[1], w[5]);
									}
									*(dp + dpL) = w[4];
									*(dp + dpL + 1) = w[4];
									*(dp + dpL + 2) = w[4];
									*(dp + dpL + 3) = w[4];
									*(dp + dpL + dpL) = w[4];
									*(dp + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + 2) = w[4];
									*(dp + dpL + dpL + 3) = w[4];
									if (Diff(w[7], w[3], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL) = Interpolation.Mix2To1To1(w[4], w[7], w[3]);
									}
									*(dp + dpL + dpL + dpL + 1) = w[4];
									*(dp + dpL + dpL + dpL + 2) = w[4];
									if (Diff(w[5], w[7], trY, trU, trV, trA))
									{
										*(dp + dpL + dpL + dpL + 3) = w[4];
									}
									else
									{
										*(dp + dpL + dpL + dpL + 3) = Interpolation.Mix2To1To1(w[4], w[7], w[5]);
									}
									break;
								}
						}
						sp++;
						dp += 4;
					}
					dp += (dpL * 3);
				}

			}
		}

	}
}