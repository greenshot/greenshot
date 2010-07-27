/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Drawing.Imaging;

namespace Greenshot.Helpers {
	/// <summary>
	/// PropertyItemProvider is a helper class to provide instances of PropertyItem 
	/// Be sure to have the PropertyItemProvider.resx too, since it contains the
	/// image we will take the PropertyItem from.
	/// </summary>
	public class PropertyItemProvider {
		private static PropertyItem propertyItem;

		private PropertyItemProvider() {
		}
		
		public static PropertyItem GetPropertyItem(int id, string value) {
			if(propertyItem == null) {
				System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertyItemProvider));
				Bitmap bmp = (Bitmap)resources.GetObject("propertyitemcontainer");
				propertyItem = bmp.GetPropertyItem(bmp.PropertyIdList[0]);
				propertyItem.Type =2; // string
				
			}
			propertyItem.Id = id;
			System.Text.ASCIIEncoding  encoding=new System.Text.ASCIIEncoding();
			propertyItem.Value = encoding.GetBytes(value + " ");
			propertyItem.Len = value.Length + 1;
			return propertyItem;
		}
	}
}
