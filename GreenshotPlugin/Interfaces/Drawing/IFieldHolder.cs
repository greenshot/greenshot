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

using System.Collections.Generic;

namespace GreenshotPlugin.Interfaces.Drawing
{
	/// <summary>
	/// Any element holding Fields must provide access to it.
	/// AbstractFieldHolder is the basic implementation.
	/// If you need the fieldHolder to have child fieldHolders,
	/// you should consider using IFieldHolderWithChildren.
	/// </summary>
	public interface IFieldHolder {
		
		event FieldChangedEventHandler FieldChanged;
		
		void AddField(IField field);
		void RemoveField(IField field);
		IList<IField> GetFields();
		IField GetField(IFieldType fieldType);
		bool HasField(IFieldType fieldType);
		void SetFieldValue(IFieldType fieldType, object value);
	}
	
	/// <summary>
	/// Extended fieldHolder which has fieldHolder children.
	/// Implementations should pass field values to and from 
	/// their children.
	/// AbstractFieldHolderWithChildren is the basic implementation.
	/// </summary>
	public interface IFieldHolderWithChildren : IFieldHolder {
		void AddChild(IFieldHolder fieldHolder);
		void RemoveChild(IFieldHolder fieldHolder);
	}
}
