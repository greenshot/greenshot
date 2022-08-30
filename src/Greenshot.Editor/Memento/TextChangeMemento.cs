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

using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// The TextChangeMemento makes it possible to undo-redo an IDrawableContainer move
    /// </summary>
    public class TextChangeMemento : IMemento
    {
        private TextContainer _textContainer;
        private readonly string _oldText;

        public TextChangeMemento(TextContainer textContainer)
        {
            _textContainer = textContainer;
            _oldText = textContainer.Text;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _textContainer = null;
            }
        }

        public bool Merge(IMemento otherMemento)
        {
            if (otherMemento is not TextChangeMemento other) return false;

            return other._textContainer.Equals(_textContainer);
        }

        public IMemento Restore()
        {
            // Before
            _textContainer.Invalidate();
            TextChangeMemento oldState = new TextChangeMemento(_textContainer);
            _textContainer.ChangeText(_oldText, false);
            // After
            _textContainer.Invalidate();
            return oldState;
        }
    }
}