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
using System.IO;
using System.Threading;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.UI.SelfService;
using Xunit;

namespace Greenshot.Tests.Forms
{
    public class SelfServiceTests
    {
        public SelfServiceTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void SelfServiceViewModel_Initialization_SetsDefaultSectionsAndSelection()
        {
            var vm = new SelfServiceViewModel();

            Assert.NotNull(vm.Sections);
            Assert.Equal(5, vm.Sections.Count);

            Assert.NotNull(vm.SystemInfoSection);
            Assert.NotNull(vm.FileInfoSection);
            Assert.NotNull(vm.ClipboardSection);
            Assert.NotNull(vm.HotkeySection);
            Assert.NotNull(vm.ChecksumSection);

            Assert.Equal("system", vm.CurrentSectionId);
            Assert.Equal(vm.SystemInfoSection, vm.SelectedSection);
            Assert.True(vm.SystemInfoSection.IsSelected);
        }

        [Fact]
        public void SelfServiceViewModel_SelectSection_SwitchesCurrentSection()
        {
            var vm = new SelfServiceViewModel();

            vm.SelectSection("files");
            Assert.Equal("files", vm.CurrentSectionId);
            Assert.Equal(vm.FileInfoSection, vm.SelectedSection);
            Assert.True(vm.FileInfoSection.IsSelected);
            Assert.False(vm.SystemInfoSection.IsSelected);

            vm.SelectSection("clipboard");
            Assert.Equal("clipboard", vm.CurrentSectionId);
            Assert.Equal(vm.ClipboardSection, vm.SelectedSection);

            vm.SelectSection("hotkeys");
            Assert.Equal("hotkeys", vm.CurrentSectionId);
            Assert.Equal(vm.HotkeySection, vm.SelectedSection);

            vm.SelectSection("checksum");
            Assert.Equal("checksum", vm.CurrentSectionId);
            Assert.Equal(vm.ChecksumSection, vm.SelectedSection);

            // Unknown section defaults back to first section
            vm.SelectSection("unknown_section");
            Assert.Equal("system", vm.CurrentSectionId);
        }

        [Fact]
        public void SelfServiceViewModel_ThemeBrushes_FollowWpfThemeHelper()
        {
            var vm = new SelfServiceViewModel();

            Assert.NotNull(vm.WindowBackgroundBrush);
            Assert.NotNull(vm.CardBackgroundBrush);
            Assert.NotNull(vm.CardBorderBrush);
            Assert.NotNull(vm.TextPrimaryBrush);
            Assert.NotNull(vm.TextSecondaryBrush);
            Assert.NotNull(vm.AccentBrush);
            Assert.NotNull(vm.ThemeToggleIcon);

            bool wasNotified = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelfServiceViewModel.WindowBackgroundBrush))
                {
                    wasNotified = true;
                }
            };

            vm.ToggleTheme();
            Assert.True(wasNotified);

            // Revert back
            vm.ToggleTheme();
        }

        [Fact]
        public void SystemInfoSectionViewModel_PopulatesMetricsAndReport()
        {
            var sysInfo = new SystemInfoSectionViewModel();

            Assert.NotNull(sysInfo.WorkingSetText);
            Assert.NotNull(sysInfo.PeakWorkingSetText);
            Assert.NotNull(sysInfo.PrivateMemoryText);
            Assert.NotNull(sysInfo.GcMemoryText);
            Assert.NotNull(sysInfo.SystemMemoryText);
            Assert.NotNull(sysInfo.ProcessStatsText);
            Assert.NotNull(sysInfo.UptimeText);
            Assert.NotNull(sysInfo.EnvironmentReport);

            Assert.Contains("Memory", sysInfo.EnvironmentReport);
            Assert.Contains("Greenshot", sysInfo.EnvironmentReport);
            Assert.Contains("PID:", sysInfo.ProcessStatsText);
        }

        [Fact]
        public void FileInfoSectionViewModel_ResolvesLogAndConfigPaths()
        {
            var fileInfo = new FileInfoSectionViewModel();

            Assert.NotNull(fileInfo.LogFilePath);
            Assert.NotNull(fileInfo.ConfigFilePath);

            // Test loading recent log doesn't crash even if file does or doesn't exist
            fileInfo.LoadRecentLog();
            Assert.NotNull(fileInfo.RecentLogContent);
        }

        [Fact]
        public void ClipboardSectionViewModel_QueriesFormatsAndStatusSafely()
        {
            var clip = new ClipboardSectionViewModel();

            Assert.NotNull(clip.Formats);
            Assert.NotEmpty(clip.Formats);
            Assert.NotNull(clip.StatusHeader);

            // Instant check
            clip.CheckClipboardStatus(logToMonitor: true);
            Assert.NotNull(clip.StatusHeader);

            // Loop monitoring toggles cleanly
            Assert.False(clip.IsMonitoring);
            clip.StartMonitoring();
            Assert.True(clip.IsMonitoring);
            Assert.Contains("Stop", clip.MonitorToggleText);

            clip.StopMonitoring();
            Assert.False(clip.IsMonitoring);
            Assert.Contains("Start", clip.MonitorToggleText);
        }

        [Fact]
        public void HotkeySectionViewModel_PopulatesHotkeysAndContenders()
        {
            var hotkeys = new HotkeySectionViewModel();

            Assert.NotNull(hotkeys.Hotkeys);
            Assert.Equal(5, hotkeys.Hotkeys.Count);

            Assert.Contains(hotkeys.Hotkeys, h => h.ConfigKey == "RegionHotkey" && !string.IsNullOrEmpty(h.ActionName));
            Assert.Contains(hotkeys.Hotkeys, h => h.ConfigKey == "WindowHotkey" && !string.IsNullOrEmpty(h.ActionName));
            Assert.Contains(hotkeys.Hotkeys, h => h.ConfigKey == "FullscreenHotkey" && !string.IsNullOrEmpty(h.ActionName));
            Assert.Contains(hotkeys.Hotkeys, h => h.ConfigKey == "LastregionHotkey" && !string.IsNullOrEmpty(h.ActionName));
            Assert.Contains(hotkeys.Hotkeys, h => h.ConfigKey == "ClipboardHotkey" && !string.IsNullOrEmpty(h.ActionName));

            Assert.NotNull(hotkeys.Contenders);
            Assert.NotEmpty(hotkeys.Contenders);
            Assert.Contains(hotkeys.Contenders, c => c.Name == "ShareX");
            Assert.Contains(hotkeys.Contenders, c => c.Name == "Windows Snipping Tool");
        }

        [Fact]
        public void SelfServiceWindow_STAThread_InstantiatesWindowAndDataContext()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var window = new SelfServiceWindow("clipboard");
                    Assert.NotNull(window.DataContext);
                    Assert.IsType<SelfServiceViewModel>(window.DataContext);

                    var vm = window.ViewModel;
                    Assert.Equal("clipboard", vm.CurrentSectionId);
                    Assert.Equal(vm.ClipboardSection, vm.SelectedSection);

                    window.Close();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void LogViewerWindow_STAThread_InstantiatesWindowAndLoadsLog()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var window = new LogViewerWindow();
                    Assert.NotNull(window.DataContext);
                    Assert.IsType<LogViewerWindow>(window.DataContext);

                    Assert.NotNull(window.LogText);

                    // Test Refresh action
                    window.LoadLog();
                    Assert.NotNull(window.LogText);

                    window.Close();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void ClipboardSectionViewModel_Dispose_DisposesReactiveSubscription()
        {
            var clip = new ClipboardSectionViewModel();
            Assert.NotNull(clip.Formats);
            clip.Dispose();
            // Should be idempotent
            clip.Dispose();
        }

        [Fact]
        public void ClipboardSectionViewModel_BackgroundThreadInvocation_DoesNotThrowCollectionViewException()
        {
            Exception backgroundEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var clip = new ClipboardSectionViewModel();

                    // Invoke from a different background thread to simulate ClipboardNative.OnUpdate
                    var worker = new Thread(() =>
                    {
                        try
                        {
                            clip.QueryClipboardFormats();
                            clip.CheckClipboardStatus(logToMonitor: true);
                        }
                        catch (Exception ex)
                        {
                            backgroundEx = ex;
                        }
                    });
                    worker.Start();
                    worker.Join(2000);

                    clip.Dispose();
                }
                catch (Exception ex)
                {
                    backgroundEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(backgroundEx);
        }

        [Fact]
        public void ClipboardSectionViewModel_BitmapOnClipboard_EnumeratesFormatsSafelyWithoutCrash()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using (var bmp = new System.Drawing.Bitmap(32, 32))
                    {
                        System.Windows.Forms.Clipboard.SetImage(bmp);
                    }

                    var clip = new ClipboardSectionViewModel();
                    clip.QueryClipboardFormats();

                    Assert.NotNull(clip.Formats);
                    Assert.Contains(clip.Formats, f => f.Name.Contains("BITMAP") || f.Name.Contains("Bitmap") || f.Name.Contains("DIB"));

                    clip.CheckClipboardStatus(logToMonitor: true);
                    Assert.False(clip.IsBlocked);

                    clip.Dispose();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void LogViewerWindow_ChunkedLoading_LoadsTailTrimsFirstLineAndLoadsEarlierChunks()
        {
            string tempLog = Path.Combine(Path.GetTempPath(), $"greenshot_chunk_test_{Guid.NewGuid():N}.log");
            try
            {
                // Write 1000 lines of known structured text (~100 KB)
                using (var writer = new StreamWriter(tempLog, false, System.Text.Encoding.UTF8))
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        writer.WriteLine($"[LINE_{i:D4}] 2026-09-25 09:00:00,000 INFO - Log line number {i:D4} test message padding for length.");
                    }
                }

                Exception threadEx = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        var window = new LogViewerWindow(tempLog);
                        // Load a small initial chunk (e.g. 10 KB)
                        window.LoadLog(initialChunkBytes: 10 * 1024);

                        Assert.True(window.CanLoadMore);
                        Assert.Contains("[LINE_0999]", window.LogText);
                        Assert.DoesNotContain("[LINE_0000]", window.LogText);

                        // Ensure first line in LogText starts cleanly with [LINE_
                        string trimmed = window.LogText.TrimStart();
                        Assert.StartsWith("[LINE_", trimmed);

                        // Load earlier lines (+10 KB)
                        window.LoadEarlierLines(chunkBytes: 10 * 1024);
                        Assert.True(window.CanLoadMore);
                        Assert.Contains("[LINE_0999]", window.LogText);

                        // Load entire log
                        window.LoadEntireLog();
                        Assert.False(window.CanLoadMore);
                        Assert.Contains("[LINE_0000]", window.LogText);
                        Assert.Contains("[LINE_0999]", window.LogText);

                        window.Close();
                    }
                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                Assert.Null(threadEx);
            }
            finally
            {
                if (File.Exists(tempLog))
                {
                    try { File.Delete(tempLog); } catch { }
                }
            }
        }

        [Fact]
        public void SelfService_LanguageChanged_UpdatesViewModels()
        {
            var vm = new SelfServiceViewModel();
            Assert.NotNull(vm.WindowTitle);

            // Verify sections have localized titles
            Assert.False(string.IsNullOrEmpty(vm.SystemInfoSection.Title));
            Assert.False(string.IsNullOrEmpty(vm.FileInfoSection.Title));
            Assert.False(string.IsNullOrEmpty(vm.ClipboardSection.Title));
            Assert.False(string.IsNullOrEmpty(vm.HotkeySection.Title));
            Assert.False(string.IsNullOrEmpty(vm.ChecksumSection.Title));

            // Trigger language update notification
            vm.SystemInfoSection.OnLanguageChanged();
            vm.FileInfoSection.OnLanguageChanged();
            vm.ClipboardSection.OnLanguageChanged();
            vm.HotkeySection.OnLanguageChanged();
            vm.ChecksumSection.OnLanguageChanged();

            Assert.False(string.IsNullOrEmpty(vm.SystemInfoSection.Title));
            Assert.False(string.IsNullOrEmpty(vm.FileInfoSection.Title));
            Assert.False(string.IsNullOrEmpty(vm.ClipboardSection.Title));
            Assert.False(string.IsNullOrEmpty(vm.HotkeySection.Title));
            Assert.False(string.IsNullOrEmpty(vm.ChecksumSection.Title));
        }

        [Fact]
        public void ChecksumSectionViewModel_TryParseChecksumLine_ValidatesAndParsesLineFormats()
        {
            // Standard space-separated format
            bool success = ChecksumSectionViewModel.TryParseChecksumLine(
                "B16F15C685193C406266C53E2C73D93B2CEDC3219550931AD8D81EA0FD6E3F60  Dapplo.HttpExtensions.dll",
                out string hash, out string path);
            Assert.True(success);
            Assert.Equal("B16F15C685193C406266C53E2C73D93B2CEDC3219550931AD8D81EA0FD6E3F60", hash);
            Assert.Equal("Dapplo.HttpExtensions.dll", path);

            // Forward slash path with asterisk (binary mode indicator)
            success = ChecksumSectionViewModel.TryParseChecksumLine(
                "5B3670C223E8453BE5F5917AB48A36E084C5421E14A96F927B3225AF29F7754A *Plugins/Greenshot.Plugin.Box/Dapplo.Windows.User32.dll",
                out hash, out path);
            Assert.True(success);
            Assert.Equal("5B3670C223E8453BE5F5917AB48A36E084C5421E14A96F927B3225AF29F7754A", hash);
            Assert.Equal(@"Plugins\Greenshot.Plugin.Box\Dapplo.Windows.User32.dll", path);

            // Comments and empty lines
            Assert.False(ChecksumSectionViewModel.TryParseChecksumLine("# Comment line", out _, out _));
            Assert.False(ChecksumSectionViewModel.TryParseChecksumLine("; Another comment", out _, out _));
            Assert.False(ChecksumSectionViewModel.TryParseChecksumLine("", out _, out _));
            Assert.False(ChecksumSectionViewModel.TryParseChecksumLine("   ", out _, out _));

            // Malformed hex or too short
            Assert.False(ChecksumSectionViewModel.TryParseChecksumLine("NotHexAtAll", out _, out _));
            Assert.False(ChecksumSectionViewModel.TryParseChecksumLine("B16F15C685193C406266C53E2C73D93B2CEDC3219550931AD8D81EA0FD6E3F6G  InvalidHexChar.dll", out _, out _));
        }

        [Fact]
        public void ChecksumSectionViewModel_ComputeFileSha256_CalculatesCorrectHash()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"sha_test_{Guid.NewGuid():N}.txt");
            try
            {
                byte[] content = System.Text.Encoding.UTF8.GetBytes("GreenshotChecksumValidationTestContent12345");
                File.WriteAllBytes(tempFile, content);

                string computed = ChecksumSectionViewModel.ComputeFileSha256(tempFile);
                Assert.NotNull(computed);
                Assert.Equal(64, computed.Length);

                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    string expected = BitConverter.ToString(sha.ComputeHash(content)).Replace("-", "").ToUpperInvariant();
                    Assert.Equal(expected, computed);
                }
            }
            finally
            {
                if (File.Exists(tempFile)) try { File.Delete(tempFile); } catch { }
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task ChecksumSectionViewModel_ValidateChecksums_IdentifiesMatchesMismatchesMissingAndUnlisted()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"greenshot_integrity_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create valid file
                string validFilePath = Path.Combine(tempDir, "valid.dll");
                File.WriteAllText(validFilePath, "valid content");
                string validHash = ChecksumSectionViewModel.ComputeFileSha256(validFilePath);

                // Create corrupted file (content differs from hash recorded in checksum file)
                string corruptedFilePath = Path.Combine(tempDir, "corrupted.dll");
                File.WriteAllText(corruptedFilePath, "corrupted content on disk");
                string expectedCorruptHash = "1111222233334444555566667777888899990000AAAABBBBCCCCDDDDEEEEFFFF";

                // Create unlisted file (exists on disk but omitted from checksum file)
                string unlistedFilePath = Path.Combine(tempDir, "unlisted.dll");
                File.WriteAllText(unlistedFilePath, "unlisted file content");

                // Missing file is only recorded in manifest, not on disk
                string missingHash = "9999888877776666555544443333222211110000AAAABBBBCCCCDDDDEEEEFFFF";

                // Write checksum.SHA256
                string manifestPath = Path.Combine(tempDir, "checksum.SHA256");
                var manifestLines = new[]
                {
                    $"{validHash}  valid.dll",
                    $"{expectedCorruptHash}  corrupted.dll",
                    $"{missingHash}  missing.dll"
                };
                File.WriteAllLines(manifestPath, manifestLines);

                var vm = new ChecksumSectionViewModel
                {
                    BaseDirectory = tempDir,
                    ChecksumFilePath = manifestPath
                };

                await vm.ValidateChecksumsAsync();

                Assert.Equal(4, vm.TotalCount);
                Assert.Equal(1, vm.MatchedCount);
                Assert.Equal(1, vm.MismatchedCount);
                Assert.Equal(1, vm.MissingCount);
                Assert.Equal(1, vm.UnlistedCount);

                Assert.True(vm.HasCorruptFiles);
                Assert.True(vm.HasMissingFiles);
                Assert.True(vm.HasIssues);
                Assert.Equal(2, vm.IssuesCount);

                // Check item statuses
                var validItem = System.Linq.Enumerable.FirstOrDefault(vm.AllItems, i => i.RelativePath == "valid.dll");
                Assert.NotNull(validItem);
                Assert.Equal(ChecksumStatus.Match, validItem.Status);
                Assert.Equal(validHash, validItem.ActualHash);
                Assert.Equal(validHash, validItem.ExpectedHash);
                Assert.False(validItem.IsIssue);

                var corruptItem = System.Linq.Enumerable.FirstOrDefault(vm.AllItems, i => i.RelativePath == "corrupted.dll");
                Assert.NotNull(corruptItem);
                Assert.Equal(ChecksumStatus.Mismatch, corruptItem.Status);
                Assert.NotEqual(corruptItem.ActualHash, corruptItem.ExpectedHash);
                Assert.True(corruptItem.IsIssue);

                var missingItem = System.Linq.Enumerable.FirstOrDefault(vm.AllItems, i => i.RelativePath == "missing.dll");
                Assert.NotNull(missingItem);
                Assert.Equal(ChecksumStatus.Missing, missingItem.Status);
                Assert.Equal("-", missingItem.ActualHash);
                Assert.True(missingItem.IsIssue);

                var unlistedItem = System.Linq.Enumerable.FirstOrDefault(vm.AllItems, i => i.RelativePath == "unlisted.dll");
                Assert.NotNull(unlistedItem);
                Assert.Equal(ChecksumStatus.Unlisted, unlistedItem.Status);
                Assert.Equal("-", unlistedItem.ExpectedHash);

                // Test Filtering
                vm.ActiveFilter = "Issues";
                Assert.Equal(2, vm.FilteredItems.Count);

                vm.ActiveFilter = "Mismatch";
                Assert.Single(vm.FilteredItems);
                Assert.Equal("corrupted.dll", vm.FilteredItems[0].RelativePath);

                vm.ActiveFilter = "Missing";
                Assert.Single(vm.FilteredItems);
                Assert.Equal("missing.dll", vm.FilteredItems[0].RelativePath);

                vm.ActiveFilter = "Unlisted";
                Assert.Single(vm.FilteredItems);
                Assert.Equal("unlisted.dll", vm.FilteredItems[0].RelativePath);

                vm.ActiveFilter = "Match";
                Assert.Single(vm.FilteredItems);
                Assert.Equal("valid.dll", vm.FilteredItems[0].RelativePath);

                // Test Search
                vm.ActiveFilter = "All";
                vm.SearchQuery = "corrupted";
                Assert.Single(vm.FilteredItems);
                Assert.Equal("corrupted.dll", vm.FilteredItems[0].RelativePath);

                // Test Copy Report (should not throw)
                vm.CopyReportToClipboard();
                Assert.NotNull(vm.StatusMessage);

                // Test Copy Selected Details (should not throw)
                vm.SelectedItem = corruptItem;
                vm.CopySelectedDetails();
                Assert.NotNull(vm.StatusMessage);

                vm.Cleanup();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, recursive: true); } catch { }
                }
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task ChecksumSectionViewModel_EmptyOrMissingManifest_ReportsFileNotFoundBanner()
        {
            string nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_manifest_{Guid.NewGuid():N}.SHA256");
            var vm = new ChecksumSectionViewModel
            {
                ChecksumFilePath = nonExistentPath
            };

            await vm.ValidateChecksumsAsync();

            Assert.True(vm.ChecksumFileNotFound);
            Assert.Contains("not found", vm.BannerTitle, StringComparison.OrdinalIgnoreCase);

            vm.Cleanup();
        }

        [Fact]
        public async System.Threading.Tasks.Task ChecksumSectionViewModel_RealChecksumManifest_ExecutesWithoutError()
        {
            string binReleaseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Greenshot\bin\Release\net480"));
            string manifestPath = Path.Combine(binReleaseDir, "checksum.SHA256");

            if (File.Exists(manifestPath))
            {
                var vm = new ChecksumSectionViewModel
                {
                    BaseDirectory = binReleaseDir,
                    ChecksumFilePath = manifestPath
                };

                await vm.ValidateChecksumsAsync();

                Assert.False(vm.ChecksumFileNotFound);
                Assert.True(vm.TotalCount > 0);
                Assert.NotNull(vm.BannerTitle);
                Assert.NotEmpty(vm.AllItems);

                // Verify unlisted files are identified (e.g. Greenshot.exe which is omitted from checksum.SHA256)
                if (File.Exists(Path.Combine(binReleaseDir, "Greenshot.exe")))
                {
                    Assert.Contains(vm.AllItems, i => i.RelativePath == "Greenshot.exe" && i.Status == ChecksumStatus.Unlisted);
                }

                vm.Cleanup();
            }
        }

        [Fact]
        public void SelfService_LanguageResources_GermanCoversAllSelfServiceKeys()
        {
            string enPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Greenshot\Languages\language-en-US.xml");
            string dePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Greenshot\Languages\language-de-DE.xml");

            if (!File.Exists(enPath)) enPath = Path.GetFullPath(@"src\Greenshot\Languages\language-en-US.xml");
            if (!File.Exists(dePath)) dePath = Path.GetFullPath(@"src\Greenshot\Languages\language-de-DE.xml");

            if (File.Exists(enPath) && File.Exists(dePath))
            {
                var enDoc = new System.Xml.XmlDocument();
                enDoc.Load(enPath);

                var deDoc = new System.Xml.XmlDocument();
                deDoc.Load(dePath);

                var enKeys = new System.Collections.Generic.HashSet<string>();
                foreach (System.Xml.XmlNode node in enDoc.SelectNodes("//resource"))
                {
                    string name = node.Attributes?["name"]?.Value;
                    if (name != null && name.StartsWith("selfservice_"))
                    {
                        enKeys.Add(name);
                    }
                }

                var deKeys = new System.Collections.Generic.HashSet<string>();
                foreach (System.Xml.XmlNode node in deDoc.SelectNodes("//resource"))
                {
                    string name = node.Attributes?["name"]?.Value;
                    if (name != null && name.StartsWith("selfservice_"))
                    {
                        deKeys.Add(name);
                    }
                }

                Assert.NotEmpty(enKeys);
                foreach (var key in enKeys)
                {
                    Assert.True(deKeys.Contains(key), $"German language file is missing selfservice key: {key}");
                }
            }
        }

        [Fact]
        public void ChecksumSectionViewModel_IsLanguageXmlFile_IdentifiesLanguageXmlCorrectly()
        {
            Assert.True(ChecksumSectionViewModel.CanSkipFile("language-en-US.xml"));
            Assert.True(ChecksumSectionViewModel.CanSkipFile("language-de-DE.xml"));
            Assert.True(ChecksumSectionViewModel.CanSkipFile(@"Languages\language-fr-FR.xml"));
            Assert.True(ChecksumSectionViewModel.CanSkipFile(@"Plugins\Office\Languages\language_office-de-DE.xml"));
            Assert.True(ChecksumSectionViewModel.CanSkipFile(@"Languages\custom.xml"));

            Assert.False(ChecksumSectionViewModel.CanSkipFile("Greenshot.exe"));
            Assert.False(ChecksumSectionViewModel.CanSkipFile("log4net-release.xml"));
            Assert.False(ChecksumSectionViewModel.CanSkipFile("checksum.SHA256"));
            Assert.False(ChecksumSectionViewModel.CanSkipFile("Greenshot.exe.config"));
            Assert.False(ChecksumSectionViewModel.CanSkipFile(""));
            Assert.False(ChecksumSectionViewModel.CanSkipFile(null));
        }

        [Fact]
        public async System.Threading.Tasks.Task ChecksumSectionViewModel_IgnoresLanguageXmlFiles_ForBothManifestAndDisk()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"greenshot_lang_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a normal valid file
                string validFilePath = Path.Combine(tempDir, "valid.dll");
                File.WriteAllText(validFilePath, "valid content");
                string validHash = ChecksumSectionViewModel.ComputeFileSha256(validFilePath);

                // Create a language file on disk (should NOT be reported as unlisted!)
                string langDir = Path.Combine(tempDir, "Languages");
                Directory.CreateDirectory(langDir);
                string langFilePath = Path.Combine(langDir, "language-custom.xml");
                File.WriteAllText(langFilePath, "<language>custom</language>");

                // Write checksum manifest containing valid.dll and also an entry for a language file
                string manifestPath = Path.Combine(tempDir, "checksum.SHA256");
                var manifestLines = new[]
                {
                    $"{validHash} *valid.dll",
                    $"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA *Languages\\language-en-US.xml"
                };
                File.WriteAllLines(manifestPath, manifestLines);

                var vm = new ChecksumSectionViewModel
                {
                    BaseDirectory = tempDir,
                    ChecksumFilePath = manifestPath
                };

                await vm.ValidateChecksumsAsync();

                // TotalCount should be 1 (only valid.dll, the language entry in manifest and on disk must be completely ignored)
                Assert.Equal(1, vm.TotalCount);
                Assert.Equal(1, vm.MatchedCount);
                Assert.Equal(0, vm.MissingCount);
                Assert.Equal(0, vm.UnlistedCount);
                Assert.Equal(0, vm.MismatchedCount);

                // The language files should not exist in AllItems
                Assert.DoesNotContain(vm.AllItems, i => i.RelativePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

                vm.Cleanup();
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
