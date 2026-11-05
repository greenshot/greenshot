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
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class ImageIOTmpFilesTests
    {
        [Fact]
        public void RemoveExpiredTmpFiles_RemovesOnlyOldFiles()
        {
            string directory = ImageIO.GetTmpDirectory();
            string oldFile = Path.Combine(directory, $"test_old_{Guid.NewGuid():N}.png");
            string newFile = Path.Combine(directory, $"test_new_{Guid.NewGuid():N}.png");
            File.WriteAllText(oldFile, "old");
            File.WriteAllText(newFile, "new");
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-1));
            try
            {
                ImageIO.RemoveExpiredTmpFiles();

                Assert.False(File.Exists(oldFile));
                Assert.True(File.Exists(newFile));
            }
            finally
            {
                File.Delete(oldFile);
                File.Delete(newFile);
            }
        }
    }
}
