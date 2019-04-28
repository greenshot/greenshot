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
using System.ComponentModel;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;

namespace Greenshot.Addon.LegacyEditor.Drawing.Filters
{
	/// <summary>
	///     Graphical filter which can be added to DrawableContainer.
	///     Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
	///     OnPropertyChanged whenever a public property has been changed.
	/// </summary>
	[Serializable]
	public abstract class AbstractFilter : AbstractFieldHolder, IFilter
	{
		private bool _invert;

		protected DrawableContainer parent;

		[NonSerialized] private PropertyChangedEventHandler _propertyChanged;

		public AbstractFilter(DrawableContainer parent, IEditorConfiguration editorConfiguration) : base(editorConfiguration)
		{
			this.parent = parent;
		}

		public event PropertyChangedEventHandler PropertyChanged
		{
			add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

		public bool Invert
		{
			get => _invert;
            set
			{
				_invert = value;
				OnPropertyChanged("Invert");
			}
		}

		public DrawableContainer Parent
		{
			get => parent;
            set => parent = value;
        }

		public abstract void Apply(Graphics graphics, IBitmapWithNativeSupport applyBitmap, NativeRect rect, RenderMode renderMode);

		protected void OnPropertyChanged(string propertyName)
		{
			_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}