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
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class CoreConfigurationPathNormalizationTests
    {
        public CoreConfigurationPathNormalizationTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(@"C:\Users\user\Desktop", @"C:\Users\user\Desktop")]
        [InlineData(@"C:\\Users\\user\\Desktop", @"C:\Users\user\Desktop")]
        [InlineData(@"C:\\\\\\Users\\\\\\user\\\\\\Desktop", @"C:\Users\user\Desktop")]
        [InlineData(@"C:/Users/user/Desktop", @"C:\Users\user\Desktop")]
        [InlineData(@"C:\Screenshots\", @"C:\Screenshots")]
        [InlineData(@"C:\", @"C:\")]
        [InlineData(@"\\server\share\folder", @"\\server\share\folder")]
        [InlineData(@"\\\\\\\\server\\\\\\\\share\\\\\\\\folder", @"\\server\share\folder")]
        [InlineData(@"\\\\server\\share\\", @"\\server\share")]
        [InlineData(@"C:\Screenshots\${capturetime:d""yyyy-MM-dd""}\", @"C:\Screenshots\${capturetime:d""yyyy-MM-dd""}")]
        [InlineData(@"   C:\Users\user\Desktop   ", @"C:\Users\user\Desktop")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void NormalizePath_VariousFormats_NormalizedCorrectly(string input, string expected)
        {
            string actual = CoreConfigurationImpl.NormalizePath(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NormalizePath_Excessive128Backslashes_NormalizesCleanly()
        {
            string excessiveSlashes = new string('\\', 128);
            string input = $"C:{excessiveSlashes}Users{excessiveSlashes}user{excessiveSlashes}OneDrive{excessiveSlashes}Desktop";

            string actual = CoreConfigurationImpl.NormalizePath(input);

            Assert.Equal(@"C:\Users\user\OneDrive\Desktop", actual);
        }

        [Fact]
        public void OutputFilePath_PropertySet_NormalizesAutomatically()
        {
            var config = new CoreConfigurationImpl();
            config.OutputFilePath = @"C:\\\\\\Users\\\\\\user\\\\\\Screenshots";

            Assert.Equal(@"C:\Users\user\Screenshots", config.OutputFilePath);
        }

        [Fact]
        public void OutputFileAsFullpath_PropertySet_NormalizesAutomatically()
        {
            var config = new CoreConfigurationImpl();
            config.OutputFileAsFullpath = @"D:\\\\\\Captures\\\\\\test.png";

            Assert.Equal(@"D:\Captures\test.png", config.OutputFileAsFullpath);
        }

        [Fact]
        public void OnAfterLoad_HealsExistingExcessiveSlashes()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            string excessiveSlashes = new string('\\', 128);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string malformedDesktop = desktopPath.Replace(@"\", excessiveSlashes);
            string dummyFile = System.IO.Path.Combine(desktopPath, "dummy.png");
            string malformedFile = dummyFile.Replace(@"\", excessiveSlashes);

            // Simulate raw un-normalized values loaded from a legacy INI file
            config.SetRawValue(nameof(config.OutputFilePath), malformedDesktop);
            config.SetRawValue(nameof(config.OutputFileAsFullpath), malformedFile);

            config.OnAfterLoad();

            Assert.Equal(desktopPath, config.OutputFilePath);
            Assert.Equal(dummyFile, config.OutputFileAsFullpath);
        }
    }
}
