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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Greenshot.Recipes.Catalog
{
    /// <summary>
    /// The signature of the catalog index: ECDSA P-256 with SHA-256 over the bytes of index.json, as openssl writes it
    /// (<c>openssl dgst -sha256 -sign key.pem</c>, DER). Greenshot trusts the public keys compiled into it: the current key and
    /// the spare one for a rotation. A leaked key is replaced with a Greenshot release which trusts a new key.
    /// </summary>
    public static class RecipeCatalogSignature
    {
        /// <summary>
        /// The public keys of the catalog, base64 of the SubjectPublicKeyInfo (the text of the .pub.pem file between its first and last line)
        /// </summary>
        public static readonly IReadOnlyList<string> CatalogKeys = new[]
        {
            // TODO before merging: the two keys of the catalog (RECIPE_INDEX_SIGNING_KEY and the spare one)
        };

        /// <summary>
        /// The start of every P-256 SubjectPublicKeyInfo: the algorithm (id-ecPublicKey, prime256v1) and the bit string header of the point
        /// </summary>
        private static readonly byte[] P256SpkiPrefix =
        {
            0x30, 0x59, 0x30, 0x13, 0x06, 0x07, 0x2a, 0x86, 0x48, 0xce, 0x3d, 0x02, 0x01, 0x06, 0x08, 0x2a,
            0x86, 0x48, 0xce, 0x3d, 0x03, 0x01, 0x07, 0x03, 0x42, 0x00, 0x04
        };

        private const int CoordinateLength = 32;

        /// <summary>
        /// True when one of the keys signed the data
        /// </summary>
        /// <param name="data">The signed bytes</param>
        /// <param name="derSignature">The signature as openssl writes it (DER sequence of r and s)</param>
        /// <param name="publicKeys">Base64 SubjectPublicKeyInfo of P-256 keys</param>
        public static bool Verify(byte[] data, byte[] derSignature, IEnumerable<string> publicKeys)
        {
            if (data == null || derSignature == null || publicKeys == null)
            {
                return false;
            }
            byte[] signature = DerToIeeeP1363(derSignature);
            if (signature == null)
            {
                return false;
            }
            foreach (string key in publicKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                var parameters = ReadPublicKey(key);
                if (parameters == null)
                {
                    continue;
                }
                using var ecdsa = ECDsa.Create(parameters.Value);
                if (ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The point of a P-256 public key, null for anything else
        /// </summary>
        internal static ECParameters? ReadPublicKey(string base64Spki)
        {
            byte[] spki;
            try
            {
                spki = Convert.FromBase64String(base64Spki.Trim());
            }
            catch (FormatException)
            {
                return null;
            }
            if (spki.Length != P256SpkiPrefix.Length + 2 * CoordinateLength || !spki.Take(P256SpkiPrefix.Length).SequenceEqual(P256SpkiPrefix))
            {
                return null;
            }
            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = spki.Skip(P256SpkiPrefix.Length).Take(CoordinateLength).ToArray(),
                    Y = spki.Skip(P256SpkiPrefix.Length + CoordinateLength).Take(CoordinateLength).ToArray()
                }
            };
        }

        /// <summary>
        /// The DER signature (SEQUENCE of the INTEGERs r and s) as r and s of 32 bytes each, which .NET verifies; null when it isn't one
        /// </summary>
        internal static byte[] DerToIeeeP1363(byte[] der)
        {
            int position = 0;
            if (!ReadHeader(der, ref position, 0x30, out int sequenceLength) || position + sequenceLength != der.Length)
            {
                return null;
            }
            var result = new byte[2 * CoordinateLength];
            for (int part = 0; part < 2; part++)
            {
                if (!ReadHeader(der, ref position, 0x02, out int length) || length == 0 || position + length > der.Length)
                {
                    return null;
                }
                // An INTEGER has a leading 0 when its first bit is set; a number longer than 32 bytes isn't a P-256 value
                int start = position;
                int end = position + length;
                while (start < end - 1 && der[start] == 0)
                {
                    start++;
                }
                int valueLength = end - start;
                if (valueLength > CoordinateLength)
                {
                    return null;
                }
                Array.Copy(der, start, result, part * CoordinateLength + CoordinateLength - valueLength, valueLength);
                position = end;
            }
            return position == der.Length ? result : null;
        }

        private static bool ReadHeader(byte[] der, ref int position, byte tag, out int length)
        {
            length = 0;
            if (der == null || position + 2 > der.Length || der[position] != tag)
            {
                return false;
            }
            int first = der[position + 1];
            position += 2;
            if (first < 0x80)
            {
                length = first;
                return true;
            }
            // A signature is short: only one length byte follows (0x81)
            if (first != 0x81 || position >= der.Length)
            {
                return false;
            }
            length = der[position];
            position++;
            return true;
        }
    }
}
