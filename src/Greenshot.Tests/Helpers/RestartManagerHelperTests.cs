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
using Greenshot.Helpers;
using Xunit;

namespace Greenshot.Tests.Helpers
{
    public class RestartManagerHelperTests
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(string commandLine, out int argumentCount);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        /// <summary>
        /// Splits the arguments the way Windows does for the restarted process.
        /// The executable name follows different rules, so a dummy one is put in front and left out of the result.
        /// </summary>
        private static string[] SplitArguments(string arguments)
        {
            IntPtr argv = CommandLineToArgvW("Greenshot.exe " + arguments, out int count);
            Assert.NotEqual(IntPtr.Zero, argv);
            try
            {
                var result = new string[count - 1];
                for (int i = 1; i < count; i++)
                {
                    result[i - 1] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(argv, i * IntPtr.Size));
                }

                return result;
            }
            finally
            {
                LocalFree(argv);
            }
        }

        [Fact]
        public void CreateRestartArguments_WithoutIniDirectory_OnlyRestores()
        {
            CommandLineOptions options = GreenshotCommandLine.Parse(SplitArguments(RestartManagerHelper.CreateRestartArguments(null)));

            Assert.NotNull(options);
            Assert.True(options.Restore);
            Assert.Null(options.IniDirectory);
        }

        [Theory]
        [InlineData(@"C:\Users\Some User\Greenshot Settings")]
        [InlineData(@"D:\")]
        [InlineData(@"\\server\share\greenshot\")]
        public void CreateRestartArguments_WithIniDirectory_RestartsWithTheSameDirectory(string iniDirectory)
        {
            CommandLineOptions options = GreenshotCommandLine.Parse(SplitArguments(RestartManagerHelper.CreateRestartArguments(iniDirectory)));

            Assert.NotNull(options);
            Assert.True(options.Restore);
            Assert.Equal(iniDirectory, options.IniDirectory);
        }
    }
}
