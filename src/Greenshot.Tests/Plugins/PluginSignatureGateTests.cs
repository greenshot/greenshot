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
using System.IO;
using Greenshot.Plugins;
using Xunit;

namespace Greenshot.Tests.Plugins
{
    public class PluginSignatureGateTests
    {
        [Fact]
        public void CanUserChange_FileInTempFolder_True()
        {
            var file = Path.Combine(Path.GetTempPath(), $"Greenshot.Plugin.Test{Guid.NewGuid():N}.dll");
            File.WriteAllBytes(file, new byte[] { 0 });
            try
            {
                Assert.True(PluginSignatureGate.CanUserChange(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void CanUserChange_FileInSystemFolder_False()
        {
            // Owned by TrustedInstaller, users and administrators without elevation can only read it
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "kernel32.dll");
            Assert.False(PluginSignatureGate.CanUserChange(file));
        }
    }
}
