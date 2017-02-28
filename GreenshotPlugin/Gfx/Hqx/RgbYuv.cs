/*
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
	/// Provides cached RGB to YUV lookup without alpha support.
	/// </summary>
	/// <remarks>
	/// This class is public so a user can manually load and unload the lookup table.
	/// Looking up a color calculates the lookup table if not present.
	/// All methods except UnloadLookupTable should be thread-safe, although there will be a performance overhead if GetYUV is called while Initialize has not finished.
	/// </remarks>
	public static class RgbYuv
	{
		const uint RgbMask = 0x00ffffff;

#if EASYTHREADING
        private static volatile int[] lookupTable;
#else
		private static volatile int[] lookupTable;
#endif

		private static int[] LookupTable
		{
			get
			{
				if (lookupTable == null) Initialize();
				return lookupTable;
			}
		}

		/// <summary>
		/// Gets whether the lookup table is ready.
		/// </summary>
		public static bool Initialized
		{
			get
			{
				return lookupTable != null;
			}
		}

		/// <summary>
		/// Returns the 24bit YUV equivalent of the provided 24bit RGB color.
		/// <para>Any alpha component is dropped.</para>
		/// </summary>
		/// <param name="rgb">A 24bit rgb color.</param>
		/// <returns>The corresponding 24bit YUV color.</returns>
		public static int GetYuv(uint rgb)
		{
			return LookupTable[rgb & RgbMask];
		}

		/// <summary>
		/// Calculates the lookup table.
		/// </summary>
		public static unsafe void Initialize()
		{
			var lTable = new int[0x1000000]; // 256 * 256 * 256
			fixed (int* lookupP = lTable)
			{
				byte* lP = (byte*)lookupP;
				for (uint i = 0; i < lTable.Length; i++)
				{
#if EASYTHREADING
                    if(lookupTable != null) return; // table was set by another thread
#endif

					float r = (i & 0xff0000) >> 16;
					float g = (i & 0x00ff00) >> 8;
					float b = (i & 0x0000ff);

					lP++; //Skip alpha byte
					*(lP++) = (byte)(.299 * r + .587 * g + .114 * b);
					*(lP++) = (byte)((int)(-.169 * r - .331 * g + .5 * b) + 128);
					*(lP++) = (byte)((int)(.5 * r - .419 * g - .081 * b) + 128);
				}
			}
			lookupTable = lTable;
		}

		/// <summary>
		/// Releases the reference to the lookup table.
		/// <para>The table has to be calculated again for the next lookup.</para>
		/// </summary>
		public static void UnloadLookupTable()
		{
			lookupTable = null;
		}
	}
}