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
using NUnit.Framework;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Greenshot.Drawing;
using Greenshot.Drawing.Filters;
using GreenshotPlugin.Core;

namespace Greenshot.Test.Drawing.Filters {
	/// <summary>
	/// Description of BitmapBufferTest.
	/// </summary>
	[TestFixture]
	public class BitmapBufferTest
	{
		private Bitmap bmp;
		private BitmapBuffer buf;
		private Bitmap bmpRect;
		private BitmapBuffer bufRect;
		
		
		
		[SetUp]
		public void Init()
		{
			bmp = new Bitmap(6,6);
			for(int i=0; i<6; i++) {
				int col = 255-i*51;
				bmp.SetPixel(0,i, Color.FromArgb(col,0,0));
				bmp.SetPixel(1,i, Color.FromArgb(col,col,0));
				bmp.SetPixel(2,i, Color.FromArgb(0,col,0));
				bmp.SetPixel(3,i, Color.FromArgb(0,col,col));
				bmp.SetPixel(4,i, Color.FromArgb(0,0,col));
				bmp.SetPixel(5,i, Color.FromArgb(col,0,col));
			}
			buf = new BitmapBuffer(bmp);
			
			bmpRect = new Bitmap(6,6);
			for(int i=0; i<6; i++) {
				int col = 255-i*51;
				bmpRect.SetPixel(0,i, Color.FromArgb(col,0,0));
				bmpRect.SetPixel(1,i, Color.FromArgb(col,col,0));
				bmpRect.SetPixel(2,i, Color.FromArgb(0,col,0));
				bmpRect.SetPixel(3,i, Color.FromArgb(0,col,col));
				bmpRect.SetPixel(4,i, Color.FromArgb(0,0,col));
				bmpRect.SetPixel(5,i, Color.FromArgb(col,0,col));
			}
			bufRect = new BitmapBuffer(bmpRect,new Rectangle(2,2,2,2));
		}
		
		
		
		[TearDown]
		public void Dispose()
		{
			buf.Dispose();
			buf = null;
			bmp.Dispose();
			bmp = null;
		}
		
		[Test]
		public void TestGetSetColor()	
		{
//			Assert.AreEqual(255, buf.R);
//			Assert.AreEqual(0, buf.G);
//			Assert.AreEqual(0, buf.B);
//			Assert.AreEqual(Color.FromArgb(255,0,0), buf.Color);
//			
//			buf.B = 255;
//			Assert.AreEqual(255, buf.R);
//			Assert.AreEqual(0, buf.G);
//			Assert.AreEqual(255, buf.B);
//			Assert.AreEqual(Color.FromArgb(255,0,255), buf.Color);
//			
//			buf.Color = Color.Red;
//			Assert.AreEqual(255, buf.R);
//			Assert.AreEqual(0, buf.G);
//			Assert.AreEqual(0, buf.B);
//			Assert.AreEqual(Color.FromArgb(255,0,0), buf.Color);
		}
		
		[Test]
		public void TestGetSetColorRect()	
		{
//			Assert.AreEqual(0, bufRect.R);
//			Assert.AreEqual(153, bufRect.G);
//			Assert.AreEqual(0, bufRect.B);
//			Assert.AreEqual(Color.FromArgb(0,153,0), bufRect.Color);
//			
//			bufRect.B = 255;
//			Assert.AreEqual(0, bufRect.R);
//			Assert.AreEqual(153, bufRect.G);
//			Assert.AreEqual(255, bufRect.B);
//			Assert.AreEqual(Color.FromArgb(0,153,255), bufRect.Color);
//			
//			bufRect.Color = Color.FromArgb(0,153,0);
//			Assert.AreEqual(0, bufRect.R);
//			Assert.AreEqual(153, bufRect.G);
//			Assert.AreEqual(0, bufRect.B);
//			Assert.AreEqual(Color.FromArgb(0,153,0), bufRect.Color);
		}
		
		[Test]
		public void TestModifyBitmap()
		{
//			//buf.MoveTo(0,0);
//			buf.Color = Color.FromArgb(255,255,255);
//			buf.Dispose();
//			buf = new BitmapBuffer(bmp);
//			Assert.AreEqual(Color.FromArgb(255,255,255), buf.Color);
//			buf.Color = Color.FromArgb(255,0,0);
		}
		
		[Test]
		public void TestTraverse() 
		{
			/*buf.MoveTo(0,0);
			buf.Traverse(1,0);
			Assert.AreEqual(Color.FromArgb(255,255,0), buf.Color);
			buf.Traverse(0,1);
			Assert.AreEqual(Color.FromArgb(204,204,0), buf.Color);
			buf.Traverse(2,2);
			Assert.AreEqual(Color.FromArgb(0,102,102), buf.Color);
			buf.MoveTo(4,4);
			Assert.AreEqual(Color.FromArgb(0,0,51), buf.Color);
			buf.MoveTo(2,2);
			Assert.AreEqual(Color.FromArgb(0,153,0), buf.Color);
			buf.MoveTo(0,0);
			Assert.AreEqual(Color.FromArgb(255,0,0), buf.Color);*/
		}
		
		[Test]
		public void TestColorAt(){
			Assert.AreEqual(Color.FromArgb(255,0,0),buf.GetColorAt(0,0));
			Assert.AreEqual(Color.FromArgb(255,255,0),buf.GetColorAt(1,0));
			Assert.AreEqual(Color.FromArgb(204,204,0),buf.GetColorAt(1,1));
			Assert.AreEqual(Color.FromArgb(0,153,0),buf.GetColorAt(2,2));
			Assert.AreEqual(Color.FromArgb(204,204,0),buf.GetColorAt(1,1));
			Assert.AreEqual(Color.FromArgb(0,0,51),buf.GetColorAt(4,4));
		}
		
		[Test]
		public void TestIterate() 
		{
//			buf.IteratePixel += delegate { 
//				if(buf.Location.X == 0 && buf.Location.Y == 0) 
//				{
//					Assert.AreEqual(Color.FromArgb(255,0,0), buf.Color);
//				}
//				else if(buf.Location.X == 3 && buf.Location.Y == 3) 
//				{
//					Assert.AreEqual(Color.FromArgb(0,102,102), buf.Color);
//				}
//			};
//			buf.Iterate();
		}
		
		[Test]
		public void TestIterateRect() 
		{
//			bufRect.IteratePixel += delegate { 
//				if(bufRect.Location.X == 0 && bufRect.Location.Y == 0) 
//				{
//					Assert.AreEqual(Color.FromArgb(0,153,0), bufRect.Color);
//				}
//				else if(bufRect.Location.X == 1 && bufRect.Location.Y == 0) 
//				{
//					Assert.AreEqual(Color.FromArgb(0,153,153), bufRect.Color);
//				}
//			};
//			bufRect.Iterate();
		}
		

		
	}
}
