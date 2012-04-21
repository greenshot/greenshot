using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace GreenshotPlugin.Core {
	public static class EncryptionHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger("EncryptionHelper");
		private const string RGBIV = "dlgjowejgogkklwj";
		private const string KEY = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

		/// <summary>
		/// A simply rijndael aes encryption, can be used to store passwords
		/// </summary>
		/// <param name="ClearText">the string to call upon</param>
		/// <returns>an encryped string in base64 form</returns>
		public static string Encrypt(this string ClearText) {
			string returnValue = null;
			try {
				byte[] clearTextBytes = Encoding.ASCII.GetBytes(ClearText);
				SymmetricAlgorithm rijn = SymmetricAlgorithm.Create();
	
				using (MemoryStream ms = new MemoryStream()) {
					byte[] rgbIV = Encoding.ASCII.GetBytes(RGBIV);
					byte[] key = Encoding.ASCII.GetBytes(KEY);
					CryptoStream cs = new CryptoStream(ms, rijn.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write);
	
					cs.Write(clearTextBytes, 0, clearTextBytes.Length);
	
					cs.Close();
					returnValue = Convert.ToBase64String(ms.ToArray());
				}
			} catch (Exception ex) {
				LOG.ErrorFormat("Error encrypting, error: ", ex.Message);
			}
			return returnValue;
		}

		/// <summary>
		/// A simply rijndael aes decryption, can be used to store passwords
		/// </summary>
		/// <param name="EncryptedText">a base64 encoded rijndael encrypted string</param>
		/// <returns>Decrypeted text</returns>
		public static string Decrypt(this string EncryptedText) {
			string returnValue = null;
			try {
				byte[] encryptedTextBytes = Convert.FromBase64String(EncryptedText);
				using (MemoryStream ms = new MemoryStream()) {
					SymmetricAlgorithm rijn = SymmetricAlgorithm.Create();
	
	
					byte[] rgbIV = Encoding.ASCII.GetBytes(RGBIV);
					byte[] key = Encoding.ASCII.GetBytes(KEY);
	
					CryptoStream cs = new CryptoStream(ms, rijn.CreateDecryptor(key, rgbIV),
					CryptoStreamMode.Write);
	
					cs.Write(encryptedTextBytes, 0, encryptedTextBytes.Length);
	
					cs.Close();
					returnValue = Encoding.ASCII.GetString(ms.ToArray());
				}
			} catch (Exception ex) {
				LOG.ErrorFormat("Error decrypting {0}, error: ", EncryptedText, ex.Message);
			}

			return returnValue;
		}
	}
}
