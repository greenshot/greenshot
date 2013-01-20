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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Forms;
using Greenshot.Helpers;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using NUnit.Framework;

namespace Greenshot.Test
{
	[TestFixture]
	public class ScaleHelperTest {
		
		[Test]
		public void FreeScaleTest() {
			RectangleF r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_BOTTOM_RIGHT, new PointF(10,20));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
			Assert.AreEqual(10, r.Width);
			Assert.AreEqual(20, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_BOTTOM_RIGHT, new PointF(30,40));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(30, r.Width);
			Assert.AreEqual(40, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_BOTTOM_CENTER, new PointF(9999,40));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(10, r.Width);
			Assert.AreEqual(40, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_BOTTOM_LEFT, new PointF(3,40));
			Assert.AreEqual(3, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(7, r.Width);
			Assert.AreEqual(40, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_MIDDLE_RIGHT, new PointF(48,9999));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(48, r.Width);
			Assert.AreEqual(20, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_MIDDLE_RIGHT, new PointF(8,9999));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(8, r.Width);
			Assert.AreEqual(20, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_TOP_RIGHT, new PointF(17,-37));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(-37, r.Top);
		    Assert.AreEqual(17, r.Width);
			Assert.AreEqual(57, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_TOP_CENTER, new PointF(9998,-77));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(-77, r.Top);
		    Assert.AreEqual(10, r.Width);
			Assert.AreEqual(97, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.Scale(ref r, Gripper.POSITION_TOP_LEFT, new PointF(-23,-54));
			Assert.AreEqual(-23, r.Left);
			Assert.AreEqual(-54, r.Top);
		    Assert.AreEqual(33, r.Width);
			Assert.AreEqual(74, r.Height);
		}
		
		[Test]
		public void RationalScaleTest() {
			RectangleF r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_BOTTOM_RIGHT, new PointF(10,20));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
			Assert.AreEqual(10, r.Width);
			Assert.AreEqual(20, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_BOTTOM_RIGHT, new PointF(30,60));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(30, r.Width);
			Assert.AreEqual(60, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_BOTTOM_RIGHT, new PointF(30,96768));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(30, r.Width);
			Assert.AreEqual(60, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_BOTTOM_CENTER, new PointF(9999,40));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(10, r.Width);
			Assert.AreEqual(40, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_BOTTOM_LEFT, new PointF(-90, 9234));
			Assert.AreEqual(-90, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(100, r.Width);
			Assert.AreEqual(200, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_MIDDLE_RIGHT, new PointF(48,9999));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(48, r.Width);
			Assert.AreEqual(20, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_MIDDLE_RIGHT, new PointF(8,9999));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(8, r.Width);
			Assert.AreEqual(20, r.Height);
			
			r = new RectangleF(0,20,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_TOP_RIGHT, new PointF(20,829634235));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(20, r.Width);
			Assert.AreEqual(40, r.Height);
			
			r = new RectangleF(0,0,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_TOP_CENTER, new PointF(9998,-77));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(-77, r.Top);
		    Assert.AreEqual(10, r.Width);
			Assert.AreEqual(97, r.Height);
			
			r = new RectangleF(10,20,10,20);
			ScaleHelper.RationalScale(ref r, Gripper.POSITION_TOP_LEFT, new PointF(0,0));
			Assert.AreEqual(0, r.Left);
			Assert.AreEqual(0, r.Top);
		    Assert.AreEqual(20, r.Width);
			Assert.AreEqual(40, r.Height);
		}
		
		/*[Test]
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
			
		}*/
	}
}
