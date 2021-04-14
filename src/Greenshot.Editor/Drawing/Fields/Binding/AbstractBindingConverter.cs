/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;

namespace Greenshot.Editor.Drawing.Fields.Binding
{
    /// <summary>
    /// Basic IBindingConverter implementation
    /// </summary>
    public abstract class AbstractBindingConverter<T1, T2> : IBindingConverter
    {
        public object convert(object o)
        {
            if (o == null)
            {
                return null;
            }

            if (o is T1)
            {
                return convert((T1) o);
            }

            if (o is T2)
            {
                return convert((T2) o);
            }

            throw new ArgumentException("Cannot handle argument of type " + o.GetType());
        }

        protected abstract T2 convert(T1 o);
        protected abstract T1 convert(T2 o);
    }
}