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
            Assert.Equal(4, vm.Sections.Count);

            Assert.NotNull(vm.SystemInfoSection);
            Assert.NotNull(vm.FileInfoSection);
            Assert.NotNull(vm.ClipboardSection);
            Assert.NotNull(vm.HotkeySection);

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

            Assert.Contains(hotkeys.Hotkeys, h => h.ActionName == "Capture Region");
            Assert.Contains(hotkeys.Hotkeys, h => h.ActionName == "Capture Window");
            Assert.Contains(hotkeys.Hotkeys, h => h.ActionName == "Capture Fullscreen");
            Assert.Contains(hotkeys.Hotkeys, h => h.ActionName == "Capture Last Region");
            Assert.Contains(hotkeys.Hotkeys, h => h.ActionName == "Capture Clipboard");

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
    }
}
