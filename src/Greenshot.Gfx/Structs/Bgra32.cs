// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Gfx.Structs
{
    /// <summary>
    /// A struct with the BGR values for a 32bit pixel
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Bgra32 : IEquatable<Bgra32>
    {
        /// <summary>
        /// Blue component of the pixel
        /// </summary>
        public byte B;

        /// <summary>
        /// Green component of the pixel
        /// </summary>
        public byte G;

        /// <summary>
        /// Red component of the pixel
        /// </summary>
        public byte R;

        /// <summary>
        /// Alpha component of the pixel
        /// </summary>
        public byte A;

        /// <summary>
        /// Equal
        /// </summary>
        public static bool operator ==(Bgra32 left, Bgra32 right) => Equals(left, right);

        /// <summary>
        /// Not equal
        /// </summary>
        public static bool operator !=(Bgra32 left, Bgra32 right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object obj) => (obj is Bgra32 bgra32) && Equals(bgra32);

        /// <summary>
        /// Equals implementation
        /// </summary>
        /// <param name="other">Bgra32</param>
        /// <returns>bool</returns>
        public bool Equals(Bgra32 other) => (B, G, R, A) == (other.B, other.G, other.R, other.A);

        /// <inheritdoc />
        public override int GetHashCode() => (B, G, R, A).GetHashCode();
    }
}
