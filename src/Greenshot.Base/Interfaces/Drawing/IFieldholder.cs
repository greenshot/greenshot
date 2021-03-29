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

using System.Collections.Generic;

namespace Greenshot.Base.Interfaces.Drawing
{
    /// <summary>
    /// Any element holding Fields must provide access to it.
    /// AbstractFieldHolder is the basic implementation.
    /// If you need the fieldHolder to have child fieldHolders,
    /// you should consider using IFieldHolderWithChildren.
    /// </summary>
    public interface IFieldHolder
    {
        event FieldChangedEventHandler FieldChanged;
        IList<IField> GetFields();
        IField GetField(IFieldType fieldType);
        bool HasField(IFieldType fieldType);
    }
}