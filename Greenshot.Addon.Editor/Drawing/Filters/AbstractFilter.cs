//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Drawing;
using Greenshot.Addon.Editor.Drawing.Fields;
using Greenshot.Addon.Interfaces.Drawing;

#endregion

/// <summary>
/// Graphical filter which can be added to DrawableContainer.
/// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
/// OnPropertyChanged whenever a public property has been changed.
/// </summary>

namespace Greenshot.Addon.Editor.Drawing.Filters
{
	[Serializable]
	public abstract class AbstractFilter : AbstractFieldHolder, IFilter
	{
		private bool invert;

		[NonSerialized] protected IDrawableContainer parent;

		public AbstractFilter(DrawableContainer parent)
		{
			this.parent = parent;
			InitFieldAttributes();
		}

		public bool Invert
		{
			get { return invert; }
			set
			{
				invert = value;
				OnPropertyChanged("Invert");
			}
		}

		public IDrawableContainer Parent
		{
			get { return parent; }
			set { parent = value; }
		}

		public abstract void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode);

		public override void Invalidate()
		{
			parent.Invalidate();
		}
	}
}