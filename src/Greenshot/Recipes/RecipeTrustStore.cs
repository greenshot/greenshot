/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Record representing an approved external capture recipe file.
    /// </summary>
    public class RecipeTrustRecord
    {
        public string FilePath { get; set; }
        public string Sha256Hash { get; set; }
        public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
        public bool AllowExternalCommands { get; set; }
        public string RecipeName { get; set; }
        public string RecipeVersion { get; set; }
    }

    /// <summary>
    /// Manages cryptographic approvals of external recipe files.
    /// Uses Windows DPAPI (ProtectedData) with application-specific entropy to prevent unauthorized
    /// third-party processes in the user's session from writing spoofed approvals into configuration.
    /// </summary>
    public static class RecipeTrustStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeTrustStore));
        private static readonly object LockObj = new object();

        private static readonly byte[] Entropy = SHA256.Create().ComputeHash(
            Encoding.UTF8.GetBytes($"Greenshot.RecipeTrust.Salt.{Environment.MachineName}.6f8c2b1e")
        );

        private static string StoreFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Greenshot",
            "recipe_trust.dat"
        );

        private static Dictionary<string, RecipeTrustRecord> _records;

        /// <summary>
        /// Computes the SHA-256 hash (lowercase hex) of a given file.
        /// </summary>
        public static string ComputeSha256(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static Dictionary<string, RecipeTrustRecord> LoadRecords()
        {
            if (_records != null) return _records;

            _records = new Dictionary<string, RecipeTrustRecord>(StringComparer.OrdinalIgnoreCase);

            string path = StoreFilePath;
            if (!File.Exists(path)) return _records;

            try
            {
                byte[] cipherBytes = File.ReadAllBytes(path);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(plainBytes);
                var list = JsonConvert.DeserializeObject<List<RecipeTrustRecord>>(json);

                if (list != null)
                {
                    foreach (var rec in list)
                    {
                        if (!string.IsNullOrEmpty(rec.FilePath))
                        {
                            _records[rec.FilePath] = rec;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to read encrypted recipe trust store; starting with fresh database.", ex);
            }

            return _records;
        }

        private static void SaveRecords()
        {
            try
            {
                string path = StoreFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var list = new List<RecipeTrustRecord>(_records.Values);
                string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(path, cipherBytes);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save encrypted recipe trust store.", ex);
            }
        }

        /// <summary>
        /// Checks whether the specified recipe file has an approved SHA-256 fingerprint on record.
        /// </summary>
        public static bool IsRecipeApproved(string filePath, out string currentHash, out bool allowExternalCommands)
        {
            currentHash = null;
            allowExternalCommands = false;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

            currentHash = ComputeSha256(filePath);
            if (string.IsNullOrEmpty(currentHash)) return false;

            lock (LockObj)
            {
                var records = LoadRecords();
                string fullPath = Path.GetFullPath(filePath);

                if (records.TryGetValue(fullPath, out var record))
                {
                    if (string.Equals(record.Sha256Hash, currentHash, StringComparison.OrdinalIgnoreCase))
                    {
                        allowExternalCommands = record.AllowExternalCommands;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the stored trust record for the specified recipe file, if any exists.
        /// </summary>
        public static RecipeTrustRecord GetTrustRecord(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            lock (LockObj)
            {
                var records = LoadRecords();
                try
                {
                    string fullPath = Path.GetFullPath(filePath);
                    return records.TryGetValue(fullPath, out var record) ? record : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Records explicit user trust and approval for a recipe file.
        /// </summary>
        public static void RecordApproval(string filePath, string sha256, bool allowExternalCommands, string recipeName = null, string recipeVersion = null)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            string fullPath = Path.GetFullPath(filePath);
            sha256 = sha256 ?? ComputeSha256(fullPath);

            lock (LockObj)
            {
                var records = LoadRecords();
                records[fullPath] = new RecipeTrustRecord
                {
                    FilePath = fullPath,
                    Sha256Hash = sha256,
                    ApprovedAt = DateTime.UtcNow,
                    AllowExternalCommands = allowExternalCommands,
                    RecipeName = recipeName,
                    RecipeVersion = recipeVersion
                };
                SaveRecords();
            }

            Log.InfoFormat("Recorded cryptographic user approval for recipe '{0}' (SHA256: {1}, AllowExternalCommands: {2})", fullPath, sha256, allowExternalCommands);
        }

        /// <summary>
        /// Revokes approval for a given recipe file.
        /// </summary>
        public static void RevokeApproval(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            string fullPath = Path.GetFullPath(filePath);

            lock (LockObj)
            {
                var records = LoadRecords();
                if (records.Remove(fullPath))
                {
                    SaveRecords();
                    Log.InfoFormat("Revoked approval for recipe '{0}'", fullPath);
                }
            }
        }
    }
}
