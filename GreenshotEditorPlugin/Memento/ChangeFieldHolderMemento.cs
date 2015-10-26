/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Interfaces.Drawing;

namespace GreenshotEditorPlugin.Memento
{
	/// <summary>
	/// The ChangeFieldHolderMemento makes it possible to undo-redo an IDrawableContainer move
	/// </summary>
	public class ChangeFieldHolderMemento : IMemento
	{
		private IFieldHolder fieldHolder;
		private readonly FieldAttribute fieldAttribute;
		private readonly object oldValue;

		public ChangeFieldHolderMemento(IFieldHolder fieldHolder, FieldAttribute fieldAttribute)
		{
			this.fieldHolder = fieldHolder;
			this.fieldAttribute = fieldAttribute;
			oldValue = fieldAttribute.GetValue(fieldHolder);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) { }
			fieldHolder = null;
		}

		public string ActionDescription
		{
			get
			{
				return "";
			}
		}

		public bool Merge(IMemento otherMemento)
		{
			ChangeFieldHolderMemento other = otherMemento as ChangeFieldHolderMemento;
			if (other != null)
			{
				// Check if it's the same IFieldHolder
				if (other.fieldHolder.Equals(fieldHolder))
				{
					// Check if it'S the same field
					if (other.fieldAttribute.Equals(fieldAttribute))
					{
						// Match, do not store anything as the initial state is what we want.
						return true;
					}
				}
			}
			return false;
		}

		public IMemento Restore()
		{
			// Before
			fieldHolder.Invalidate();
			ChangeFieldHolderMemento oldState = new ChangeFieldHolderMemento(fieldHolder, fieldAttribute);
			// invalidation will be triggered by the SetValue
			fieldAttribute.SetValue(fieldHolder, oldValue);
			return oldState;
		}
	}
}