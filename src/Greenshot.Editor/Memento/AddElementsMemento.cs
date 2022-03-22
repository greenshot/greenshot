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

using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// The AddElementMemento makes it possible to undo adding an element
    /// </summary>
    public class AddElementsMemento : IMemento
    {
        private IDrawableContainerList _containerList;
        private ISurface _surface;

        public AddElementsMemento(ISurface surface, IDrawableContainerList containerList)
        {
            _surface = surface;
            _containerList = containerList;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _containerList?.Dispose();
            }

            _containerList = null;
            _surface = null;
        }

        public bool Merge(IMemento otherMemento)
        {
            return false;
        }

        public IMemento Restore()
        {
            var oldState = new DeleteElementsMemento(_surface, _containerList);

            _surface.RemoveElements(_containerList, false);

            // After, so everything is gone
            _surface.Invalidate();
            return oldState;
        }
    }
}