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
using log4net;

namespace Greenshot.Base.Trust;

/// <summary>
/// Which content of a file the user approved, by its SHA-256
/// </summary>
public interface IFileApprovals
{
    /// <summary>
    /// True when the user approved this content (hash) of the file
    /// </summary>
    bool IsApproved(string filePath, string sha256);

    /// <summary>
    /// True when the user approved some content of the file: Greenshot wrote or imported it, it may have changed since
    /// </summary>
    bool IsKnown(string filePath);

    /// <summary>
    /// The user approved this content of the file, e.g. Greenshot wrote it or the user confirmed it
    /// </summary>
    void Approve(string filePath, string sha256);

    /// <summary>
    /// The file is gone or not used anymore
    /// </summary>
    void Revoke(string filePath);
}

/// <summary>
/// An approval of a file: its full path and the hash of the approved content
/// </summary>
public sealed class FileApprovalRecord
{
    public string FilePath { get; set; }

    public string Sha256Hash { get; set; }

    public DateTime ApprovedAt { get; set; }
}

/// <summary>
/// The approvals of the definition files (commands, uploaders) of the user, like the recipe approvals: in
/// %LocalAppData%\Greenshot\definition_trust.dat, protected with DPAPI, so a file copied into a folder or changed outside
/// Greenshot isn't used before the user approves it.
/// </summary>
public sealed class FileApprovals : IFileApprovals
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(FileApprovals));
    private readonly object _lock = new object();
    private readonly ProtectedRecordFile<FileApprovalRecord> _file;
    private Dictionary<string, FileApprovalRecord> _records;

    /// <summary>
    /// The approvals of the definition files of the user
    /// </summary>
    public static IFileApprovals Definitions { get; } = new FileApprovals(() => ProtectedRecordFile<FileApprovalRecord>.InLocalAppData("definition_trust.dat"));

    /// <param name="storeFilePath">the file of the approvals</param>
    public FileApprovals(Func<string> storeFilePath)
    {
        _file = new ProtectedRecordFile<FileApprovalRecord>(storeFilePath, $"Greenshot.DefinitionTrust.Salt.{Environment.MachineName}.3d91a7c4", record => record.FilePath);
    }

    public bool IsApproved(string filePath, string sha256)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(sha256)) return false;
        lock (_lock)
        {
            return Records().TryGetValue(Path.GetFullPath(filePath), out var record) && string.Equals(record.Sha256Hash, sha256, StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsKnown(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        lock (_lock)
        {
            return Records().ContainsKey(Path.GetFullPath(filePath));
        }
    }

    public void Approve(string filePath, string sha256)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(sha256)) return;
        string fullPath = Path.GetFullPath(filePath);
        lock (_lock)
        {
            Records()[fullPath] = new FileApprovalRecord { FilePath = fullPath, Sha256Hash = sha256, ApprovedAt = DateTime.UtcNow };
            _file.Save(Records().Values);
        }

        Log.InfoFormat("Approved {0} (SHA256: {1})", fullPath, sha256);
    }

    public void Revoke(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        lock (_lock)
        {
            if (Records().Remove(Path.GetFullPath(filePath)))
            {
                _file.Save(Records().Values);
            }
        }
    }

    private Dictionary<string, FileApprovalRecord> Records() => _records ??= _file.Load();
}
