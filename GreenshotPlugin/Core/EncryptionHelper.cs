using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace GreenshotPlugin.Core {
	public static class EncryptionHelper {
		private const string RGBIV = "dlgjowejgogkklwj";
		private const string KEY = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

		/// <summary>
		/// A simply rijndael aes encryption, can be used to store passwords
		/// </summary>
		/// <param name="ClearText">the string to call upon</param>
		/// <returns>an encryped string in base64 form</returns>
		public static string Encrypt(this string ClearText) {
			byte[] clearTextBytes = Encoding.UTF8.GetBytes(ClearText);

			SymmetricAlgorithm rijn = SymmetricAlgorithm.Create();

			using (MemoryStream ms = new MemoryStream()) {
				byte[] rgbIV = Encoding.ASCII.GetBytes(RGBIV);
				byte[] key = Encoding.ASCII.GetBytes(KEY);
				CryptoStream cs = new CryptoStream(ms, rijn.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write);

				cs.Write(clearTextBytes, 0, clearTextBytes.Length);
				return Convert.ToBase64String(ms.ToArray());
			}
		}

		/// <summary>
		/// A simply rijndael aes decryption, can be used to store passwords
		/// </summary>
		/// <param name="EncryptedText">a base64 encoded rijndael encrypted string</param>
		/// <returns>Decrypeted text</returns>
		public static string Decrypt(this string EncryptedText) {
			byte[] encryptedTextBytes = Convert.FromBase64String(EncryptedText);

			using (MemoryStream ms = new MemoryStream()) {
				SymmetricAlgorithm rijn = SymmetricAlgorithm.Create();


				byte[] rgbIV = Encoding.ASCII.GetBytes(RGBIV);
				byte[] key = Encoding.ASCII.GetBytes(KEY);

				CryptoStream cs = new CryptoStream(ms, rijn.CreateDecryptor(key, rgbIV),
				CryptoStreamMode.Write);

				cs.Write(encryptedTextBytes, 0, encryptedTextBytes.Length);

				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}
	}
}
