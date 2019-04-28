// Dapplo - building blocks for desktop applications
// Copyright (C) 2019 Dapplo
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
// 

using System.Drawing;

namespace Greenshot.Gfx.Structs
{
    /// <summary>
    /// These are extensions to help with pixels
    /// </summary>
    public static class PixelExtensions
    {
        /// <summary>
        /// Make a Brga32 from the specified color
        /// </summary>
        /// <param name="color">Color</param>
        /// <returns>Bgr32</returns>
        public static Bgra32 FromColorWithAlpha(this Color color)
        {
            return new Bgra32
            {
                A = color.A,
                R = color.R,
                G = color.G,
                B = color.B,
            };
        }

        /// <summary>
        /// Make a Brg32 from the specified color
        /// </summary>
        /// <param name="color">Color</param>
        /// <returns>Bgr32</returns>
        public static Bgr32 FromColor(this Color color)
        {
            return new Bgr32
            {
                R = color.R,
                G = color.G,
                B = color.B,
            };
        } 
    }
}