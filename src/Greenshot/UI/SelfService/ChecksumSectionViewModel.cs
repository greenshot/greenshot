/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.UI.SelfService
{
    public enum ChecksumStatus
    {
        Match,
        Mismatch,
        Missing,
        Unlisted
    }

    public class ChecksumItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ChecksumStatus _status;
        private string _actualHash;
        private string _expectedHash;
        private long? _fileSize;

        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public string FileName => Path.GetFileName(RelativePath);
        public string Directory => Path.GetDirectoryName(RelativePath);

        public ChecksumStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusBrush));
                    OnPropertyChanged(nameof(StatusHint));
                    OnPropertyChanged(nameof(IsIssue));
                    OnPropertyChanged(nameof(IsMatch));
                    OnPropertyChanged(nameof(IsMismatch));
                    OnPropertyChanged(nameof(IsMissing));
                    OnPropertyChanged(nameof(IsUnlisted));
                }
            }
        }

        public string ActualHash
        {
            get => _actualHash;
            set
            {
                if (_actualHash != value)
                {
                    _actualHash = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExpectedHash
        {
            get => _expectedHash;
            set
            {
                if (_expectedHash != value)
                {
                    _expectedHash = value;
                    OnPropertyChanged();
                }
            }
        }

        public long? FileSize
        {
            get => _fileSize;
            set
            {
                if (_fileSize != value)
                {
                    _fileSize = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FileSizeText));
                }
            }
        }

        public string FileSizeText
        {
            get
            {
                if (!FileSize.HasValue) return "-";
                long b = FileSize.Value;
                if (b >= 1024 * 1024) return $"{b / (1024.0 * 1024.0):F2} MB";
                if (b >= 1024) return $"{b / 1024.0:F1} KB";
                return $"{b:N0} B";
            }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case ChecksumStatus.Match:
                        return Language.GetString("selfservice_checksum_status_match");
                    case ChecksumStatus.Mismatch:
                        return Language.GetString("selfservice_checksum_status_mismatch");
                    case ChecksumStatus.Missing:
                        return Language.GetString("selfservice_checksum_status_missing");
                    case ChecksumStatus.Unlisted:
                        return Language.GetString("selfservice_checksum_status_unlisted");
                    default:
                        return Status.ToString();
                }
            }
        }

        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case ChecksumStatus.Match: return "✔️";
                    case ChecksumStatus.Mismatch: return "❌";
                    case ChecksumStatus.Missing: return "⚠️";
                    case ChecksumStatus.Unlisted: return "ℹ️";
                    default: return "❓";
                }
            }
        }

        public SolidColorBrush StatusBrush
        {
            get
            {
                switch (Status)
                {
                    case ChecksumStatus.Match:
                        return new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Emerald #22C55E
                    case ChecksumStatus.Mismatch:
                        return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red #EF4444
                    case ChecksumStatus.Missing:
                        return new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange #F97316
                    case ChecksumStatus.Unlisted:
                        return new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue #3B82F6
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
        }

        public string StatusHint
        {
            get
            {
                switch (Status)
                {
                    case ChecksumStatus.Match:
                        return Language.GetString("selfservice_checksum_detail_hint_match");
                    case ChecksumStatus.Mismatch:
                        return Language.GetString("selfservice_checksum_detail_hint_mismatch");
                    case ChecksumStatus.Missing:
                        return Language.GetString("selfservice_checksum_detail_hint_missing");
                    case ChecksumStatus.Unlisted:
                        return Language.GetString("selfservice_checksum_detail_hint_unlisted");
                    default:
                        return string.Empty;
                }
            }
        }

        public bool IsIssue => Status == ChecksumStatus.Mismatch || Status == ChecksumStatus.Missing;
        public bool IsMatch => Status == ChecksumStatus.Match;
        public bool IsMismatch => Status == ChecksumStatus.Mismatch;
        public bool IsMissing => Status == ChecksumStatus.Missing;
        public bool IsUnlisted => Status == ChecksumStatus.Unlisted;

        public void RefreshLanguage()
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusHint));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChecksumSectionViewModel : SelfServiceSectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ChecksumSectionViewModel));

        public override string Id => "checksum";
        public override string Title => Language.GetString("selfservice_category_checksum");
        public override string Subtitle => Language.GetString("selfservice_category_checksum_sub");
        public override string Icon => "🔒";

        private string _checksumFilePath;
        private string _baseDirectory;
        private bool _isScanning;
        private int _scanProgressPercentage;
        private string _scanProgressText;
        private string _statusMessage;
        private string _searchQuery = string.Empty;
        private string _activeFilter = "All";
        private ChecksumItemViewModel _selectedItem;
        private CancellationTokenSource _cts;

        private int _totalCount;
        private int _matchedCount;
        private int _mismatchedCount;
        private int _missingCount;
        private int _unlistedCount;
        private bool _checksumFileNotFound;
        private bool _hasEverScanned;

        public string ChecksumFilePath
        {
            get => _checksumFilePath;
            set { _checksumFilePath = value; OnPropertyChanged(); }
        }

        public string BaseDirectory
        {
            get => _baseDirectory;
            set { _baseDirectory = value; OnPropertyChanged(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            private set
            {
                if (_isScanning != value)
                {
                    _isScanning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanStartScan));
                }
            }
        }

        public bool CanStartScan => !IsScanning;

        public int ScanProgressPercentage
        {
            get => _scanProgressPercentage;
            private set { _scanProgressPercentage = value; OnPropertyChanged(); }
        }

        public string ScanProgressText
        {
            get => _scanProgressText;
            private set { _scanProgressText = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        public string ActiveFilter
        {
            get => _activeFilter;
            set
            {
                if (_activeFilter != value)
                {
                    _activeFilter = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        public ChecksumItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedItem));
                }
            }
        }

        public bool HasSelectedItem => SelectedItem != null;

        public ObservableCollection<ChecksumItemViewModel> AllItems { get; } = new ObservableCollection<ChecksumItemViewModel>();
        public ObservableCollection<ChecksumItemViewModel> FilteredItems { get; } = new ObservableCollection<ChecksumItemViewModel>();

        public int TotalCount { get => _totalCount; private set { _totalCount = value; OnPropertyChanged(); } }
        public int MatchedCount { get => _matchedCount; private set { _matchedCount = value; OnPropertyChanged(); } }
        public int MismatchedCount { get => _mismatchedCount; private set { _mismatchedCount = value; OnPropertyChanged(); } }
        public int MissingCount { get => _missingCount; private set { _missingCount = value; OnPropertyChanged(); } }
        public int UnlistedCount { get => _unlistedCount; private set { _unlistedCount = value; OnPropertyChanged(); } }
        public int IssuesCount => MismatchedCount + MissingCount;
        public bool HasCorruptFiles => MismatchedCount > 0;
        public bool HasMissingFiles => MissingCount > 0;
        public bool HasIssues => IssuesCount > 0;
        public bool ChecksumFileNotFound { get => _checksumFileNotFound; private set { _checksumFileNotFound = value; OnPropertyChanged(); } }

        public string BannerTitle
        {
            get
            {
                if (ChecksumFileNotFound)
                    return Language.GetString("selfservice_checksum_status_notfound_title");
                if (HasCorruptFiles)
                    return Language.GetString("selfservice_checksum_status_corrupt_title");
                if (HasMissingFiles)
                    return Language.GetString("selfservice_checksum_status_missing_title");
                return Language.GetString("selfservice_checksum_status_ok_title");
            }
        }

        public string BannerDescription
        {
            get
            {
                if (ChecksumFileNotFound)
                    return Language.GetFormattedString("selfservice_checksum_status_notfound_desc", ChecksumFilePath);
                if (HasCorruptFiles)
                    return Language.GetFormattedString("selfservice_checksum_status_corrupt_desc", MismatchedCount);
                if (HasMissingFiles)
                    return Language.GetFormattedString("selfservice_checksum_status_missing_desc", MissingCount);
                return Language.GetString("selfservice_checksum_status_ok_desc");
            }
        }

        public string BannerIcon
        {
            get
            {
                if (ChecksumFileNotFound || HasCorruptFiles) return "❌";
                if (HasMissingFiles) return "⚠️";
                return "✔️";
            }
        }

        public SolidColorBrush BannerBrush
        {
            get
            {
                if (ChecksumFileNotFound || HasCorruptFiles)
                    return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red #EF4444
                if (HasMissingFiles)
                    return new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange #F97316
                return new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Emerald #22C55E
            }
        }

        public SolidColorBrush BannerBackgroundBrush
        {
            get
            {
                if (ChecksumFileNotFound || HasCorruptFiles)
                    return new SolidColorBrush(Color.FromArgb(24, 239, 68, 68));
                if (HasMissingFiles)
                    return new SolidColorBrush(Color.FromArgb(24, 249, 115, 22));
                return new SolidColorBrush(Color.FromArgb(24, 34, 197, 94));
            }
        }

        public ChecksumSectionViewModel()
        {
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ChecksumFilePath = Path.Combine(BaseDirectory, "checksum.SHA256");
            StatusMessage = Language.GetString("selfservice_category_checksum_sub");
        }

        public override void OnNavigatedTo()
        {
            if (!_hasEverScanned)
            {
                _hasEverScanned = true;
                _ = ValidateChecksumsAsync();
            }
        }

        public override void Refresh()
        {
            _ = ValidateChecksumsAsync();
        }

        public async Task ValidateChecksumsAsync(CancellationToken externalCancellationToken = default)
        {
            if (IsScanning) return;

            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var token = _cts.Token;

            IsScanning = true;
            ScanProgressPercentage = 0;
            ScanProgressText = Language.GetString("selfservice_checksum_searching") != null 
                ? string.Format(Language.GetString("selfservice_checksum_searching"), 0, 0)
                : "Validating files...";

            string baseDir = BaseDirectory;
            string checksumPath = ChecksumFilePath;

            try
            {
                var results = await Task.Run(() => PerformValidation(baseDir, checksumPath, token, (cur, total, name) =>
                {
                    RunOnUi(() =>
                    {
                        if (total > 0)
                        {
                            ScanProgressPercentage = (int)((cur / (double)total) * 100);
                            ScanProgressText = Language.GetFormattedString("selfservice_checksum_searching", cur, total);
                        }
                    });
                }), token);

                if (token.IsCancellationRequested) return;

                RunOnUi(() =>
                {
                    AllItems.Clear();
                    foreach (var item in results.Items)
                    {
                        AllItems.Add(item);
                    }

                    ChecksumFileNotFound = results.FileNotFound;
                    TotalCount = results.TotalCount;
                    MatchedCount = results.MatchedCount;
                    MismatchedCount = results.MismatchedCount;
                    MissingCount = results.MissingCount;
                    UnlistedCount = results.UnlistedCount;

                    UpdateBadgeAndBanners();
                    ApplyFilter();

                    if (SelectedItem == null && FilteredItems.Count > 0)
                    {
                        SelectedItem = FilteredItems.FirstOrDefault(i => i.IsIssue) ?? FilteredItems.FirstOrDefault();
                    }

                    StatusMessage = BannerTitle;
                });
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Checksum validation canceled.";
            }
            catch (Exception ex)
            {
                Log.Error("Error during checksum validation", ex);
                StatusMessage = $"Validation error: {ex.Message}";
            }
            finally
            {
                RunOnUi(() =>
                {
                    IsScanning = false;
                });
            }
        }

        private class ValidationResults
        {
            public List<ChecksumItemViewModel> Items { get; set; } = new List<ChecksumItemViewModel>();
            public bool FileNotFound { get; set; }
            public int TotalCount { get; set; }
            public int MatchedCount { get; set; }
            public int MismatchedCount { get; set; }
            public int MissingCount { get; set; }
            public int UnlistedCount { get; set; }
        }

        private ValidationResults PerformValidation(
            string baseDir, 
            string checksumPath, 
            CancellationToken token, 
            Action<int, int, string> reportProgress)
        {
            var res = new ValidationResults();

            if (!File.Exists(checksumPath))
            {
                res.FileNotFound = true;
                return res;
            }

            // 1. Parse checksum.SHA256 lines
            var expectedList = new List<KeyValuePair<string, string>>();
            var expectedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string[] lines = File.ReadAllLines(checksumPath);
            foreach (string line in lines)
            {
                if (TryParseChecksumLine(line, out string hash, out string relPath))
                {
                    if (CanSkipFile(relPath))
                    {
                        continue;
                    }

                    expectedList.Add(new KeyValuePair<string, string>(relPath, hash));
                    expectedMap[relPath] = hash;
                }
            }

            // 2. Discover all disk files
            var diskFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(baseDir))
            {
                string normalizedBase = baseDir.TrimEnd('\\', '/') + "\\";
                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
                    {
                        token.ThrowIfCancellationRequested();

                        string fileName = Path.GetFileName(filePath);
                        if (string.Equals(fileName, "checksum.SHA256", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string relPath = filePath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase)
                            ? filePath.Substring(normalizedBase.Length)
                            : Path.GetFileName(filePath);

                        // Ignore some files for all checks
                        if (CanSkipFile(relPath))
                        {
                            continue;
                        }

                        // Skip VCS/IDE metadata directories if present
                        if (relPath.StartsWith(".git\\", StringComparison.OrdinalIgnoreCase) ||
                            relPath.StartsWith(".vs\\", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        diskFiles.Add(relPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Directory enumeration encountered an error", ex);
                }
            }

            int totalOperations = expectedList.Count + diskFiles.Count;
            int currentOp = 0;

            // 3. Check expected entries
            foreach (var pair in expectedList)
            {
                token.ThrowIfCancellationRequested();
                currentOp++;
                if (currentOp % 10 == 0 || currentOp == totalOperations)
                {
                    reportProgress(currentOp, totalOperations, pair.Key);
                }

                string relPath = pair.Key;
                string expectedHash = pair.Value;
                string fullPath = Path.Combine(baseDir, relPath);

                var item = new ChecksumItemViewModel
                {
                    RelativePath = relPath,
                    FullPath = fullPath,
                    ExpectedHash = expectedHash
                };

                if (!File.Exists(fullPath))
                {
                    item.Status = ChecksumStatus.Missing;
                    item.ActualHash = "-";
                    item.FileSize = null;
                    res.MissingCount++;
                }
                else
                {
                    diskFiles.Remove(relPath);

                    try
                    {
                        var fi = new FileInfo(fullPath);
                        item.FileSize = fi.Length;
                        string actualHash = ComputeFileSha256(fullPath);
                        item.ActualHash = actualHash;

                        if (string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            item.Status = ChecksumStatus.Match;
                            res.MatchedCount++;
                        }
                        else
                        {
                            item.Status = ChecksumStatus.Mismatch;
                            res.MismatchedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to compute hash for file {fullPath}", ex);
                        item.Status = ChecksumStatus.Mismatch;
                        item.ActualHash = $"Error: {ex.Message}";
                        res.MismatchedCount++;
                    }
                }

                res.Items.Add(item);
            }

            // 4. Any remaining disk files are Unlisted
            foreach (var unlistedRelPath in diskFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();
                currentOp++;
                if (currentOp % 10 == 0 || currentOp == totalOperations)
                {
                    reportProgress(currentOp, totalOperations, unlistedRelPath);
                }

                string fullPath = Path.Combine(baseDir, unlistedRelPath);
                var item = new ChecksumItemViewModel
                {
                    RelativePath = unlistedRelPath,
                    FullPath = fullPath,
                    ExpectedHash = "-",
                    Status = ChecksumStatus.Unlisted
                };

                try
                {
                    var fi = new FileInfo(fullPath);
                    item.FileSize = fi.Length;
                    item.ActualHash = ComputeFileSha256(fullPath);
                }
                catch (Exception ex)
                {
                    item.ActualHash = $"Error: {ex.Message}";
                }

                res.UnlistedCount++;
                res.Items.Add(item);
            }

            res.TotalCount = res.Items.Count;
            return res;
        }

        public static bool TryParseChecksumLine(string line, out string hash, out string relativePath)
        {
            hash = null;
            relativePath = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            line = line.Trim();
            if (line.StartsWith("#") || line.StartsWith(";")) return false;

            if (line.Length < 66) return false;

            string potentialHash = line.Substring(0, 64);
            if (!IsHexString(potentialHash)) return false;

            string rest = line.Substring(64).Trim();
            if (string.IsNullOrEmpty(rest)) return false;

            if (rest.StartsWith("*"))
            {
                rest = rest.Substring(1).Trim();
            }

            if (string.IsNullOrEmpty(rest)) return false;

            hash = potentialHash.ToUpperInvariant();
            relativePath = rest.Replace('/', '\\');
            return true;
        }

        private static bool IsHexString(string s)
        {
            foreach (char c in s)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether a given relative or file path corresponds to a file which can be skipped for checksum validation.
        /// </summary>
        public static bool CanSkipFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fileName = Path.GetFileName(path);
            if (fileName.StartsWith("language-", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("language_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string normalized = path.Replace('/', '\\');
            var segments = normalized.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (string.Equals(segment, "Languages", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string ComputeFileSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }

        public void ApplyFilter()
        {
            FilteredItems.Clear();

            string query = (SearchQuery ?? string.Empty).Trim();
            string filter = ActiveFilter ?? "All";

            IEnumerable<ChecksumItemViewModel> queryable = AllItems;

            // Apply category filter
            switch (filter)
            {
                case "Issues":
                    queryable = queryable.Where(i => i.IsIssue);
                    break;
                case "Mismatch":
                    queryable = queryable.Where(i => i.Status == ChecksumStatus.Mismatch);
                    break;
                case "Missing":
                    queryable = queryable.Where(i => i.Status == ChecksumStatus.Missing);
                    break;
                case "Unlisted":
                    queryable = queryable.Where(i => i.Status == ChecksumStatus.Unlisted);
                    break;
                case "Match":
                    queryable = queryable.Where(i => i.Status == ChecksumStatus.Match);
                    break;
            }

            // Apply search query
            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(i =>
                    (i.RelativePath != null && i.RelativePath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.ActualHash != null && i.ActualHash.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.ExpectedHash != null && i.ExpectedHash.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.StatusText != null && i.StatusText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Order so that issues are prominently listed first
            var ordered = queryable
                .OrderBy(i => i.Status == ChecksumStatus.Mismatch ? 0 :
                              i.Status == ChecksumStatus.Missing ? 1 :
                              i.Status == ChecksumStatus.Unlisted ? 2 : 3)
                .ThenBy(i => i.RelativePath, StringComparer.OrdinalIgnoreCase);

            foreach (var item in ordered)
            {
                FilteredItems.Add(item);
            }

            if (SelectedItem != null && !FilteredItems.Contains(SelectedItem))
            {
                SelectedItem = FilteredItems.FirstOrDefault();
            }
        }

        private void UpdateBadgeAndBanners()
        {
            if (HasCorruptFiles || ChecksumFileNotFound)
            {
                BadgeText = $"{MismatchedCount + (ChecksumFileNotFound ? 1 : 0)} ❌";
                BadgeBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else if (HasMissingFiles)
            {
                BadgeText = $"{MissingCount} ⚠️";
                BadgeBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22));
            }
            else if (TotalCount > 0)
            {
                BadgeText = "OK";
                BadgeBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            }
            else
            {
                BadgeText = null;
            }

            OnPropertyChanged(nameof(HasCorruptFiles));
            OnPropertyChanged(nameof(HasMissingFiles));
            OnPropertyChanged(nameof(HasIssues));
            OnPropertyChanged(nameof(IssuesCount));
            OnPropertyChanged(nameof(BannerTitle));
            OnPropertyChanged(nameof(BannerDescription));
            OnPropertyChanged(nameof(BannerIcon));
            OnPropertyChanged(nameof(BannerBrush));
            OnPropertyChanged(nameof(BannerBackgroundBrush));
        }

        public void CopyReportToClipboard()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Greenshot Installation Integrity Report");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Greenshot Version: {EnvironmentInfo.GetGreenshotVersion()} ({OsInfo.Bits}-bit)");
                sb.AppendLine($"Base Directory: {BaseDirectory}");
                sb.AppendLine($"Checksum File: {ChecksumFilePath}");
                sb.AppendLine();
                sb.AppendLine("Summary:");
                sb.AppendLine($"  Total Files Scanned: {TotalCount}");
                sb.AppendLine($"  Valid / Matched:     {MatchedCount}");
                sb.AppendLine($"  Mismatched (Corrupt): {MismatchedCount}");
                sb.AppendLine($"  Missing Files:       {MissingCount}");
                sb.AppendLine($"  Unlisted Files:      {UnlistedCount}");
                sb.AppendLine($"  Overall Status:      {BannerTitle}");
                sb.AppendLine();

                if (MismatchedCount > 0)
                {
                    sb.AppendLine("MISMATCHED FILES (POSSIBLE CORRUPT INSTALLATION):");
                    foreach (var item in AllItems.Where(i => i.Status == ChecksumStatus.Mismatch))
                    {
                        sb.AppendLine($"  [MISMATCH] {item.RelativePath}");
                        sb.AppendLine($"    Expected SHA-256: {item.ExpectedHash}");
                        sb.AppendLine($"    Actual SHA-256:   {item.ActualHash}");
                    }
                    sb.AppendLine();
                }

                if (MissingCount > 0)
                {
                    sb.AppendLine("MISSING FILES:");
                    foreach (var item in AllItems.Where(i => i.Status == ChecksumStatus.Missing))
                    {
                        sb.AppendLine($"  [MISSING] {item.RelativePath} (Expected: {item.ExpectedHash})");
                    }
                    sb.AppendLine();
                }

                if (UnlistedCount > 0)
                {
                    sb.AppendLine("UNLISTED FILES:");
                    foreach (var item in AllItems.Where(i => i.Status == ChecksumStatus.Unlisted))
                    {
                        sb.AppendLine($"  [UNLISTED] {item.RelativePath} (Actual: {item.ActualHash})");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("ALL FILES:");
                sb.AppendLine("Status\tFile\tActual SHA-256\tExpected SHA-256\tSize");
                foreach (var item in AllItems)
                {
                    sb.AppendLine($"{item.Status}\t{item.RelativePath}\t{item.ActualHash}\t{item.ExpectedHash}\t{item.FileSizeText}");
                }

                Clipboard.SetText(sb.ToString());
                StatusMessage = Language.GetString("selfservice_checksum_copied_report") ?? "Integrity report copied to clipboard!";
            }
            catch (Exception ex)
            {
                Log.Error("Failed to copy integrity report to clipboard", ex);
                StatusMessage = $"Copy failed: {ex.Message}";
            }
        }

        public void CopySelectedDetails()
        {
            if (SelectedItem == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"File: {SelectedItem.RelativePath}");
                sb.AppendLine($"Status: {SelectedItem.StatusText}");
                sb.AppendLine($"Actual SHA-256:   {SelectedItem.ActualHash}");
                sb.AppendLine($"Expected SHA-256: {SelectedItem.ExpectedHash}");
                sb.AppendLine($"Size: {SelectedItem.FileSizeText}");
                sb.AppendLine($"Path: {SelectedItem.FullPath}");
                Clipboard.SetText(sb.ToString());
                StatusMessage = Language.GetString("selfservice_copied") ?? "Copied!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Copy failed: {ex.Message}";
            }
        }

        public void OpenChecksumFile()
        {
            try
            {
                if (File.Exists(ChecksumFilePath))
                {
                    ExplorerHelper.OpenInExplorer(ChecksumFilePath);
                    StatusMessage = $"Opened Explorer for: {ChecksumFilePath}";
                }
                else if (Directory.Exists(BaseDirectory))
                {
                    ExplorerHelper.OpenInExplorer(BaseDirectory);
                    StatusMessage = $"Checksum file not found, opened containing folder: {BaseDirectory}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open file: {ex.Message}";
            }
        }

        public override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            OnPropertyChanged(nameof(BannerTitle));
            OnPropertyChanged(nameof(BannerDescription));
            foreach (var item in AllItems)
            {
                item.RefreshLanguage();
            }
        }

        public void Cleanup()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
