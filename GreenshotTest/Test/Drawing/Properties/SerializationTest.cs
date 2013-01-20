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
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Drawing.Filters;
using Greenshot.Helpers;

namespace Greenshot.Test.Drawing.Properties
{
	/// <summary>
	/// Description of SerializationTest.
	/// </summary>
	[TestFixture]
	public class SerializationTest
	{
		
		public SerializationTest()
		{
		}
		
		[Test]
		public void TestSerializeField() {
			Field f = new Field(FieldType.ARROWHEADS, GetType());
			f.myValue = ArrowContainer.ArrowHeadCombination.BOTH;
			
			Field clone = (Field) Objects.DeepClone(f);
			Assert.AreEqual(f, clone);
			Assert.AreEqual(f.Value, clone.Value);
			Assert.AreEqual(f.Scope, clone.Scope);
			
			f.Scope = this.GetType().ToString();
			
			clone = (Field) Objects.DeepClone(f);
			Assert.AreEqual(f, clone);
			Assert.AreEqual(f.Value, clone.Value);
			Assert.AreEqual(f.Scope, clone.Scope);
		}
		
		[Test]
		public void TestSerializeFieldHolder() {
			AbstractFieldHolder afh = new TestFieldHolder();
			AbstractFieldHolder clone = (AbstractFieldHolder)Objects.DeepClone(afh);
			Assert.AreEqual(afh.GetFields(), clone.GetFields());
		}
		
		[Test]
		public void TestSerializeFieldHolderWithChildren() {
			AbstractFieldHolderWithChildren afh = new TestFieldHolderWithChildren();
			AbstractFieldHolderWithChildren clone = (AbstractFieldHolderWithChildren)Objects.DeepClone(afh);
			Assert.AreEqual(afh.GetFields(), clone.GetFields());
		}
		
		[Test]
		public void TestSerializeFilterContainer() {
			ObfuscateContainer oc = new ObfuscateContainer(new Surface());
			
			ObfuscateContainer clone = (ObfuscateContainer)Objects.DeepClone(oc);
			Assert.AreEqual(oc.Children.GetType(), clone.Children.GetType());
			System.Collections.Generic.List<Field> ocFields = oc.GetFields();
			System.Collections.Generic.List<Field> cloneFields = clone.GetFields();
			Assert.AreEqual(ocFields, cloneFields);
		}
		
		[Serializable]
		private class TestFieldHolder : AbstractFieldHolder {}
		[Serializable]
		private class TestFieldHolderWithChildren : AbstractFieldHolderWithChildren {}
	}
	
}
