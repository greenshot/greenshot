/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Forms;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using NUnit.Framework;

namespace Greenshot.Test
{
	[TestFixture]
	public class SaveImageFileDialogTest {
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		[Test]
		public void GetSetExtensionTest() {
			SaveImageFileDialog sifd = new SaveImageFileDialog();
			sifd.Extension = "jpg";
			Assert.AreEqual(sifd.Extension, "jpg");
			sifd.Extension = "gif";
			Assert.AreEqual(sifd.Extension, "gif");
			sifd.Extension = "png";
			Assert.AreEqual(sifd.Extension, "png");
			sifd.Extension = "bmp";
			Assert.AreEqual(sifd.Extension, "bmp");
		}
		
		[Test]
		public void GetFileNameWithExtensionTest() {
			SaveImageFileDialog sifd = new SaveImageFileDialog();
			
			sifd.InitialDirectory = @"C:\some\path";
			sifd.FileName = "myimage.jpg";
			sifd.Extension = "jpg";
			Assert.AreEqual("myimage.jpg",sifd.FileNameWithExtension);
			
			sifd.Extension = "gif";
			Assert.AreEqual("myimage.jpg.gif",sifd.FileNameWithExtension);
			
			sifd.FileName = "myimage";
			Assert.AreEqual("myimage.gif",sifd.FileNameWithExtension);
			
		}
		
		[Test]
		public void SuggestBasicFileNameTest() {
			//conf.Output_FileAs_Fullpath = @"c:\path\to\greenshot_testdir\gstest_28.jpg";
			conf.OutputFilePath = @"c:\path\to\greenshot_testdir\";
			conf.OutputFileFilenamePattern = "gstest_${NUM}";
			conf.OutputFileFormat = OutputFormat.png;
			conf.OutputFileIncrementingNumber = 28;
			SaveImageFileDialog sifd = new SaveImageFileDialog();
			
			Assert.AreEqual(sifd.InitialDirectory, @"c:\path\to\greenshot_testdir");
			Assert.AreEqual(sifd.FileNameWithExtension, "gstest_000028.jpg");
			
		}
	}
}
