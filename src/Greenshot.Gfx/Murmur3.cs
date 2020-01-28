// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Greenshot.Gfx
{
    /// <summary>
    /// This is an implementation of the Murmur3 hash algorithm
    /// See <a href="https://en.wikipedia.org/wiki/MurmurHash">MurmurHash</a>
    /// </summary>
    public sealed class Murmur3
    {
        private const uint C1 = 0xcc9e2d51;
        private const uint C2 = 0x1b873593;
        private const int R1 = 15;
        private const int R2 = 13;
        private const uint M = 5;
        private const uint N = 0xe6546b64;

        private readonly uint _seed;

        /// <summary>
        /// Constructor for the Murmur3 algorithm
        /// </summary>
        /// <param name="seed"></param>
        public Murmur3(uint seed)
        {
            _seed = seed;
        }

        /// <summary>
        /// Wrapper for byte*
        /// </summary>
        /// <returns>uint</returns>
        public unsafe uint CalculateHash(byte* pointer, int offset, int length)
        {
            var values = new ReadOnlySpan<byte>(pointer+offset, length);
            return CalculateHash(values);
        }

        /// <summary>
        /// Wrapper for string etc
        /// </summary>
        /// <typeparam name="TSpan">Type for the span</typeparam>
        /// <param name="valuesToHash">ReadOnlySpan of TSpan</param>
        /// <returns>uint</returns>
        public uint CalculateHash<TSpan>(ReadOnlySpan<TSpan> valuesToHash) where TSpan : struct
        {
            return CalculateHash(MemoryMarshal.Cast<TSpan, byte>(valuesToHash));
        }

        /// <summary>
        /// Calculate a Murmur3 hash for the specified values
        /// </summary>
        /// <param name="valuesToHash">ReadOnlySpan of byte</param>
        /// <returns>uint with the hash</returns>
        public uint CalculateHash(ReadOnlySpan<byte> valuesToHash)
        {
            var uintSpan = MemoryMarshal.Cast<byte, uint>(valuesToHash);
            uint hash = _seed;
            var uintSpanLength = uintSpan.Length;
            // Hash with uints
            for (int index = 0; index < uintSpanLength; index++)
            {
                var k = uintSpan[index];
                unchecked
                {
                    k *= C1;
                    k = RotateLeft(k, R1);
                    k *= C2;
                    hash ^= k;
                    hash = RotateLeft(hash, R2);
                    hash = hash * M + N;
                }
            }

            int valuesToProcess = valuesToHash.Length - (uintSpanLength * 4);
            if (valuesToProcess > 0)
            {
                uint k = 0;
                var startingOffset = uintSpanLength * 4;
                // Hash the rest
                for (int index = 0; index < valuesToProcess; index++)
                {
                    k |= (uint)valuesToHash[startingOffset + index] << (8 * index);
                }

                unchecked
                {
                    k *= C1;
                    k = RotateLeft(k, R1);
                    k *= C2;
                    hash ^= k;
                }
            }

            // Calculate the final hash
            hash ^= (uint)valuesToHash.Length;
            unchecked
            {
                hash ^= hash >> 16;
                hash *= 0x85ebca6b;
                hash ^= hash >> 13;
                hash *= 0xc2b2ae35;
                hash ^= hash >> 16;
            }
            return hash;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
