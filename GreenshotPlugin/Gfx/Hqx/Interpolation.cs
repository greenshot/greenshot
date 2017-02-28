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

namespace hqx
{
	/// <summary>
	/// Contains the color-blending operations used internally by hqx.
	/// </summary>
	internal static class Interpolation
	{
		const uint Mask4 = 0xff000000;
		const uint Mask2 = 0x0000ff00;
		const uint Mask13 = 0x00ff00ff;

		// return statements:
		//     1. line: green
		//     2. line: red and blue
		//     3. line: alpha

		public static uint Mix3To1(uint c1, uint c2)
		{
			//return (c1*3+c2) >> 2;
			if (c1 == c2)
			{
				return c1;

			}
			return ((((c1 & Mask2) * 3 + (c2 & Mask2)) >> 2) & Mask2) |
				((((c1 & Mask13) * 3 + (c2 & Mask13)) >> 2) & Mask13) |
				((((c1 & Mask4) >> 2) * 3 + ((c2 & Mask4) >> 2)) & Mask4);
		}

		public static uint Mix2To1To1(uint c1, uint c2, uint c3)
		{
			//return (c1*2+c2+c3) >> 2;
			return ((((c1 & Mask2) * 2 + (c2 & Mask2) + (c3 & Mask2)) >> 2) & Mask2) |
				  ((((c1 & Mask13) * 2 + (c2 & Mask13) + (c3 & Mask13)) >> 2) & Mask13) |
				((((c1 & Mask4) >> 2) * 2 + ((c2 & Mask4) >> 2) + ((c3 & Mask4) >> 2)) & Mask4);
		}

		public static uint Mix7To1(uint c1, uint c2)
		{
			//return (c1*7+c2)/8;
			if (c1 == c2)
			{
				return c1;

			}
			return ((((c1 & Mask2) * 7 + (c2 & Mask2)) >> 3) & Mask2) |
				((((c1 & Mask13) * 7 + (c2 & Mask13)) >> 3) & Mask13) |
				((((c1 & Mask4) >> 3) * 7 + ((c2 & Mask4) >> 3)) & Mask4);
		}

		public static uint Mix2To7To7(uint c1, uint c2, uint c3)
		{
			//return (c1*2+(c2+c3)*7)/16;
			return ((((c1 & Mask2) * 2 + (c2 & Mask2) * 7 + (c3 & Mask2) * 7) >> 4) & Mask2) |
				  ((((c1 & Mask13) * 2 + (c2 & Mask13) * 7 + (c3 & Mask13) * 7) >> 4) & Mask13) |
				((((c1 & Mask4) >> 4) * 2 + ((c2 & Mask4) >> 4) * 7 + ((c3 & Mask4) >> 4) * 7) & Mask4);
		}

		public static uint MixEven(uint c1, uint c2)
		{
			//return (c1+c2) >> 1;
			if (c1 == c2)
			{
				return c1;

			}
			return ((((c1 & Mask2) + (c2 & Mask2)) >> 1) & Mask2) |
				((((c1 & Mask13) + (c2 & Mask13)) >> 1) & Mask13) |
				((((c1 & Mask4) >> 1) + ((c2 & Mask4) >> 1)) & Mask4);
		}

		public static uint Mix4To2To1(uint c1, uint c2, uint c3)
		{
			//return (c1*5+c2*2+c3)/8;
			return ((((c1 & Mask2) * 5 + (c2 & Mask2) * 2 + (c3 & Mask2)) >> 3) & Mask2) |
				  ((((c1 & Mask13) * 5 + (c2 & Mask13) * 2 + (c3 & Mask13)) >> 3) & Mask13) |
				((((c1 & Mask4) >> 3) * 5 + ((c2 & Mask4) >> 3) * 2 + ((c3 & Mask4) >> 3)) & Mask4);
		}

		public static uint Mix6To1To1(uint c1, uint c2, uint c3)
		{
			//return (c1*6+c2+c3)/8;
			return ((((c1 & Mask2) * 6 + (c2 & Mask2) + (c3 & Mask2)) >> 3) & Mask2) |
				  ((((c1 & Mask13) * 6 + (c2 & Mask13) + (c3 & Mask13)) >> 3) & Mask13) |
				((((c1 & Mask4) >> 3) * 6 + ((c2 & Mask4) >> 3) + ((c3 & Mask4) >> 3)) & Mask4);
		}

		public static uint Mix5To3(uint c1, uint c2)
		{
			//return (c1*5+c2*3)/8;
			if (c1 == c2)
			{
				return c1;

			}
			return ((((c1 & Mask2) * 5 + (c2 & Mask2) * 3) >> 3) & Mask2) |
				  ((((c1 & Mask13) * 5 + (c2 & Mask13) * 3) >> 3) & Mask13) |
				((((c1 & Mask4) >> 3) * 5 + ((c2 & Mask4) >> 3) * 3) & Mask4);
		}

		public static uint Mix2To3To3(uint c1, uint c2, uint c3)
		{
			//return (c1*2+(c2+c3)*3)/8;
			return ((((c1 & Mask2) * 2 + (c2 & Mask2) * 3 + (c3 & Mask2) * 3) >> 3) & Mask2) |
				  ((((c1 & Mask13) * 2 + (c2 & Mask13) * 3 + (c3 & Mask13) * 3) >> 3) & Mask13) |
				((((c1 & Mask4) >> 3) * 2 + ((c2 & Mask4) >> 3) * 3 + ((c3 & Mask4) >> 3) * 3) & Mask4);
		}

		public static uint Mix14To1To1(uint c1, uint c2, uint c3)
		{
			//return (c1*14+c2+c3)/16;
			return ((((c1 & Mask2) * 14 + (c2 & Mask2) + (c3 & Mask2)) >> 4) & Mask2) |
				  ((((c1 & Mask13) * 14 + (c2 & Mask13) + (c3 & Mask13)) >> 4) & Mask13) |
				((((c1 & Mask4) >> 4) * 14 + ((c2 & Mask4) >> 4) + ((c3 & Mask4) >> 4)) & Mask4);
		}
	}
}