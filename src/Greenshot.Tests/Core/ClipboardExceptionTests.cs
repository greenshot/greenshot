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
using System.Runtime.InteropServices;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class ClipboardExceptionTests
    {
        public ClipboardExceptionTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void ClipboardException_WithoutOwner_FormatsCorrectMessage()
        {
            var inner = new ExternalException("Cannot open clipboard", unchecked((int)0x800401D0));
            var ex = new ClipboardException("Cannot open clipboard", inner);

            Assert.Equal("Cannot open clipboard", ex.Message);
            Assert.Null(ex.OwnerProcess);
            Assert.Equal(unchecked((int)0x800401D0), ex.ErrorCode);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void ClipboardException_WithOwner_FormatsDetailedMessage()
        {
            var inner = new ExternalException("Cannot open clipboard", unchecked((int)0x800401D0));
            var ex = new ClipboardException("Cannot open clipboard", "notepad.exe", inner);

            Assert.Contains("notepad.exe", ex.Message);
            Assert.Equal("notepad.exe", ex.OwnerProcess);
            Assert.Equal(unchecked((int)0x800401D0), ex.ErrorCode);
        }

        [Fact]
        public void DestinationExportException_StoresDestinationAndMessage()
        {
            var inner = new InvalidOperationException("Failed to write");
            var ex = new DestinationExportException("Failed to copy to clipboard", "Clipboard", inner);

            Assert.Equal("Clipboard", ex.FailedDestination);
            Assert.Equal("Failed to copy to clipboard", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void TrySetClipboardData_WithNullSurface_ReturnsFalseWithoutThrowing()
        {
            // Null surface should safely return false without throwing
            bool result = ClipboardHelper.TrySetClipboardData((ISurface)null, out string errorMessage);
            Assert.False(result);
            Assert.NotNull(errorMessage);
        }
    }
}
