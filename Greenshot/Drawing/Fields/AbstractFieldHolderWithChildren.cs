/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using GreenshotPlugin.Interfaces.Drawing;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Greenshot.Drawing.Fields {
	/// <summary>
	/// Basic IFieldHolderWithChildren implementation. Similar to IFieldHolder,
	/// but has a List<IFieldHolder> of children.
	/// Field values are passed to and from children as well.
	/// </summary>
	[Serializable()] 
	public abstract class AbstractFieldHolderWithChildren : AbstractFieldHolder {
		readonly FieldChangedEventHandler fieldChangedEventHandler;
		
		[NonSerialized]
		private EventHandler childrenChanged;
		public event EventHandler ChildrenChanged {
			add { childrenChanged += value; }
			remove { childrenChanged -= value; }
		}
		
		public List<IFieldHolder> Children = new List<IFieldHolder>();
	
		public AbstractFieldHolderWithChildren() {
			fieldChangedEventHandler = OnFieldChanged;
		}
		
		[OnDeserialized()]
		private void OnDeserialized(StreamingContext context) {
			// listen to changing properties
			foreach(IFieldHolder fieldHolder in Children) {
				fieldHolder.FieldChanged += fieldChangedEventHandler;
			}
			if(childrenChanged != null) childrenChanged(this, EventArgs.Empty);
		}
		
		public void AddChild(IFieldHolder fieldHolder) {
			Children.Add(fieldHolder);
			fieldHolder.FieldChanged += fieldChangedEventHandler;
			if(childrenChanged != null) childrenChanged(this, EventArgs.Empty);
		}
		
		public void RemoveChild(IFieldHolder fieldHolder) {
			Children.Remove(fieldHolder);
			fieldHolder.FieldChanged -= fieldChangedEventHandler;
			if(childrenChanged != null) childrenChanged(this, EventArgs.Empty);
		}
		
		public new IList<IField> GetFields() {
			List<IField> ret = new List<IField>();
			ret.AddRange(base.GetFields());
			foreach(IFieldHolder fh in Children) {
				ret.AddRange(fh.GetFields());
			}
			return ret;
		}

		public new IField GetField(IFieldType fieldType)
		{
			IField ret = null;
			if (base.HasField(fieldType))
			{
				ret = base.GetField(fieldType);
			}
			else {
				foreach (IFieldHolder fh in Children)
				{
					if (fh.HasField(fieldType))
					{
						ret = fh.GetField(fieldType);
						break;
					}
				}
			}
			if (ret == null)
			{
				throw new ArgumentException("Field '" + fieldType + "' does not exist in " + GetType());
			}
			return ret;
		}

		public new bool HasField(IFieldType fieldType) {
			bool ret = base.HasField(fieldType);
			if(!ret) {
				foreach(IFieldHolder fh in Children) {
					if(fh.HasField(fieldType)) {
						ret = true;
						break;
					}
				}
			}
			return ret;
		}
		
		public new bool HasFieldValue(IFieldType fieldType) {
			IField f = GetField(fieldType);
			return f != null && f.HasValue;
		}
		
		public new void SetFieldValue(IFieldType fieldType, object value) {
			IField f = GetField(fieldType);
			if(f != null) f.Value = value;
		}
		
	}
}
