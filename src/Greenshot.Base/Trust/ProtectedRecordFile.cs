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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Greenshot.Base.Serialization;
using log4net;

namespace Greenshot.Base.Trust;

/// <summary>
/// A list of records in a file of the user, protected with DPAPI (ProtectedData, current user) and an entropy of the kind of
/// records, so another program of the user can't write approvals into it unnoticed. Used for the approvals of recipes and of
/// definition files. Not locked: the owner locks.
/// </summary>
/// <typeparam name="TRecord">the record, JSON with <see cref="GreenshotJson.Options"/></typeparam>
public sealed class ProtectedRecordFile<TRecord> where TRecord : class
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ProtectedRecordFile<TRecord>));
    private readonly byte[] _entropy;
    private readonly Func<string> _path;
    private readonly Func<TRecord, string> _keyOf;

    /// <param name="path">the file, asked at each load and save (tests use another one)</param>
    /// <param name="entropy">the entropy of this kind of records (hashed), it should hold the name of the computer</param>
    /// <param name="keyOf">the key of a record (a full path), records without one aren't kept</param>
    public ProtectedRecordFile(Func<string> path, string entropy, Func<TRecord, string> keyOf)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _keyOf = keyOf ?? throw new ArgumentNullException(nameof(keyOf));
        _entropy = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(entropy ?? throw new ArgumentNullException(nameof(entropy))));
    }

    /// <summary>
    /// A file in %LocalAppData%\Greenshot
    /// </summary>
    public static string InLocalAppData(string fileName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Greenshot", fileName);

    /// <summary>
    /// The records by key (ignoring case). A file which can't be read (e.g. a profile copied to another computer) is moved to
    /// ".unreadable" next to it, the next save would overwrite it, and there are no records.
    /// </summary>
    public Dictionary<string, TRecord> Load()
    {
        var records = new Dictionary<string, TRecord>(StringComparer.OrdinalIgnoreCase);
        string path = _path();
        if (!File.Exists(path)) return records;

        try
        {
            byte[] plainBytes = ProtectedData.Unprotect(File.ReadAllBytes(path), _entropy, DataProtectionScope.CurrentUser);
            var list = JsonSerializer.Deserialize<List<TRecord>>(Encoding.UTF8.GetString(plainBytes), GreenshotJson.Options);
            foreach (var record in list ?? new List<TRecord>())
            {
                string key = record == null ? null : _keyOf(record);
                if (!string.IsNullOrEmpty(key))
                {
                    records[key] = record;
                }
            }
        }
        catch (Exception ex)
        {
            string unreadablePath = path + ".unreadable";
            Log.Warn($"Failed to read the encrypted approvals {path}, they are moved to '{unreadablePath}'; starting without approvals.", ex);
            try
            {
                File.Delete(unreadablePath);
                File.Move(path, unreadablePath);
            }
            catch (Exception moveException)
            {
                Log.Warn("Could not move the unreadable approvals.", moveException);
            }
        }

        return records;
    }

    /// <summary>
    /// Write the records, a failure is logged
    /// </summary>
    public void Save(IEnumerable<TRecord> records)
    {
        string path = _path();
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(new List<TRecord>(records), GreenshotJson.IndentedOptions);
            byte[] cipherBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), _entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(path, cipherBytes);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save the encrypted approvals {path}.", ex);
        }
    }
}
