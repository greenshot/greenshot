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
using System.Diagnostics;
using System.IO;
using Greenshot.Base.Native;
using Xunit;

namespace Greenshot.Tests.Native
{
    /// <summary>
    /// ProcessHelper.IsRunning finds processes by image name without building Process objects
    /// </summary>
    public class ProcessHelperTests
    {
        [Fact]
        public void IsRunning_FindsTheTestProcess_IgnoringCase()
        {
            using var current = Process.GetCurrentProcess();
            string imageName = Path.GetFileName(current.MainModule.FileName);

            Assert.True(ProcessHelper.IsRunning(imageName));
            Assert.True(ProcessHelper.IsRunning(imageName.ToUpperInvariant()));
            Assert.True(ProcessHelper.IsRunning(imageName.ToLowerInvariant()));
        }

        [Fact]
        public void IsRunning_MatchesWhatGetProcessesByNameSees()
        {
            foreach (var name in new[] { "svchost", "explorer", "csrss" })
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var process in processes)
                {
                    process.Dispose();
                }

                Assert.Equal(processes.Length > 0, ProcessHelper.IsRunning(name + ".exe"));
            }
        }

        [Fact]
        public void IsRunning_UnknownOrEmptyName_IsFalse()
        {
            Assert.False(ProcessHelper.IsRunning(Guid.NewGuid().ToString("N") + ".exe"));
            Assert.False(ProcessHelper.IsRunning(""));
            Assert.False(ProcessHelper.IsRunning(null));
            // Without the extension it's not the image name
            using var current = Process.GetCurrentProcess();
            Assert.False(ProcessHelper.IsRunning(current.ProcessName));
        }
    }
}
