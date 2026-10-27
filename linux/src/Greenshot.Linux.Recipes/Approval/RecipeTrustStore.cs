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

#nullable disable

using Greenshot.Linux.Core.Serialization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Greenshot.Linux.Recipes.Approval
{

    /// <summary>
    /// Manages cryptographic approvals of external recipe files.
    /// Windows uses DPAPI (ProtectedData) to keep other processes from writing spoofed approvals; Linux has none, the store is
    /// encrypted with AES-GCM with a random key of the keyring (<see cref="ProtectionKey"/>), the file only readable by the user.
    /// </summary>
    public static class RecipeTrustStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeTrustStore));
        private static readonly object LockObj = new object();

        /// <summary>
        /// The 256-bit key of the store, kept in the keyring by the app; without one (tests) a key derived from the machine
        /// </summary>
        public static byte[] ProtectionKey { get; set; }

        private static byte[] Key => ProtectionKey ?? SHA256.HashData(Encoding.UTF8.GetBytes($"Greenshot.RecipeTrust.Salt.{Environment.MachineName}.6f8c2b1e"));

        private const int NonceSize = 12;
        private const int TagSize = 16;

        private static byte[] Protect(byte[] plain)
        {
            var result = new byte[NonceSize + TagSize + plain.Length];
            var nonce = result.AsSpan(0, NonceSize);
            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(Key, TagSize);
            aes.Encrypt(nonce, plain, result.AsSpan(NonceSize + TagSize), result.AsSpan(NonceSize, TagSize));
            return result;
        }

        private static byte[] Unprotect(byte[] cipher)
        {
            if (cipher.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("The store is too short");
            }

            var plain = new byte[cipher.Length - NonceSize - TagSize];
            using var aes = new AesGcm(Key, TagSize);
            aes.Decrypt(cipher.AsSpan(0, NonceSize), cipher.AsSpan(NonceSize + TagSize), cipher.AsSpan(NonceSize, TagSize), plain);
            return plain;
        }

        private static string _storeFilePathOverride;

        private static string StoreFilePath => _storeFilePathOverride ?? Path.Combine(Greenshot.Linux.Core.Configuration.XdgDirectories.StateHome, "greenshot", "recipe_trust.dat");

        /// <summary>
        /// Uses another store file (tests), null for the default one
        /// </summary>
        internal static void UseStoreFile(string storeFilePath)
        {
            lock (LockObj)
            {
                _storeFilePathOverride = storeFilePath;
                _records = null;
            }
        }

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

        /// <summary>
        /// Computes the SHA-256 hash (lowercase hex) of the given bytes: use the bytes that were read once, shown and parsed,
        /// so the approval belongs to exactly what the user saw.
        /// </summary>
        public static string ComputeSha256(byte[] content)
        {
            if (content == null) return null;

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(content);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// The longest file content kept in a record, to show the changes later
        /// </summary>
        public const int MaxApprovedContentLength = 256 * 1024;

        /// <summary>
        /// The approval of a recipe of the file, when the record belongs to this content (hash). Null when the user didn't
        /// approve this recipe of this version of the file.
        /// </summary>
        public static RecipeApproval GetApproval(string filePath, string sha256, string recipeId)
        {
            var record = GetTrustRecord(filePath);
            if (record == null || string.IsNullOrEmpty(sha256) || !string.Equals(record.Sha256Hash, sha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return record.GetApproval(recipeId);
        }

        /// <summary>
        /// Records what the user approved for one recipe of a file with this content. Approvals of the other recipes of the same
        /// content are kept, a new content starts a new record.
        /// </summary>
        public static void RecordApproval(string filePath, string sha256, RecipeApproval approval, string content = null, string origin = null, string recipeName = null, string recipeVersion = null)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(sha256) || approval == null) return;

            string fullPath = Path.GetFullPath(filePath);
            lock (LockObj)
            {
                var records = LoadRecords();
                if (!records.TryGetValue(fullPath, out var record) || record.Recipes == null ||
                    !string.Equals(record.Sha256Hash, sha256, StringComparison.OrdinalIgnoreCase))
                {
                    record = new RecipeTrustRecord
                    {
                        FilePath = fullPath,
                        Sha256Hash = sha256,
                        Recipes = new List<RecipeApproval>(),
                        Origin = origin
                    };
                    records[fullPath] = record;
                }
                record.ApprovedAt = DateTime.UtcNow;
                record.RecipeName = recipeName ?? record.RecipeName;
                record.RecipeVersion = recipeVersion ?? record.RecipeVersion;
                record.Origin = origin ?? record.Origin;
                if (content != null && content.Length <= MaxApprovedContentLength)
                {
                    record.ApprovedContent = content;
                }
                record.AllowExternalCommands = approval.IsGateAllowed(Greenshot.Linux.Recipes.Pipeline.RecipeGateType.ExternalCommand);
                record.Recipes.RemoveAll(r => string.Equals(r.RecipeId, approval.RecipeId, StringComparison.OrdinalIgnoreCase));
                record.Recipes.Add(approval);
                SaveRecords();
            }

            Log.InfoFormat("Recorded user approval for recipe '{0}' in '{1}' (SHA256: {2}, triggers: {3}, allowed: {4})", approval.RecipeId, fullPath, sha256,
                approval.AllTriggers ? "all" : string.Join(",", approval.ApprovedTriggers ?? new List<string>()),
                string.Join(",", approval.AllowedGates ?? new List<Greenshot.Linux.Recipes.Pipeline.RecipeGateType>()));
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
                byte[] plainBytes = Unprotect(cipherBytes);
                string json = Encoding.UTF8.GetString(plainBytes);
                var list = JsonSerializer.Deserialize<List<RecipeTrustRecord>>(json, GreenshotJson.Options);

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
                // Kept next to it, the next approval would overwrite it: e.g. a profile copied to another computer can't decrypt it
                string unreadablePath = path + ".unreadable";
                Log.Warn($"Failed to read encrypted recipe trust store, it is moved to '{unreadablePath}'; starting with fresh database.", ex);
                try
                {
                    File.Delete(unreadablePath);
                    File.Move(path, unreadablePath);
                }
                catch (Exception moveException)
                {
                    Log.Warn("Could not move the unreadable recipe trust store.", moveException);
                }
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
                string json = JsonSerializer.Serialize(list, GreenshotJson.IndentedOptions);
                byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                byte[] cipherBytes = Protect(plainBytes);
                var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }

                using (var stream = new FileStream(path + ".tmp", options))
                {
                    stream.Write(cipherBytes);
                }

                File.Move(path + ".tmp", path, overwrite: true);
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
        /// Revokes the approval of one recipe of a file; the record goes when no approved recipe is left in it.
        /// </summary>
        public static void RevokeRecipeApproval(string filePath, string recipeId)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            string fullPath = Path.GetFullPath(filePath);
            lock (LockObj)
            {
                var records = LoadRecords();
                if (!records.TryGetValue(fullPath, out var record))
                {
                    return;
                }
                record.Recipes?.RemoveAll(r => string.Equals(r.RecipeId, recipeId, StringComparison.OrdinalIgnoreCase));
                if (record.Recipes == null || record.Recipes.Count == 0)
                {
                    records.Remove(fullPath);
                }
                SaveRecords();
            }
            Log.InfoFormat("Revoked the approval of recipe '{0}' in '{1}'", recipeId, fullPath);
        }

        /// <summary>
        /// Removes the records of all other files, with their approved content: the files which aren't used anymore
        /// </summary>
        public static void KeepOnly(IEnumerable<string> filePaths)
        {
            var keep = new HashSet<string>(filePaths ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            lock (LockObj)
            {
                var records = LoadRecords();
                var unused = records.Keys.Where(path => !keep.Contains(path)).ToList();
                if (unused.Count == 0)
                {
                    return;
                }
                foreach (var path in unused)
                {
                    records.Remove(path);
                }
                SaveRecords();
                Log.InfoFormat("Removed the approvals of {0} recipe file(s) which aren't used anymore.", unused.Count);
            }
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
