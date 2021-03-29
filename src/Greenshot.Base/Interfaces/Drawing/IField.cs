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
using System.ComponentModel;

namespace Greenshot.Base.Interfaces.Drawing
{
    [Flags]
    public enum FieldFlag
    {
        NONE = 0,
        CONFIRMABLE = 1,
        COUNTER = 2
    }

    public interface IFieldType
    {
        string Name { get; set; }
    }

    public interface IField : INotifyPropertyChanged
    {
        object Value { get; set; }
        IFieldType FieldType { get; set; }
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
    public class FieldChangedEventArgs : EventArgs
    {
        public IField Field { get; private set; }

        public FieldChangedEventArgs(IField field)
        {
            Field = field;
        }
    }
}