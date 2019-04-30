// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;

namespace Greenshot.Addons.Interfaces.Drawing
{
	/// <summary>
	///     Any element holding Fields must provide access to it.
	///     AbstractFieldHolder is the basic implementation.
	///     If you need the fieldHolder to have child fieldHolders,
	///     you should consider using IFieldHolderWithChildren.
	/// </summary>
	public interface IFieldHolder
	{
		/// <summary>
		/// This event is fired when the field changes
		/// </summary>
		event FieldChangedEventHandler FieldChanged;

        /// <summary>
        /// Add a field to the holder
        /// </summary>
        /// <param name="field">IField</param>
        void AddField(IField field);

		/// <summary>
		/// Remove the specified field from this holder
		/// </summary>
		/// <param name="field"></param>
		void RemoveField(IField field);

		/// <summary>
        /// Get all the fields
        /// </summary>
        /// <returns></returns>
		IList<IField> GetFields();

        /// <summary>
        /// Get the field with the specified type
        /// </summary>
        /// <param name="fieldType">IFieldType</param>
        /// <returns>IField</returns>
		IField GetField(IFieldType fieldType);

        /// <summary>
        /// Does this holder have a certain field?
        /// </summary>
        /// <param name="fieldType">IFieldType</param>
        /// <returns>bool</returns>
		bool HasField(IFieldType fieldType);

        /// <summary>
        /// Set the value for a certain field type
        /// </summary>
        /// <param name="fieldType">IFieldType</param>
        /// <param name="value">object</param>
		void SetFieldValue(IFieldType fieldType, object value);
	}
}