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

using System.ComponentModel;

namespace Greenshot.Addons.Interfaces.Drawing
{
    /// <summary>
    /// The interface for a field
    /// </summary>
	public interface IField : INotifyPropertyChanged
	{
		/// <summary>
		/// The field value
		/// </summary>
		object Value { get; set; }

		/// <summary>
		/// Field type
		/// </summary>
		IFieldType FieldType { get; set; }

		/// <summary>
		/// The scope of the field
		/// </summary>
		string Scope { get; set; }

		/// <summary>
		/// Does the field have a value
		/// </summary>
		bool HasValue { get; }
	}
}