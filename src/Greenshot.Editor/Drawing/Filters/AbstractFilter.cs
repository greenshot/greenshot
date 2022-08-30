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
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing.Filters
{
    /// <summary>
    /// Graphical filter which can be added to DrawableContainer.
    /// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
    /// OnPropertyChanged whenever a public property has been changed.
    /// </summary>
    [Serializable]
    public abstract class AbstractFilter : AbstractFieldHolder, IFilter
    {
        [NonSerialized] private PropertyChangedEventHandler propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { propertyChanged += value; }
            remove { propertyChanged -= value; }
        }

        private bool invert;

        public bool Invert
        {
            get { return invert; }
            set
            {
                invert = value;
                OnPropertyChanged("Invert");
            }
        }

        protected DrawableContainer parent;

        public DrawableContainer Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public AbstractFilter(DrawableContainer parent)
        {
            this.parent = parent;
        }

        public DrawableContainer GetParent()
        {
            return parent;
        }

        public abstract void Apply(Graphics graphics, Bitmap applyBitmap, NativeRect rect, RenderMode renderMode);

        protected void OnPropertyChanged(string propertyName)
        {
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}