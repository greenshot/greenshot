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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Greenshot.Base.Core;

namespace Greenshot.UI.SelfService
{
    public class SystemInfoSectionViewModel : SelfServiceSectionViewModel
    {
        public override string Id => "system";
        public override string Title
        {
            get
            {
                return Language.GetString("selfservice_category_system");
            }
        }

        public override string Subtitle
        {
            get
            {
                return Language.GetString("selfservice_category_system_sub");
            }
        }
        public override string Icon => "💻";

        // Memory & Process metrics
        private string _workingSetText;
        private string _peakWorkingSetText;
        private string _privateMemoryText;
        private string _gcMemoryText;
        private string _systemMemoryText;
        private string _memoryLoadText;
        private string _processStatsText;
        private string _uptimeText;
        private string _environmentReport;
        private string _statusMessage;

        public string WorkingSetText { get => _workingSetText; private set { _workingSetText = value; OnPropertyChanged(); } }
        public string PeakWorkingSetText { get => _peakWorkingSetText; private set { _peakWorkingSetText = value; OnPropertyChanged(); } }
        public string PrivateMemoryText { get => _privateMemoryText; private set { _privateMemoryText = value; OnPropertyChanged(); } }
        public string GcMemoryText { get => _gcMemoryText; private set { _gcMemoryText = value; OnPropertyChanged(); } }
        public string SystemMemoryText { get => _systemMemoryText; private set { _systemMemoryText = value; OnPropertyChanged(); } }
        public string MemoryLoadText { get => _memoryLoadText; private set { _memoryLoadText = value; OnPropertyChanged(); } }
        public string ProcessStatsText { get => _processStatsText; private set { _processStatsText = value; OnPropertyChanged(); } }
        public string UptimeText { get => _uptimeText; private set { _uptimeText = value; OnPropertyChanged(); } }
        public string EnvironmentReport { get => _environmentReport; private set { _environmentReport = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        public SystemInfoSectionViewModel()
        {
            Refresh();
        }

        public override void OnNavigatedTo()
        {
            Refresh();
        }

        public override void Refresh()
        {
            try
            {
                using var proc = Process.GetCurrentProcess();
                proc.Refresh();

                long ws = proc.WorkingSet64;
                long peakWs = proc.PeakWorkingSet64;
                long priv = proc.PrivateMemorySize64;
                long gcMem = GC.GetTotalMemory(false);

                WorkingSetText = FormatBytes(ws);
                PeakWorkingSetText = FormatBytes(peakWs);
                PrivateMemoryText = FormatBytes(priv);
                GcMemoryText = FormatBytes(gcMem);

                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    SystemMemoryText = $"{availGb:F1} GB free of {totalGb:F1} GB";
                    MemoryLoadText = $"{memStatus.dwMemoryLoad}% in use";
                }
                else
                {
                    SystemMemoryText = "Unknown";
                    MemoryLoadText = "N/A";
                }

                uint gdiObjects = 0;
                uint userObjects = 0;
                try
                {
                    gdiObjects = GetGuiResources(proc.Handle, 0);
                    userObjects = GetGuiResources(proc.Handle, 1);
                }
                catch
                {
                    // Ignore
                }

                int threads = proc.Threads.Count;
                int handles = proc.HandleCount;
                ProcessStatsText = $"PID: {proc.Id} | Threads: {threads} | Handles: {handles} | GDI: {gdiObjects} | USER: {userObjects}";

                TimeSpan uptime = DateTime.Now - proc.StartTime;
                UptimeText = $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s";

                // Build complete report combining environment info + memory metrics
                var sb = new StringBuilder();
                sb.AppendLine("=== Greenshot System & Memory Diagnostics ===");
                sb.AppendLine(EnvironmentInfo.EnvironmentToString(true));
                sb.AppendLine();
                sb.AppendLine("=== Memory Information ===");
                sb.AppendLine($"Process Working Set:       {WorkingSetText} (Peak: {PeakWorkingSetText})");
                sb.AppendLine($"Process Private Memory:    {PrivateMemoryText}");
                sb.AppendLine($"GC Managed Heap:           {GcMemoryText}");
                sb.AppendLine($"System Physical Memory:    {SystemMemoryText} (Load: {MemoryLoadText})");
                sb.AppendLine();
                sb.AppendLine("=== Process Diagnostics ===");
                sb.AppendLine(ProcessStatsText);
                sb.AppendLine($"Process Uptime:            {UptimeText}");

                EnvironmentReport = sb.ToString().TrimEnd();
                StatusMessage = $"Updated at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                EnvironmentReport = $"Error gathering system information:\n{ex}";
                StatusMessage = "Failed to refresh metrics";
            }
        }

        public void CopyReportToClipboard()
        {
            try
            {
                Clipboard.SetText(EnvironmentReport ?? string.Empty);
                StatusMessage = Language.GetString("selfservice_sysinfo_copied");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Copy failed: {ex.Message}";
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
            {
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            }
            if (bytes >= 1024)
            {
                return $"{bytes / 1024.0:F1} KB";
            }
            return $"{bytes} B";
        }
    }
}
