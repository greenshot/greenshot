#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Collections.Generic;

#endregion

namespace GreenshotPlugin.Interfaces.Drawing
{
	/// <summary>
	///     Any element holding Fields must provide access to it.
	///     AbstractFieldHolder is the basic implementation.
	///     If you need the fieldHolder to have child fieldHolders,
	///     you should consider using IFieldHolderWithChildren.
	/// </summary>
	public interface IFieldHolder
	{
		event FieldChangedEventHandler FieldChanged;

		void AddField(IField field);
		void RemoveField(IField field);
		IList<IField> GetFields();
		IField GetField(IFieldType fieldType);
		bool HasField(IFieldType fieldType);
		void SetFieldValue(IFieldType fieldType, object value);
	}
}