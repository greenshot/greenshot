/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Cryptography;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// Cryptographically secure random numbers (the AES-CBC encrypted zeros), the same key and IV give the same numbers
    /// </summary>
    public sealed class CryptoRandomBuffer : IDisposable
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly byte[] _zeros;
        private readonly byte[] _buffer;
        private int _index;

        /// <summary>
        /// A new random key and IV, for numbers which nobody else can repeat
        /// </summary>
        public static (byte[] Key, byte[] Iv) NewKey()
        {
            // Aes.Create generates a random key and IV
            using var aes = Aes.Create();
            return (aes.Key, aes.IV);
        }

        public CryptoRandomBuffer(byte[] key, byte[] iv, int size)
        {
            // Ensure size is a multiple of the AES block size
            int alignedSize = ((size + 15) / 16) * 16;
            _buffer = new byte[alignedSize];
            _zeros = new byte[alignedSize];
            _aes = Aes.Create();
            _aes.Padding = PaddingMode.None;
            _aes.Key = key;
            _aes.IV = iv;
            _encryptor = _aes.CreateEncryptor();
            Refill();
        }

        private void Refill()
        {
            // The encryptor keeps chaining, so every refill gives new numbers
            _encryptor.TransformBlock(_zeros, 0, _zeros.Length, _buffer, 0);
            _index = 0;
        }

        private uint GetNextUInt32()
        {
            if (_index + 4 > _buffer.Length)
            {
                Refill();
            }
            uint val = BitConverter.ToUInt32(_buffer, _index);
            _index += 4;
            return val;
        }

        public int GetNextInt(int min, int max)
        {
            uint range = (uint)(max - min + 1);
            if (range <= 1) return min;

            uint limit = uint.MaxValue - (uint.MaxValue % range);
            uint val;
            do
            {
                val = GetNextUInt32();
            } while (val >= limit);

            return min + (int)(val % range);
        }

        /// <summary>
        /// A number from min to max (at most 256 numbers) from one random byte: for many small numbers, e.g. noise
        /// </summary>
        public int GetNextSmallInt(int min, int max)
        {
            int range = max - min + 1;
            if (range <= 1) return min;
            if (range > 256) throw new ArgumentOutOfRangeException(nameof(max), "At most 256 numbers");

            int limit = 256 - 256 % range;
            int val;
            do
            {
                if (_index >= _buffer.Length)
                {
                    Refill();
                }
                val = _buffer[_index++];
            } while (val >= limit);

            return min + val % range;
        }

        public void Dispose()
        {
            _encryptor.Dispose();
            _aes.Dispose();
        }
    }
}
