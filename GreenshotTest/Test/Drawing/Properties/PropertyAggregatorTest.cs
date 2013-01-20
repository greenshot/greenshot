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
using System.Windows.Forms;
using Greenshot.Drawing;


using Greenshot.Drawing.Fields;


namespace Greenshot.Test.Drawing.Properties
{
	/// <summary>
	/// Description of PropertyAggregatorTest.
	/// </summary>
	[TestFixture]
	public class PropertyAggregatorTest
	{
		
		
		[SetUp]
		public void Init()
		{
			
		}
		
		
		
		[TearDown]
		public void Dispose()
		{
		}
		
		
		[Test]
		public void Test()
		{

			FieldType t = FieldType.LINE_THICKNESS;

			Surface s = new Surface();
			FieldAggregator ep = new FieldAggregator();
			//ep.SetFieldValue(t,598);

			RectangleContainer rc = new RectangleContainer(s);
			rc.SetFieldValue(t,597);
			//Assert.AreNotEqual(ep.GetField(t), rc.GetField(t));
			ep.BindElement(rc);
			Assert.AreEqual(597, ep.GetField(t).Value);
			Assert.AreEqual(597, rc.GetField(t).Value);
			
			RectangleContainer rc2 = new RectangleContainer(s);
			Assert.AreEqual(597, ep.GetField(t).Value);
			rc2.SetFieldValue(t,595);
			Assert.AreEqual(595, rc2.GetField(t).Value);
			ep.BindElement(rc2);
			Assert.AreEqual(595, ep.GetField(t).Value);
			Assert.AreEqual(597, rc.GetField(t).Value);
			
			RectangleContainer rc3 = new RectangleContainer(s);
			rc3.SetFieldValue(t,600);
			ep.BindElement(rc3);
			
			//Assert.AreEqual(600, ep.GetField(t).Value);
			Assert.AreEqual(600, rc3.GetField(t).Value);
			Assert.AreEqual(597, rc.GetField(t).Value);
			
			ep.SetFieldValue(t, 599);
			Assert.AreEqual(599, ep.GetField(t).Value);
			Assert.AreEqual(599, rc.GetField(t).Value);
			Assert.AreEqual(599, rc2.GetField(t).Value);
			Assert.AreEqual(599, rc3.GetField(t).Value);
			
			
			
			
			
		}

		public class TestIrrelevantPropertyHolder : RectangleContainer
		{
			public TestIrrelevantPropertyHolder() : base(new Surface())
			{
			}
			public int Leet = 1337;
		}
		

		
	}
}
