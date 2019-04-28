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
        private readonly uint _initialLength;
        private uint _hash;
        private uint _length;

        /// <inheritdoc />
        public override int HashSize => 32;

        /// <summary>
        /// Constructor for the Murmur3 algorythm
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="length"></param>
        public Murmur3(uint seed, uint length = 0)
        {
            _seed = seed;
            _initialLength = length;
            Initialize();
        }

        /// <summary>
        /// Add the bytes to the hash
        /// </summary>
        /// <param name="one">first byte</param>
        /// <param name="two">second byte</param>
        /// <param name="three">third byte</param>
        /// <param name="four">fourth byte</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBytes(byte one, byte two, byte three, byte four)
        {
            unchecked
            {
                var k = (uint)(one | two << 8 | three << 16 | four << 24);
                k *= C1;
                k = RotateLeft(k, R1);
                k *= C2;
                _hash ^= k;
                _hash = RotateLeft(_hash, R2);
                _hash = _hash * M + N;
            }
        }

        /// <summary>
        /// Add the last bytes 
        /// </summary>
        /// <param name="one">first byte</param>
        /// <param name="two">second byte</param>
        /// <param name="three">third byte</param>
        public void AddTrailingBytes(byte one, byte two = 0, byte three = 0)
        {
            unchecked
            {
                var k = (uint) (one | two << 8 | three << 16);
                k *= C1;
                k = RotateLeft(k, R1);
                k *= C2;
                _hash ^= k;
            }
        }

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _length = (uint)cbSize;
 
            var curLength = cbSize;
            var currentIndex = ibStart;
            while (curLength >= 4)
            {
                AddBytes(array[currentIndex++], array[currentIndex++], array[currentIndex++], array[currentIndex++]);
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
                    AddTrailingBytes(array[currentIndex++], array[currentIndex++], array[currentIndex]);
                    break;
                case 2:
                    AddTrailingBytes(array[currentIndex++], array[currentIndex]);
                    break;
                case 1:
                    AddTrailingBytes(array[currentIndex]);
                    break;
            }
        }

        /// <summary>
        /// Returns the hash
        /// </summary>
        public uint CalculatedHash
        {
            get
            {
                var hash = _hash ^ _length;
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
            _length = _initialLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
