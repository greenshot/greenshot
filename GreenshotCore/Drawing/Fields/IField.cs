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
using System.ComponentModel;

namespace Greenshot.Drawing.Fields {

	public interface IField : INotifyPropertyChanged
	{
		object Value { get; set; }
		FieldType FieldType { get; set; }
		string Scope { get; set; }
		bool HasValue { get; }
	}
	
	/// <summary>
	/// EventHandler to be used when a field value changes
	/// </summary>
	public delegate void FieldChangedEventHandler(object sender, FieldChangedEventArgs e);
	
	/// <summary>
	/// EventArgs to be used with FieldChangedEventHandler
	/// </summary>
	public class FieldChangedEventArgs : EventArgs {
		public readonly IField Field;
		public FieldChangedEventArgs(IField field) {
			this.Field = field;
		}
	}
}
