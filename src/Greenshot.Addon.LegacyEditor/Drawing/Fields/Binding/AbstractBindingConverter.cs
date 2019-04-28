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

using System;

namespace Greenshot.Addon.LegacyEditor.Drawing.Fields.Binding
{
	/// <summary>
	///     Basic IBindingConverter implementation
	/// </summary>
	public abstract class AbstractBindingConverter<T1, T2> : IBindingConverter
	{
		public object Convert(object o)
		{
			if (o == null)
			{
				return null;
			}
			if (o is T1)
			{
				return Convert((T1) o);
			}
			if (o is T2)
			{
				return Convert((T2) o);
			}
			throw new ArgumentException($"{GetType()} cannot handle argument of type {o.GetType()}");
		}

		protected abstract T2 Convert(T1 o);
		protected abstract T1 Convert(T2 o);
	}
}