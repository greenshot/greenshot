/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// Undo and redo of a change of the emoji of one or more emoji elements, e.g. a pick in the Selection panel
    /// </summary>
    public sealed class EmojiChangeMemento : IMemento
    {
        private List<(IEmojiContainer Container, string Emoji)> _states;

        /// <summary>
        /// Remember the emoji of the elements before they change
        /// </summary>
        public EmojiChangeMemento(IEnumerable<IEmojiContainer> containers)
        {
            _states = containers.Select(container => (container, container.Emoji)).ToList();
        }

        public void Dispose()
        {
            _states = null;
        }

        /// <summary>
        /// Every pick is its own undo step
        /// </summary>
        public bool Merge(IMemento otherMemento) => false;

        public IMemento Restore()
        {
            var redo = new EmojiChangeMemento(_states.Select(state => state.Container));
            foreach (var (container, emoji) in _states)
            {
                container.Invalidate();
                container.Emoji = emoji;
                container.Invalidate();
            }

            return redo;
        }
    }
}
