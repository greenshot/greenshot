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

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Greenshot.Gfx
{
    /// <summary>
    /// This is an implementation of the Murmur3 hash algorithm
    /// See <a href="https://en.wikipedia.org/wiki/MurmurHash">MurmurHash</a>
    /// </summary>
    public sealed class Murmur3 : HashAlgorithm
    {
        private const uint C1 = 0xcc9e2d51;
        private const uint C2 = 0x1b873593;
        private const int R1 = 15;
        private const int R2 = 13;
        private const uint M = 5;
        private const uint N = 0xe6546b64;

        private readonly uint _seed;
        private uint _hash;
        private int _length;

        public override int HashSize => 32;

        public Murmur3(uint seed)
        {
            _seed = seed;
            Initialize();
        }

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _length = cbSize;
            uint k = 0;

            var curLength = cbSize;
            var currentIndex = ibStart;
            while (curLength >= 4)
            {
                k = (uint)(array[currentIndex++] | array[currentIndex++] << 8 | array[currentIndex++] << 16 | array[currentIndex++] << 24);
                k *= C1;
                k = RotateLeft(k, R1);
                k *= C2;
                _hash ^= k;
                _hash = RotateLeft(_hash, R2);
                _hash = _hash * M + N;
                curLength -= 4;
            }
            // Process the remaining bytes, if any
            if (curLength <= 0)
            {
                return;
            }
            switch (curLength)
            {
                case 3:
                    k = (uint)(array[currentIndex++] | array[currentIndex++] << 8 | array[currentIndex] << 16);
                    break;
                case 2:
                    k = (uint)(array[currentIndex++] | array[currentIndex] << 8);
                    break;
                case 1:
                    k = array[currentIndex];
                    break;
            }
            k *= C1;
            k = RotateLeft(k, R1);
            k *= C2;
            _hash ^= k;
        }

        /// <summary>
        /// Returns the hash
        /// </summary>
        public uint CalculatedHash
        {
            get
            {
                var hash = _hash ^ (uint)_length;
                hash ^= hash >> 16;
                hash *= 0x85ebca6b;
                hash ^= hash >> 13;
                hash *= 0xc2b2ae35;
                hash ^= hash >> 16;
                return hash;
            }
        }

        /// <summary>
        /// Generate a hash for the specified bytes
        /// </summary>
        /// <param name="bytes">byte array</param>
        /// <param name="offset">optional int with offset into the byte array</param>
        /// <param name="size">optional int with the size</param>
        /// <returns>uint with the hash</returns>
        public uint GenerateHash(byte[] bytes, int? offset = null, int? size = null)
        {
            Initialize();
            HashCore(bytes, offset ?? 0, size ?? bytes.Length);
            return CalculatedHash;
        }

        /// <inheritdoc />
        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(CalculatedHash);
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            // re-initialize the Hash with the seed, to allow reuse
            _hash = _seed;
            _length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
